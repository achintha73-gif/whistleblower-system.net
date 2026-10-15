# Whistleblowing Management System

සේවා ස්ථානයක ගැටලු සහ අක්‍රමිකතා රහසිගතව වාර්තා කර, සමාලෝචනය කර, විමර්ශනය කිරීමට භාවිත කරන වෙබ් පද්ධතියකි. සේවකයින්ට ගිණුමක් හරහා හෝ අනන්‍යතාව හෙළි නොකර පැමිණිලි ඉදිරිපත් කළ හැක. අදාළ කළමනාකරුවන් සහ විමර්ශකයින්ට පැමිණිලි පාලනය කර වාර්තා සකස් කළ හැක.

## ප්‍රධාන විශේෂාංග

- සේවක ගිණුම්, පුරනය වීම, පැතිකඩ සහ අමතක වූ මුරපද නැවත සකස් කිරීම.
- සේවක සහ අනන්‍ය පැමිණිලි ඉදිරිපත් කිරීම.
- යොමු කේතයක් භාවිතයෙන් අනන්‍ය පැමිණිල්ලක තත්ත්වය පරීක්ෂා කිරීම සහ අමතර තොරතුරු යැවීම.
- පැමිණිලි සමාලෝචනය, නඩු විවෘත කිරීම, ප්‍රමුඛතාව නියම කිරීම සහ විමර්ශකයින් පැවරීම.
- PDF, PNG, JPG/JPEG, GIF, WEBP සහ TXT සාක්ෂි ගොනු යැවීම සහ අවසර ඇති පරිශීලකයින්ට බාගත කිරීම.
- විමර්ශන වාර්තා සකස් කිරීම සහ කළමනාකරුගේ සමාලෝචනය.
- සේවකයින්ට වාර්තා කියවීමට, මුද්‍රණය කිරීමට හෝ PDF ලෙස සුරැකීමට හැකි පිටුවක්.
- පරිශීලක, දෙපාර්තමේන්තු, පද්ධති සැකසුම්, දැනුම්දීම් සහ ක්‍රියාකාරකම් වාර්තා පාලනය.
- ජංගම සහ පරිගණක තිරවලට ගැළපෙන අතුරුමුහුණත.
- Google reCAPTCHA සහ අනන්‍ය පැමිණිලි පරීක්ෂාව සඳහා ඉල්ලීම් සීමා කිරීම.

## පරිශීලක භූමිකා

| භූමිකාව | ප්‍රධාන භාවිතය |
| --- | --- |
| පරිපාලක | පරිශීලකයින්, දෙපාර්තමේන්තු, පද්ධති සැකසුම් සහ ක්‍රියාකාරකම් වාර්තා පාලනය. |
| කළමනාකරු | පැමිණිලි සමාලෝචනය, නඩු පාලනය, විමර්ශකයින් පැවරීම සහ වාර්තා සමාලෝචනය. |
| විමර්ශක | තමන්ට පැවරූ නඩු පරීක්ෂා කිරීම, සාක්ෂි කළමනාකරණය සහ වාර්තා යැවීම. |
| සේවක | පැමිණිලි ඉදිරිපත් කිරීම සහ තම පැමිණිලිවල තත්ත්වය බැලීම. |
| අනන්‍ය පරිශීලක | ගිණුමක් නොමැතිව වාර්තා කිරීම, යොමු කේතයෙන් තත්ත්වය බැලීම සහ අමතර සාක්ෂි යැවීම. |

## භාවිත කරන තාක්ෂණ

- .NET 10 සහ ASP.NET Core MVC
- Entity Framework Core 9 සහ Pomelo MySQL provider
- MySQL 8 හෝ ඊට අනුකූල දත්තගබඩාව
- ASP.NET Core Identity සහ භූමිකා අනුව අවසර පාලනය
- Razor Views සහ Tailwind CSS
- xUnit පරීක්ෂණ ව්‍යාපෘතිය

## ව්‍යාපෘති ෆෝල්ඩර්

```text
src/
  Whistleblowing.Domain/          මූලික ආයතන, භූමිකා සහ නියත අගයන්
  Whistleblowing.Application/     DTO සහ යෙදුම් සේවා අතුරුමුහුණත්
  Whistleblowing.Infrastructure/  දත්තගබඩාව, EF Core migrations සහ සේවා
  Whistleblowing.Web/             MVC පාලක, පිටු සහ ස්ථිතික ගොනු
tests/
  Whistleblowing.UnitTests/       xUnit පරීක්ෂණ
```

## අවශ්‍යතා

- .NET 10 SDK
- MySQL 8 හෝ ගැළපෙන MySQL සේවාදායකයක්
- EF Core CLI (`dotnet-ef`) — දත්තගබඩා migrations ක්‍රියාත්මක කිරීමට

EF Core CLI ස්ථාපනය කර නොමැති නම්:

```powershell
dotnet tool install --global dotnet-ef --version 9.*
```

## දේශීයව ක්‍රියාත්මක කිරීම

පහත විධාන PowerShell එකෙන්, repository එකේ මුල් ෆෝල්ඩරයේ සිට ක්‍රියාත්මක කරන්න.

### 1. MySQL දත්තගබඩාව සකස් කරන්න

`whistleblowing_dotnet` නමින් දත්තගබඩාවක් සාදා, යෙදුමට එයට සම්බන්ධ වීමට අවශ්‍ය අවසර ඇති MySQL පරිශීලකයෙකු සකස් කරන්න. පහත උදාහරණයේ දත්තගබඩා තොරතුරු ඔබේ දේශීය සැකසුමට ගැළපෙන ලෙස වෙනස් කරන්න.

### 2. රහස් අගයන් දේශීයව සුරකින්න

දත්තගබඩා සම්බන්ධතාව සහ පළමු පරිපාලක මුරපදය .NET User Secrets තුළ තබන්න. සැබෑ මුරපද source code හෝ `appsettings.json` තුළ නොතබන්න.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=whistleblowing_dotnet;User=whistleblowing_user;Password=YOUR_LOCAL_DB_PASSWORD;CharSet=utf8mb4" --project src/Whistleblowing.Web
dotnet user-secrets set "Seed:AdminEmail" "admin@wb.local" --project src/Whistleblowing.Web
dotnet user-secrets set "Seed:AdminPassword" "YOUR_STRONG_ADMIN_PASSWORD" --project src/Whistleblowing.Web
```

පළමු පරිපාලක ගිණුම සෑදීමට Seed:AdminPassword අගය අවශ්‍යයි. එම ඊමේල් ලිපිනයෙන් පරිපාලක ගිණුමක් දැනටමත් තිබේ නම් එය නැවත සාදන්නේ නැහැ.

### 3. දත්තගබඩා වගු සකස් කරන්න

```powershell
dotnet ef database update --project src/Whistleblowing.Infrastructure --startup-project src/Whistleblowing.Web
```

### 4. වෙබ් යෙදුම ධාවනය කරන්න

```powershell
dotnet run --project src/Whistleblowing.Web
```

පර්යන්තයේ පෙන්වන දේශීය ලිපිනය බ්‍රවුසරයෙන් විවෘත කරන්න. පළමු වරට ධාවනය වන විට පද්ධතිය භූමිකා, පරිපාලක ගිණුම, නියැදි දෙපාර්තමේන්තු සහ පෙරනිමි සැකසුම් සකස් කරයි.

## අමතර සේවා සැකසුම්

### මුරපද නැවත සකස් කිරීමේ ඊමේල්

SMTP සේවාවෙන් ලබාගත් තොරතුරු User Secrets තුළ සකසන්න:

```powershell
dotnet user-secrets set "Email:Smtp:Host" "smtp.example.com" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:Port" "587" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:EnableSsl" "true" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:Username" "YOUR_SMTP_USERNAME" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:Smtp:Password" "YOUR_SMTP_PASSWORD" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:FromAddress" "noreply@example.com" --project src/Whistleblowing.Web
dotnet user-secrets set "Email:FromName" "Whistleblowing System" --project src/Whistleblowing.Web
```

### අනන්‍ය පැමිණිලි සඳහා Google reCAPTCHA

Google reCAPTCHA පාලක පැනලයෙන් ලැබෙන අඩවි යතුර සහ රහස් යතුර සකසන්න. ඔබේ පරිගණකයේ පරීක්ෂා කරන විට localhost ලිපිනයට අවසර දී ඇති බව තහවුරු කරන්න.

```powershell
dotnet user-secrets set "GoogleReCaptcha:SiteKey" "YOUR_RECAPTCHA_SITE_KEY" --project src/Whistleblowing.Web
dotnet user-secrets set "GoogleReCaptcha:SecretKey" "YOUR_RECAPTCHA_SECRET_KEY" --project src/Whistleblowing.Web
```

SMTP සැකසුම් නැතිනම් මුරපද නැවත සකස් කිරීමේ ඊමේල් යැවිය නොහැක. reCAPTCHA යතුරු සකසා ඇති විට පමණක් එම තහවුරු කිරීම සක්‍රිය වේ.

## Build болон тест

```powershell
dotnet build Whistleblowing.slnx
dotnet test Whistleblowing.slnx
```

## දත්ත සහ ආරක්ෂාව

- දත්තගබඩාව, SMTP, reCAPTCHA සහ පරිපාලක මුරපද source code තුළ නොතබන්න. .NET User Secrets හෝ ආරක්ෂිත පරිසර සැකසුම් භාවිත කරන්න.
- සාක්ෂි ගොනු src/Whistleblowing.Web/App_Data/Evidence ෆෝල්ඩරයේ ගබඩා වේ. එය GitHub වෙත යවන්නේ නැහැ. සැබෑ භාවිතයේදී මෙම ගොනු වෙනම උපස්ථ කර, ඒවාට පිවිසීම සීමා කරන්න.
- යවන ගොනුවල වර්ගය සහ ප්‍රමාණය පද්ධතිය පරීක්ෂා කරයි. සැබෑ සේවාදායකයක යෙදවීමට පෙර HTTPS, උපස්ථ, SMTP, reCAPTCHA සහ සේවාදායක ආරක්ෂාව සකසන්න.

## සහාය

පද්ධතියේ **Support** පිටුවෙන් සකසා ඇති සහාය ඊමේල් ලිපිනය සහ දුරකථන අංකය බැලිය හැක.
