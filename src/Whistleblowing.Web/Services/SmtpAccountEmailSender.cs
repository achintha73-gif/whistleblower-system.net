using System.Net;
using System.Net.Mail;

namespace Whistleblowing.Web.Services;

public sealed class SmtpAccountEmailSender(IConfiguration configuration, ILogger<SmtpAccountEmailSender> logger) : IAccountEmailSender
{
    public async Task<bool> SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:Smtp:Host"];
        var fromAddress = configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            logger.LogError("Password reset email cannot be sent because SMTP settings are missing.");
            return false;
        }

        if (!int.TryParse(configuration["Email:Smtp:Port"], out var port))
            port = 587;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, configuration["Email:FromName"] ?? "Whistleblowing System"),
            Subject = "Reset your password",
            IsBodyHtml = true,
            Body = $"<p>We received a request to reset your password.</p><p><a href=\"{WebUtility.HtmlEncode(resetUrl)}\">Reset your password</a></p><p>This link expires after a short time. If you did not request this, you can ignore this email.</p>"
        };
        message.To.Add(new MailAddress(email));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enableSsl) ? enableSsl : true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not send password reset email.");
            return false;
        }
    }
}
