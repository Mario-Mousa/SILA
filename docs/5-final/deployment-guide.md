<!--
  ═══════════ HANDOFF ═══════════════════════════════════
  Task:        T4.2 | Owner: P1 | Status: DONE
  What exists: Comprehensive production deployment guide, cloud/on-premise hosting procedures, safe secrets management, and offline demo fallback
  What is left: // TODO(T4.4): P2 references this guide in final technical documentation and presentation slides
  Depends on:  Phase 3 workflows (T3.1, T3.2, T3.3, T3.4), T4.1 test suite
  Used by:     DevOps engineers, evaluation jury, system administrators
  How to test: Follow Section 3 or Section 4 to publish the project; verify HTTP/HTTPS endpoints respond with status 200
  Notes:       BR-00: Zero secrets committed to source control. Production configuration is injected strictly via environment variables or cloud secret vaults.
  ═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Production Deployment & Operations Guide

## 1. Executive Summary & Architecture

**SILA (صِلة)** is an enterprise B2B platform engineered for the Egyptian fashion and apparel manufacturing ecosystem. It digitizes interactions between **Brand Owners**, **Garment Factories**, and **Raw Material Suppliers**.

### Production Topology Overview
```
                 [ Internet / Client Browser ]
                                │ (HTTPS / TLS 1.3)
                                ▼
                   [ Reverse Proxy / Ingress ]
                   (IIS ARR / Azure Front Door)
                                │
        ┌───────────────────────┴───────────────────────┐
        ▼                                               ▼
[ Web Application Host ]                    [ Primary Database Host ]
  • ASP.NET Core 10 MVC                       • Microsoft SQL Server 2022
  • Kestrel / IIS In-Process                  • Read/Write Transactional Store
  • Static Assets (wwwroot)                   • Automated Daily .bak Snapshots
        │                                               │
        └─────────────── [ Local File Storage ] ────────┘
                          • Verification Documents
                          • Tech-pack Attachments
```

---

## 2. System Requirements & Prerequisites

### 2.1 Hardware Requirements (Minimum vs. Recommended)
| Resource | Minimum (Staging / Demo) | Recommended (Production Enterprise) |
|---|---|---|
| **CPU** | 2 Cores (x64, 2.0 GHz+) | 4+ Cores (x64, 2.5 GHz+ e.g., Intel Xeon / AMD EPYC) |
| **RAM** | 4 GB | 8 GB – 16 GB |
| **Disk Space** | 20 GB SSD (OS + App + Logs) | 100+ GB NVMe SSD with RAID 1/10 |
| **Bandwidth** | 10 Mbps symmetrical | 100+ Mbps symmetrical |

### 2.2 Software Prerequisites
1. **Operating System**: Windows Server 2022 / Windows Server 2019, or modern 64-bit Linux (Ubuntu 22.04 LTS+).
2. **Runtime Engine**: [.NET 10.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0) (includes .NET Runtime and `AspNetCoreModuleV2` for IIS).
3. **Database Server**: Microsoft SQL Server 2019 / 2022 Standard/Enterprise, Azure SQL Database, or SQL Server Express 2022.
4. **Web Server**: Internet Information Services (IIS) 10.0+ with URL Rewrite 2.1 Module, or Nginx (for Linux reverse proxy).
5. **SSL Certificate**: Valid TLS certificate (Let's Encrypt, DigiCert, or Azure Managed Certificate).

---

## 3. Hosting Option A: Windows Server & IIS (Recommended On-Premise)

### Step 3.1: Install IIS and the .NET 10 Hosting Bundle
1. Open PowerShell as Administrator on the target Windows Server and install IIS features:
   ```powershell
   Install-WindowsFeature -name Web-Server, Web-WebServer, Web-Common-Http, Web-Static-Content, Web-Default-Doc, Web-Dir-Browsing, Web-Http-Errors, Web-App-Dev, Web-Net-Ext45, Web-ISAPI-Ext, Web-ISAPI-Filter, Web-Filtering, Web-Stat-Compression, Web-Dyn-Compression, Web-Mgmt-Console -IncludeManagementTools
   ```
2. Download and install the **.NET 10 Hosting Bundle**.
3. Restart IIS via command prompt:
   ```cmd
   net stop was /y
   net start w3svc
   ```

### Step 3.2: Compile and Publish the Application
From the repository root on your build machine or build pipeline:
```bash
dotnet restore src/SILA.sln
dotnet publish src/SILA.Web/SILA.Web.csproj -c Release -o C:\inetpub\sila-web --self-contained false -r win-x64
```

### Step 3.3: Configure the IIS Application Pool & Website
1. Open **Internet Information Services (IIS) Manager**.
2. Under **Application Pools**, click **Add Application Pool**:
   - **Name**: `SilaAppPool`
   - **.NET CLR Version**: `No Managed Code`
   - **Managed pipeline mode**: `Integrated`
3. Click **OK**, then right-click `SilaAppPool` → **Advanced Settings**:
   - **Identity**: `ApplicationPoolIdentity` (or dedicated service account).
   - **Idle Time-out (minutes)**: `0` (prevents worker process sleep).
   - **Start Mode**: `AlwaysRunning`.
4. Under **Sites**, click **Add Website**:
   - **Site name**: `SILA-Production`
   - **Application pool**: `SilaAppPool`
   - **Physical path**: `C:\inetpub\sila-web`
   - **Binding**: Type `https`, Port `443`, Host name `sila.fashion` (or your domain/IP), assign SSL certificate.
   - Add HTTP port 80 binding with automatic HTTPS redirection.

### Step 3.4: Grant Filesystem Permissions
Ensure the IIS application pool identity has read access to the application binaries and write access to the uploads directory:
```powershell
icacls "C:\inetpub\sila-web" /grant "IIS AppPool\SilaAppPool":(OI)(CI)RX /T
icacls "C:\inetpub\sila-web\wwwroot\uploads" /grant "IIS AppPool\SilaAppPool":(OI)(CI)M /T
icacls "C:\inetpub\sila-web\logs" /grant "IIS AppPool\SilaAppPool":(OI)(CI)M /T
```

---

## 4. Hosting Option B: Microsoft Azure App Service (Recommended Cloud)

### Step 4.1: Provision Azure Cloud Resources
1. **Resource Group**: `rg-sila-production-egypt`
2. **App Service Plan**: Linux or Windows B1 (Basic) or S1 (Standard) in North Europe / West Europe / UAE North.
3. **Azure SQL Database**: Provision General Purpose (Serverless or 10 DTUs) database named `sqldb-sila-prod`.
   - Set Collation: `SQL_Latin1_General_CP1_CI_AS`.
   - Add client firewall rule to allow Azure services to access server.

### Step 4.2: Deploy via Azure CLI or GitHub Actions
Using the Azure CLI:
```bash
# 1. Login to Azure
az login

# 2. Package published folder into ZIP
cd C:\inetpub\sila-web
Compress-Archive -Path * -DestinationPath ..\sila-deploy.zip

# 3. Deploy ZIP package
az webapp deploy --resource-group rg-sila-production-egypt --name sila-b2b --src-path ..\sila-deploy.zip --type zip
```

---

## 5. Safe Configuration of Production Secrets (Rule BR-00)

> [!CAUTION]
> **Zero Secrets in Git**: Connection strings, SMTP credentials, and API tokens must **NEVER** be committed to Git or stored in `appsettings.json`.

### 5.1 Environment Variables Configuration Table
Set these environment variables on the production server (Windows Environment Variables, Azure App Settings, or systemd daemon config):

| Variable Key | Description | Example Production Value |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Application Environment | `Production` |
| `ConnectionStrings__DefaultConnection` | SQL Server Production Connection String | `Server=tcp:sql-sila.database.windows.net,1433;Database=SilaDb;User ID=sila_admin;Password=YourSecurePassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` |
| `Smtp__Host` | SMTP Server hostname | `smtp.sendgrid.net` or `smtp.office365.com` |
| `Smtp__Port` | SMTP Port | `587` |
| `Smtp__SenderEmail` | Public Sender Email Address | `noreply@sila.fashion` |
| `Smtp__SenderName` | Email Display Name | `SILA Platform` |
| `Smtp__Username` | SMTP Account User | `apikey` |
| `Smtp__Password` | SMTP Password / Key | `SG.xxxxxxxxxxxxxxxxxxxx` |

### 5.2 Setting Environment Variables in Windows IIS
```powershell
# Set machine-level environment variable (requires server reboot or app pool restart)
[Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=localhost;Database=SilaDb;Trusted_Connection=True;TrustServerCertificate=True;", "Machine")
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
```

---

## 6. Database Production Migrations & Schema Deployment

In production, avoid running `context.Database.Migrate()` directly in application startup to prevent concurrency deadlocks across scaled instances. Instead, generate and execute idempotent SQL migration scripts:

### Generating Idempotent SQL Script
```bash
# From repository root
dotnet ef migrations script --idempotent --output ./publish/migrations_bundle.sql --project src/SILA.Web
```

### Applying Script on Target SQL Server
Execute the generated `migrations_bundle.sql` via SQL Server Management Studio (SSMS) or `sqlcmd`:
```cmd
sqlcmd -S tcp:localhost,1433 -d SilaDb -U sila_admin -P "YourPassword" -i ./publish/migrations_bundle.sql
```

---

## 7. Emergency Local Fallback Procedure (Graduation Demo Contingency)

> [!IMPORTANT]
> **Demo Reliability Protocol**: If internet connectivity drops, cloud hosting undergoes maintenance, or conference Wi-Fi blocks ports during the live DEPI presentation, execute this 2-minute offline fallback procedure.

### Step 1: Pre-Demo Local Machine Setup (Done 24h Before Presentation)
1. Ensure the presenter laptop has:
   - .NET 10.0 SDK installed.
   - Microsoft SQL Server LocalDB or SQL Server Express active.
   - Database pre-migrated: `dotnet ef database update --project src/SILA.Web`.
   - Database pre-seeded with all 7 accounts, 50+ material listings, sample orders, and negotiation requests (verified via `DbSeeder.cs`).
2. Generate an offline database backup file:
   ```cmd
   sqlcmd -S "(localdb)\mssqllocaldb" -Q "BACKUP DATABASE [SilaDb] TO DISK = 'C:\SILA_Backups\SilaDb_DemoReady.bak' WITH INIT;"
   ```

### Step 2: Instant Activation During Live Presentation
If cloud connection fails during presentation:
1. Open PowerShell terminal in `d:\Project SILA\SILA`:
   ```bash
   dotnet run --project src/SILA.Web --urls "http://localhost:5093"
   ```
2. Open Google Chrome / Microsoft Edge and navigate to:
   ```
   http://localhost:5093
   ```
3. Use the verified demo accounts documented in `README.md` (`admin@sila.com`, `brand1@sila.com`, `factory1@sila.com`, `supplier1@sila.com`).
4. **All operations remain 100% functional locally**: in-app notifications, status history transitions, atomic stock deductions, and offer rounds require zero external network calls.

---

## 8. Pre-Flight Production & Demo Checklist (15 Points)

Before declaring production readiness or beginning the final jury presentation:

- [ ] **1. Clean Build**: `dotnet build src/SILA.sln` outputs 0 warnings and 0 errors.
- [ ] **2. Automated Tests Passing**: `dotnet test tests/SILA.Tests/SILA.Tests.csproj` passes 100% of tests (46/46).
- [ ] **3. Database Migrations Synchronized**: `dotnet ef database update` reports no pending migrations.
- [ ] **4. Database Seed Data Present**: All 4 roles and 7 default accounts are accessible with known demo credentials.
- [ ] **5. Concurrency Control Verified**: `MaterialListing.RowVersion` enforces optimistic concurrency on material orders and edits.
- [ ] **6. Atomic Stock Verified**: Multi-line material orders deduct stock in a single transaction; overselling is impossible.
- [ ] **7. Record-Level Security (BR-07)**: Attempting to access another organization's order or request returns 403 Forbidden.
- [ ] **8. Business Rules Enforced**: Unit prices > 0, available quantities >= 0, single-supplier orders (BR-02), and out-of-stock badges at 0 units (BR-06).
- [ ] **9. Responsive Design Validated**: Interface verified at desktop (1920×1080), laptop (1366×768), and tablet (768×1024).
- [ ] **10. HTTPS & Security Headers**: SSL certificate valid; HSTS, X-Content-Type-Options, and X-Frame-Options enabled in `web.config`.
- [ ] **11. File Upload Sandbox**: File size limit capped at 10 MB per document; MIME types restricted to PDF, PNG, JPEG.
- [ ] **12. Error Pages**: Friendly error views displayed on HTTP 404 and 500 without leaking stack traces.
- [ ] **13. Fresh Backup Generated**: `.bak` snapshot saved and logged in `docs/5-final/backup-restore-guide.md`.
- [ ] **14. Offline Fallback Ready**: Presenter machine verified to launch `http://localhost:5093` within 10 seconds.
- [ ] **15. Release Tagged**: Release tag (e.g. `v1.0.0`) committed on the `main` branch on GitHub.
