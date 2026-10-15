using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Web.Models.Account;
using Whistleblowing.Web.Services;

namespace Whistleblowing.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IAccountEmailSender _emailSender;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger,
        IAccountEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
    }

    // -------------------------------------------------------------
    // FORGOT / RESET PASSWORD
    // -------------------------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && user.Status == UserStatus.Active)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action(nameof(ResetPassword), "Account", new
            {
                userId = user.Id,
                token
            }, Request.Scheme);

            if (!string.IsNullOrWhiteSpace(resetUrl))
                await _emailSender.SendPasswordResetAsync(user.Email!, resetUrl, HttpContext.RequestAborted);
        }

        // Keep the response the same whether an account exists, to avoid revealing registered emails.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return View("ResetPasswordInvalid");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return View("ResetPasswordInvalid");

        return View(new ResetPasswordViewModel { UserId = user.Id.ToString(), Token = token, Email = user.Email ?? string.Empty });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null || !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            return View("ResetPasswordInvalid");

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            TempData["PasswordResetSuccess"] = "Your password has been reset. Sign in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // -------------------------------------------------------------
    // LOGIN
    // -------------------------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, string? mode = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.LoginMode = string.Equals(mode, "anonymous", StringComparison.OrdinalIgnoreCase) ? "anonymous" : "employee";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password");
            return View(model);
        }

        if (user.Status != UserStatus.Active)
        {
            ModelState.AddModelError(string.Empty, "Your account is not active. Contact an administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked due to multiple failed attempts. Try again in 15 minutes.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password");
        return View(model);
    }

    // -------------------------------------------------------------
    // REGISTER
    // -------------------------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(string.Empty, "An account with that email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            Phone = model.Phone,
            Status = UserStatus.Active,
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, RoleNames.Employee);
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("New user registered: {Email}", model.Email);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // -------------------------------------------------------------
    // LOGOUT
    // -------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var email = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {Email} logged out", email);
        return RedirectToAction("Login");
    }

    // -------------------------------------------------------------
    // ACCESS DENIED
    // -------------------------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
    // -------------------------------------------------------------
    // PROFILE
    // -------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        ViewBag.Profile = new EditProfileViewModel { FullName = user.FullName, Phone = user.Phone };
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(EditProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewBag.Profile = model;
            return View("Profile", user);
        }

        user.FullName = model.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedByUserId = user.Id;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            ViewBag.Profile = model;
            return View("Profile", user);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["ProfileSuccess"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Profile = new EditProfileViewModel { FullName = user.FullName, Phone = user.Phone };
            ViewBag.PasswordModel = model;
            return View("Profile", user);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("Password", error.Description);
            ViewBag.Profile = new EditProfileViewModel { FullName = user.FullName, Phone = user.Phone };
            ViewBag.PasswordModel = model;
            return View("Profile", user);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["PasswordSuccess"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    // -------------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------------

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
