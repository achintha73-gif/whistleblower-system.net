namespace Whistleblowing.Domain.Enums;

public static class ApplicationSettingKeys
{
    public const string OrganizationName = "OrganizationName";
    public const string AnonymousSubmissionsEnabled = "AnonymousSubmissionsEnabled";
    public const string EvidenceMaxFileSizeMb = "EvidenceMaxFileSizeMb";
    public const string DefaultCasePriority = "DefaultCasePriority";
    public const string SupportPhone = "SupportPhone";
    public const string SupportEmail = "SupportEmail";

    public static readonly (string Key, string Value, string Description)[] Defaults =
    {
        (OrganizationName, "Whistleblowing System", "Organization name shown in the application header and page titles."),
        (AnonymousSubmissionsEnabled, "true", "Allow people without an account to submit anonymous concerns."),
        (EvidenceMaxFileSizeMb, "10", "Maximum size for a single evidence attachment, from 1 to 25 MB."),
        (DefaultCasePriority, nameof(CasePriority.Medium), "Priority selected by default when a concern becomes an investigation case."),
        (SupportPhone, "", "Optional support phone number shown as a click-to-call link on the Support page."),
        (SupportEmail, "", "Optional support email shown as a click-to-email link on the Support page."),
    };
}
