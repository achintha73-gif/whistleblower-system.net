# Whistleblowing Portal

ASP.NET Core MVC application for submitting, reviewing, investigating, and tracking workplace concerns.

## Requirements

- .NET 10 SDK
- MySQL 8 or a compatible MySQL server

## Local setup (PowerShell)

From the repository root, set the database connection and initial administrator credentials in .NET User Secrets. Replace the sample database values and password with your own local values:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=whistleblowing_dotnet;User=whistleblowing_user;Password=YOUR_LOCAL_DB_PASSWORD;CharSet=utf8mb4" --project src/Whistleblowing.Web
dotnet user-secrets set "Seed:AdminEmail" "admin@wb.local" --project src/Whistleblowing.Web
dotnet user-secrets set "Seed:AdminPassword" "YOUR_STRONG_ADMIN_PASSWORD" --project src/Whistleblowing.Web
```

Create the database and database user in MySQL, then apply the included EF Core migration:

```powershell
dotnet ef database update --project src/Whistleblowing.Infrastructure --startup-project src/Whistleblowing.Web
```

Run the web application:

```powershell
dotnet run --project src/Whistleblowing.Web
```

On first startup, the application creates the built-in roles, initial administrator, sample departments, and default settings. After the initial administrator exists, the seed password is no longer needed at startup. User Secrets stay outside source control; do not put real credentials in `appsettings.json`.

## Main capabilities

- Role-based dashboards and access for administrators, managers, investigators, and employees
- Authenticated and anonymous concern submission, reference-code tracking, and additional information
- Review and case conversion, investigator assignment, evidence handling, case reports, and manager review
- User and department management, application settings, notifications, and activity logs
