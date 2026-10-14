namespace Whistleblowing.Domain.Enums;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Investigator = "Investigator";
    public const string Employee = "Employee";

    public static readonly string[] All =
    {
        Admin,
        Manager,
        Investigator,
        Employee,
    };
}