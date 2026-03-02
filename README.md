
# MarFin — Marketing & Finance CRM

A cross-platform CRM (Customer Relationship Management) application built with **.NET MAUI Blazor Hybrid** targeting Windows, Android, iOS, and macOS. MarFin integrates Marketing campaign management, Sales pipeline tracking, and Finance/invoicing into a single role-based system backed by Microsoft SQL Server.

---

## Table of Contents

1. [Features](#features)
2. [Tech Stack](#tech-stack)
3. [Role & Permission Matrix](#role--permission-matrix)
4. [Project Structure](#project-structure)
5. [Prerequisites](#prerequisites)
6. [Database Setup](#database-setup)
7. [Application Configuration](#application-configuration)
8. [Running the Application](#running-the-application)
9. [Building for Release](#building-for-release)
10. [Default Credentials](#default-credentials)
11. [NuGet Packages](#nuget-packages)
12. [Troubleshooting](#troubleshooting)

---

## Features

| Module | Description |
|---|---|
| **Dashboard** | Role-specific KPI cards, revenue trend charts, lead sources, segment breakdown, pipeline stage view, and campaign performance charts |
| **Customers** | Customer list, detailed profiles, segmentation, and interaction history |
| **Sales Pipeline** | Kanban-style deal tracking across pipeline stages with win-rate analytics |
| **Marketing** | Campaign creation, email tracking, open-rate metrics, and customer targeting |
| **Financial** | Invoice management, payment status tracking, and financial overview (OHLC chart) |
| **Reports** | Cross-role downloadable reports with date-range filtering |
| **Documents** | Document storage and management (Admin) |
| **Audit Log** | Full activity log of every user action — create, update, delete, login/logout — with before/after change diff (Admin only) |
| **Settings** | Account management, data sync configuration, and remote database connection |

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | .NET MAUI Blazor Hybrid |
| Language | C# 13 / .NET 9 |
| Frontend | Razor Components, Bootstrap Icons, Chart.js (via JS Interop) |
| ORM / Data | Entity Framework Core 9 + raw `Microsoft.Data.SqlClient` |
| Database | Microsoft SQL Server (LocalDB for dev, cloud for production) |
| PDF Export | QuestPDF |
| Spreadsheet | ClosedXML |
| Target Platforms | Windows 10+, Android 7+, iOS 15+, macOS 12+ |

---

## Role & Permission Matrix

| Permission | Admin | Finance | Marketing | Sales Rep |
|---|:---:|:---:|:---:|:---:|
| Dashboard | ✅ | ✅ | ✅ | ✅ |
| Customers | ✅ | | ✅ | ✅ |
| Sales Pipeline | ✅ | | | ✅ |
| Marketing / Campaigns | ✅ | | ✅ | |
| Financial / Invoices | ✅ | ✅ | | |
| Transactions | ✅ | ✅ | | |
| Reports | ✅ | ✅ | ✅ | ✅ |
| Documents | ✅ | | | |
| Audit Log | ✅ | | | |
| Settings | ✅ | | | |
| Customer Segments | ✅ | | ✅ | |

---

## Project Structure

```
MarFin_Final/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor       # Authenticated shell with sidebar & top bar
│   │   ├── LoginLayout.razor      # Unauthenticated shell
│   │   └── NavMenu.razor          # Role-aware sidebar navigation
│   └── Pages/
│       ├── Login/                 # Login & registration pages
│       ├── Main/
│       │   ├── Dashboard.razor    # Role-specific dashboard
│       │   ├── AuditLog.razor     # Admin audit log viewer
│       │   └── ...
│       ├── Modules/               # Customers, Sales, Marketing, Financial, etc.
│       └── Reports/
├── Database/
│   ├── Models/                    # Plain C# model classes
│   └── Services/                  # Data-access services (ADO.NET + EF Core)
│       ├── AuthService.cs
│       ├── AuditLogService.cs
│       ├── DashboardService.cs
│       ├── RolePermission.cs
│       └── ...
├── Data/
│   └── AppDbContext.cs            # EF Core DbContext
├── wwwroot/
│   ├── css/                       # Global styles
│   ├── js/                        # Chart.js interop helpers
│   └── img/                       # Logo & static images
├── appsettings.json               # Active connection strings (gitignored)
├── appsettingsExample.example.json # Template — copy & rename to appsettings.json
├── db_marfin.sql                  # Full database creation script
└── MarFin_Final.csproj
```

---

## Prerequisites

Make sure the following are installed before setting up the project.

### Required Software

| Tool | Version | Download |
|---|---|---|
| Visual Studio 2022 | 17.8+ | [visualstudio.microsoft.com](https://visualstudio.microsoft.com/) |
| .NET 9 SDK | 9.0+ | [dot.net](https://dotnet.microsoft.com/en-us/download/dotnet/9) |
| SQL Server / LocalDB | 2019+ | Included with VS or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) |
| SQL Server Management Studio (SSMS) | *(optional)* | [aka.ms/ssmsfullsetup](https://aka.ms/ssmsfullsetup) |

### Visual Studio Workloads

Open the **Visual Studio Installer** and ensure these workloads are installed:

- **.NET Multi-platform App UI development** (MAUI)
- **ASP.NET and web development**
- **Mobile development with .NET**

> On first open, VS will prompt to install MAUI workloads automatically if missing.

---

## Database Setup

### 1. Restore the database from the SQL script

Open **SQL Server Management Studio (SSMS)** or use `sqlcmd`, then run:

```sql
-- In SSMS: File > Open > db_marfin.sql, then Execute (F5)
```

Or via command line:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "path\to\db_marfin.sql"
```

This will create the `DB_CRM_MarFin` database with all tables, indexes, constraints, and seed data.

### 2. Verify the database

After running the script, confirm the following tables exist:

| Table | Purpose |
|---|---|
| `tbl_Users` | User accounts |
| `tbl_Roles` | Roles (Admin, Finance, Marketing, Sales Representative) |
| `tbl_Customers` | Customer master data |
| `tbl_Campaigns` | Marketing campaigns |
| `tbl_Invoices` | Finance invoices |
| `tbl_Transactions` | Payment transactions |
| `tbl_SalesPipeline` | Sales opportunities |
| `tbl_Activity_Log` | Audit trail |

---

## Application Configuration

### 1. Copy the example config file

```powershell
cd MarFin_Final\MarFin_Final
Copy-Item appsettingsExample.example.json appsettings.json
```

### 2. Edit `appsettings.json`

Open `appsettings.json` and update the connection strings to match your environment:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=DB_CRM_MarFin;Integrated Security=True;Encrypt=True",
    "CloudConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASS;Encrypt=True;TrustServerCertificate=True;"
  }
}
```

| Key | Description |
|---|---|
| `DefaultConnection` | Local SQL Server / LocalDB — used for day-to-day development |
| `CloudConnection` | Remote SQL Server — used by the Data Sync feature in Settings |

> **Note:** `appsettings.json` is embedded as a resource at build time. Any change requires a rebuild.

---

## Running the Application

### Windows (recommended for development)

1. Open `MarFin_Final.sln` in Visual Studio 2022
2. Set the startup project to **MarFin_Final**
3. Select the **Windows Machine** target from the run dropdown
4. Press **F5** (Debug) or **Ctrl+F5** (Run without debugging)

### Android (emulator or device)

1. Enable **Developer Options** and **USB Debugging** on a physical device, or create an Android Virtual Device in Android Device Manager
2. Select your device/emulator from the run dropdown
3. Press **F5**

### iOS / macOS

> Requires a Mac with Xcode installed, or Visual Studio for Mac.

1. Pair your Mac with Visual Studio via **Tools → iOS → Pair to Mac**
2. Select an iOS simulator or connected device
3. Press **F5**

### CLI (Windows)

```powershell
cd MarFin_Final\MarFin_Final
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

---

## Building for Release

### Windows MSIX / Unpackaged

```powershell
dotnet publish -f net9.0-windows10.0.19041.0 -c Release
```

Output is placed in `bin\Release\net9.0-windows10.0.19041.0\`.

### Android APK

```powershell
dotnet publish -f net9.0-android -c Release
```

### iOS

```powershell
dotnet publish -f net9.0-ios -c Release
```

---

## Default Credentials

After running `db_marfin.sql` the following seed accounts are available:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@marfin.com` | *(set during DB seed)* |
| Finance | `finance@marfin.com` | *(set during DB seed)* |
| Marketing | `marketing@marfin.com` | *(set during DB seed)* |
| Sales Rep | `sales@marfin.com` | *(set during DB seed)* |

> Passwords must be at least **12 characters** and contain uppercase, lowercase, a number, and a special character (enforced by `AuthService`).

> New accounts can be registered through the **Register** page or created in SSMS directly in `tbl_Users`.

---

## NuGet Packages

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Maui.Controls` | 9.x | MAUI core |
| `Microsoft.AspNetCore.Components.WebView.Maui` | 9.x | Blazor Hybrid host |
| `Microsoft.Data.SqlClient` | 6.1.3 | ADO.NET SQL Server driver |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.1 | EF Core ORM |
| `Microsoft.AspNetCore.Components.Authorization` | 9.0.10 | Blazor auth |
| `QuestPDF` | 2025.12.0 | PDF generation (invoices/reports) |
| `ClosedXML` | 0.105.0 | Excel export |
| `Microsoft.Extensions.Configuration.Json` | 10.0.0 | JSON config loading |

All packages are restored automatically by Visual Studio or via:

```powershell
dotnet restore
```

---

## Troubleshooting

### "Connection string not found"

- Ensure `appsettings.json` exists in `MarFin_Final\MarFin_Final\`
- Rebuild the project — the file is an **embedded resource** and must be compiled in

### LocalDB not connecting

```powershell
# Start LocalDB instance
sqllocaldb start MSSQLLocalDB

# Check instance info
sqllocaldb info MSSQLLocalDB
```

### MAUI workload missing

```powershell
dotnet workload install maui
```

### Android emulator not detected

- Open **Android Device Manager** (Tools → Android → Android Device Manager) and start a virtual device
- Ensure HAXM or Hyper-V is enabled in BIOS

### "Database does not exist"

Run `db_marfin.sql` against your SQL Server instance (see [Database Setup](#database-setup)).

### Hot reload not working on Windows

Ensure **Hot Reload** is enabled under **Debug → Hot Reload Options** in Visual Studio.

---

## License

This project is developed as an academic capstone project by **IT13 — MarFin Team**.  
All rights reserved © 2025–2026.
