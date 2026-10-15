namespace Whistleblowing.Web.Services;

public interface IAccountEmailSender
{
    Task<bool> SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default);
}
