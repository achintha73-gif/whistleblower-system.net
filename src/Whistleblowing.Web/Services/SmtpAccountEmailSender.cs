using System.Net;
using System.Net.Mail;

namespace Whistleblowing.Web.Services;

public sealed class SmtpAccountEmailSender(IConfiguration configuration, ILogger<SmtpAccountEmailSender> logger) : IAccountEmailSender
{
    public Task<bool> SendCaseAssignedAsync(string email, string investigatorName, string caseTitle, string caseUrl, CancellationToken cancellationToken = default) =>
        SendWorkflowEmailAsync(email, "A case has been assigned to you", "Case assignment", investigatorName,
            $"You have been assigned to investigate <strong>{WebUtility.HtmlEncode(caseTitle)}</strong>.", caseUrl, cancellationToken);

    public Task<bool> SendReportSubmittedAsync(string email, string managerName, string caseTitle, string caseUrl, CancellationToken cancellationToken = default) =>
        SendWorkflowEmailAsync(email, "An investigation report is ready for review", "Report submitted for review", managerName,
            $"An investigator submitted the report for <strong>{WebUtility.HtmlEncode(caseTitle)}</strong>. Please review the case.", caseUrl, cancellationToken);

    public async Task<bool> SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(email, "Reset your password",
            $"<p>We received a request to reset your password.</p><p><a href=\"{WebUtility.HtmlEncode(resetUrl)}\">Reset your password</a></p><p>This link expires after a short time. If you did not request this, you can ignore this email.</p>",
            "Password reset", cancellationToken);
    }

    private Task<bool> SendWorkflowEmailAsync(string email, string subject, string heading, string recipientName, string message, string actionUrl, CancellationToken cancellationToken)
    {
        var body = $"<h2>{WebUtility.HtmlEncode(heading)}</h2><p>Hello {WebUtility.HtmlEncode(recipientName)},</p><p>{message}</p><p><a href=\"{WebUtility.HtmlEncode(actionUrl)}\">Open case</a></p><p>Whistleblowing Management System</p>";
        return SendEmailAsync(email, subject, body, subject, cancellationToken);
    }

    private async Task<bool> SendEmailAsync(string email, string subject, string body, string purpose, CancellationToken cancellationToken)
    {
        var host = configuration["Email:Smtp:Host"];
        var fromAddress = configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            logger.LogWarning("{EmailPurpose} email was not sent because SMTP settings are missing.", purpose);
            return false;
        }

        try
        {
            if (!int.TryParse(configuration["Email:Smtp:Port"], out var port)) port = 587;
            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, configuration["Email:FromName"] ?? "Whistleblowing System"),
                Subject = subject,
                IsBodyHtml = true,
                Body = body
            };
            message.To.Add(new MailAddress(email));
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enableSsl) ? enableSsl : true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            var username = configuration["Email:Smtp:Username"];
            if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, configuration["Email:Smtp:Password"]);
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not send {EmailPurpose} email to {Recipient}.", purpose, email);
            return false;
        }
    }
}
