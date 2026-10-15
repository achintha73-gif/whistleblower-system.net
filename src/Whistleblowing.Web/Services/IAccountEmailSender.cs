namespace Whistleblowing.Web.Services;

public interface IAccountEmailSender
{
    Task<bool> SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default);
    Task<bool> SendCaseAssignedAsync(string email, string investigatorName, string caseTitle, string caseUrl, CancellationToken cancellationToken = default);
    Task<bool> SendReportSubmittedAsync(string email, string managerName, string caseTitle, string caseUrl, CancellationToken cancellationToken = default);
}
