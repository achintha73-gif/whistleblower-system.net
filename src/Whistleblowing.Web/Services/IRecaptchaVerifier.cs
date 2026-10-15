namespace Whistleblowing.Web.Services;

public interface IRecaptchaVerifier
{
    Task<bool> VerifyAsync(string? responseToken, CancellationToken cancellationToken = default);
}
