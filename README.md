# Whistleblowing Management System

A web application for securely reporting, reviewing, investigating, and tracking workplace concerns. Employees can submit concerns while signed in or anonymously. Managers and investigators can review reports, manage cases, handle evidence, and prepare investigation reports.

## Features

- Employee registration, sign-in, profile management, and password reset.
- Signed-in and anonymous concern submission.
- Reference-code tracking for anonymous concerns, with options to send additional information and evidence.
- Concern review, case creation, priority management, and investigator assignment.
- Evidence uploads and downloads for PDF, PNG, JPG/JPEG, GIF, WEBP, and TXT files.
- Investigation reports with manager review.
- A report page employees can print or save as PDF from their browser.
- User and department management, system settings, notifications, and activity logs.
- Responsive layouts for mobile and desktop.
- Optional Google reCAPTCHA verification and rate limiting for anonymous tracking requests.

## User roles

| Role | Main responsibilities |
| --- | --- |
| Administrator | Manage users, departments, system settings, and activity logs. |
| Manager | Review concerns, manage cases, assign investigators, and review reports. |
| Investigator | Review assigned cases, manage evidence, and submit investigation reports. |
| Employee | Submit concerns and view the status of their own submissions. |
| Anonymous reporter | Submit a concern without an account, track it with a reference code, and provide additional information or evidence. |

## Technology

- .NET 10 and ASP.NET Core MVC
- Entity Framework Core 9 and the Pomelo MySQL provider
- MySQL 8 or a compatible MySQL server
- ASP.NET Core Identity and role-based authorization
- Razor Views and Tailwind CSS
- xUnit test project

## Project structure

~~~text
src/
  Whistleblowing.Domain/          Core entities, roles, and constants
  Whistleblowing.Application/     DTOs and application service interfaces
  Whistleblowing.Infrastructure/  Database access, EF Core migrations, and services
  Whistleblowing.Web/             MVC controllers, views, and static assets
tests/
  Whistleblowing.UnitTests/       xUnit tests
~~~

## Requirements

- .NET 10 SDK
- MySQL 8 or a compatible MySQL server
- EF Core CLI (dotnet-ef) to apply database migrations

Install the EF Core CLI if it is not already available:

~~~powershell
dotnet tool install --global dotnet-ef --version 9.*
~~~

## Run locally

Run the following PowerShell commands from the repository root.

### 1. Prepare the MySQL database

Create a database named **whistleblowing_dotnet** and a MySQL user with permission to access it. Use your own local database name, username, and password in the connection string below.

### 2. Store local secrets

Save the database connection string and initial administrator credentials with .NET User Secrets. Do not put real passwords in source code or appsettings.json.

~~~powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=whistleblowing_dotnet;User=whistleblowing_user;Password=YOUR_LOCAL_DB_PASSWORD;CharSet=utf8mb4" --project src/Whistleblowing.Web
dotnet user-secrets set "Seed:AdminEmail" "admin@wb.local" --project src/Whistleblowing.Web
dotnet user-secrets set "Seed:AdminPassword" "YOUR_STRONG_ADMIN_PASSWORD" --project src/Whistleblowing.Web
~~~

The initial administrator password is required only when the administrator account is first created. If an administrator with that email already exists in the database, the seeder does not create another one.

### 3. Apply database migrations

~~~powershell
dotnet ef database update --project src/Whistleblowing.Infrastructure --startup-project src/Whistleblowing.Web
~~~

### 4. Start the web application

~~~powershell
dotnet run --project src/Whistleblowing.Web
~~~

Open the local URL shown in the terminal. On first startup, the application creates the built-in roles, initial administrator, sample departments, and default settings.

## Optional service configuration

### Password reset emails

Configure these values with details provided by your SMTP service:

~~~powershell
dotnet user-secrets set "Email:Smtp:Host" "smtp.example.com" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:Port" "587" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:EnableSsl" "true" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:Username" "YOUR_SMTP_USERNAME" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:Password" "YOUR_SMTP_PASSWORD" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:FromAddress" "noreply@example.com" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:FromName" "Whistleblowing System" --project src/Whistleblowing.Web
~~~

Without SMTP settings, password reset emails cannot be delivered.

### Google reCAPTCHA for anonymous submissions

Configure the site key and secret key from your Google reCAPTCHA project. When testing locally, allow **localhost** in the reCAPTCHA domain settings.

~~~powershell
dotnet user-secrets set "GoogleReCaptcha:SiteKey" "YOUR_RECAPTCHA_SITE_KEY" --project src/Whistleblowing.Web
dotnet user-secrets set "GoogleReCaptcha:SecretKey" "YOUR_RECAPTCHA_SECRET_KEY" --project src/Whistleblowing.Web
~~~

reCAPTCHA verification is available when these keys are configured.

## Build and test

~~~powershell
dotnet build Whistleblowing.slnx
dotnet test Whistleblowing.slnx
~~~

## Data and security notes

- Do not store database, SMTP, reCAPTCHA, or administrator passwords in source code. Use .NET User Secrets or protected environment configuration.
- Evidence files are stored in **src/Whistleblowing.Web/App_Data/Evidence**. This folder is excluded from Git. Back it up separately and restrict access in a production deployment.
- The application checks evidence file types and sizes. Before deploying to production, configure HTTPS, backups, SMTP, reCAPTCHA, and server-level security.

## Support

The **Support** page in the application displays the configured support email address and phone number.