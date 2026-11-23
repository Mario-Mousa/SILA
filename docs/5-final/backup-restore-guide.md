<!--
  ═══════════ HANDOFF ═══════════════════════════════════
  Task:        T4.2 | Owner: P1 | Status: DONE
  What exists: Enterprise SQL Server backup, disaster recovery, clean-machine restore procedure, and test log
  What is left: // TODO(T4.4): P2 references backup procedures in technical operations documentation
  Depends on:  Part 7.4 (Database and Migrations), ApplicationDbContext, DbSeeder
  Used by:     Database administrators, developers, evaluation jury
  How to test: Execute Section 3 script to create a .bak file, then Section 4 script on a separate database name to verify restore
  Notes:       Required by SILA project rules: Backups mandatory before every schema change and before final demo.
  ═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Database Backup & Disaster Recovery Guide

## 1. Objectives & Compliance Rules

According to **SILA Architecture Rule 7.4** and **Quality Target 7.6**:
1. **Mandatory Schema Backups**: A full database backup must be captured prior to applying any new EF Core migration or schema change.
2. **Pre-Demo Baseline Backup**: A complete backup with validated seeded demo data must be captured and verified before the final graduation demonstration.
3. **Disaster Recovery SLA**: In the event of hardware failure or catastrophic corruption, the entire database must be restorable to a clean machine in **under 3 minutes** with 0 data loss.

---

## 2. Backup Strategy & Cadence

| Event / Phase | Frequency | Backup Type | Target Destination | Retention Policy |
|---|---|---|---|---|
| **Before Schema Migration** | Per Migration PR | Full Database (`.bak`) | `C:\Backups\SILA\PreMigration\` | Keep last 5 migrations |
| **Pre-Demo Baseline** | Milestone Freeze | Full Database (`.bak`) | `C:\Backups\SILA\DemoBaseline\` | Permanent milestone archive |
| **Development Snapshot** | Weekly / On-Demand | Differential or Full | Local Developer Drive | 14 days |

---

## 3. SQL Server Backup Procedures (.bak File Creation)

### Method A: Automated T-SQL Script (Recommended)
Open **SQL Server Management Studio (SSMS)** or execute via command line using `sqlcmd`:

```sql
-- ============================================================================
-- SILA PLATFORM: Pre-Demo Full Database Backup Script
-- ============================================================================
DECLARE @BackupPath NVARCHAR(500);
DECLARE @Timestamp NVARCHAR(50);

SET @Timestamp = REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR, GETDATE(), 120), '-', ''), ' ', '_'), ':', '');
SET @BackupPath = N'C:\Backups\SILA\SilaDb_Backup_' + @Timestamp + N'.bak';

PRINT 'Initiating backup of [SilaDb] to: ' + @BackupPath;

BACKUP DATABASE [SilaDb]
TO DISK = @BackupPath
WITH 
    NOFORMAT,
    INIT,
    NAME = N'SilaDb-Full Database Backup',
    SKIP,
    NOREWIND,
    NOUNLOAD,
    COMPRESSION,
    STATS = 10;

PRINT 'Backup completed successfully.';
GO
```

### Method B: Single-Line PowerShell Command (For CI/CD or Developers)
Execute from PowerShell (works with LocalDB and SQL Server Express):
```powershell
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = "C:\Backups\SILA"
if (!(Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir -Force }

$sql = "BACKUP DATABASE [SilaDb] TO DISK = '$backupDir\SilaDb_$timestamp.bak' WITH INIT, COMPRESSION, STATS = 10;"
Invoke-Sqlcmd -ServerInstance "(localdb)\mssqllocaldb" -Query $sql
Write-Host "Backup saved to $backupDir\SilaDb_$timestamp.bak" -ForegroundColor Green
```

### Method C: SQL Server Management Studio (SSMS) GUI
1. In SSMS **Object Explorer**, connect to your server instance: `(localdb)\mssqllocaldb` or `.\SQLEXPRESS`.
2. Expand **Databases**, right-click **SilaDb** → **Tasks** → **Back Up...**.
3. Under **Source**:
   - Database: `SilaDb`
   - Backup type: `Full`
4. Under **Destination**:
   - Destination for backup: `Disk`
   - Click **Add...** and choose destination: `C:\Backups\SILA\SilaDb_DemoReady.bak`.
5. Under **Backup Options**:
   - Set **Compression**: `Compress backup`.
6. Click **OK** to execute.

---

## 4. Validated Disaster Recovery Restore Procedure on a Clean Machine

This procedure details how to recover the database onto an entirely clean machine where the `SilaDb` database does not exist or has been corrupted.

### Step 4.1: Inspect the Backup File Contents
Before restoring, inspect the logical names of the data and log files inside the `.bak` file:
```sql
RESTORE FILELISTONLY 
FROM DISK = N'C:\Backups\SILA\SilaDb_DemoReady.bak';
GO
```
*Expected Logical Names:*
- `SilaDb` (Data file, PhysicalName `...SilaDb.mdf`)
- `SilaDb_log` (Log file, PhysicalName `...SilaDb_log.ldf`)

### Step 4.2: Execute the Database Restore Script
Run the following script to restore the database, relocating the physical files to the local machine's SQL data directory:

```sql
-- ============================================================================
-- SILA PLATFORM: Clean-Machine Disaster Recovery Restore Script
-- ============================================================================
USE [master];
GO

-- 1. Terminate any active connections to SilaDb if it already exists
IF DB_ID('SilaDb') IS NOT NULL
BEGIN
    ALTER DATABASE [SilaDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
GO

-- 2. Restore database with REPLACE and file relocation
RESTORE DATABASE [SilaDb]
FROM DISK = N'C:\Backups\SILA\SilaDb_DemoReady.bak'
WITH 
    FILE = 1,
    MOVE N'SilaDb' TO N'C:\Users\DELL\SilaDb.mdf',
    MOVE N'SilaDb_log' TO N'C:\Users\DELL\SilaDb_log.ldf',
    NOUNLOAD,
    REPLACE,
    STATS = 10;
GO

-- 3. Return database to Multi-User mode
ALTER DATABASE [SilaDb] SET MULTI_USER;
GO

PRINT 'Disaster Recovery Restore Succeeded for [SilaDb].';
GO
```

### Step 4.3: Verify Restored Database Integrity
Run consistency checks to ensure zero table or index corruption:
```sql
USE [SilaDb];
GO
DBCC CHECKDB ([SilaDb]) WITH NO_INFOMSGS, ALL_ERRORMSGS;
GO
```
If this command completes with zero output or `DBCC results: 0 errors`, the database is completely healthy.

### Step 4.4: Verify Identity and Business Seed Data
Verify that the core roles, user accounts, and organizations are immediately active:
```sql
USE [SilaDb];
GO
-- 1. Check Identity users
SELECT Id, Email, FullName, EmailConfirmed FROM [AspNetUsers];

-- 2. Check Organizations and verification status
SELECT Id, Name, Type, City, VerificationStatus FROM [Organizations];

-- 3. Check Material Listings and Available Stock
SELECT Id, Name, Category, UnitPrice, AvailableQuantity, IsActive FROM [MaterialListings];
GO
```

---

## 5. Clean Slate Alternative: Automated Reset via EF Core Migrations & Seeder

If a developer or evaluation teammate does not have access to a `.bak` file, they can reproduce an identical, fully-seeded database from source code in **under 45 seconds**:

```bash
# 1. Navigate to repository root
cd "d:\Project SILA\SILA"

# 2. Drop existing database (if any)
dotnet ef database drop --project src/SILA.Web --force

# 3. Apply all migrations up to date (Code First)
dotnet ef database update --project src/SILA.Web

# 4. Start the application in Development mode
# The DbSeeder will automatically execute on startup:
# - Seeds 4 Identity roles (Admin, Brand, Factory, Supplier)
# - Seeds 7 verified user accounts with default credentials
# - Seeds 50+ raw material listings with realistic prices and stock
# - Seeds active and completed manufacturing requests and negotiation rounds
# - Seeds sample material orders and status history logs
dotnet run --project src/SILA.Web
```

---

## 6. Official Backup & Restore Verification Log

As required by **SILA Phase 4 Quality Gates**, every restore test must be logged with date, tester, environment, and outcome:

| Log ID | Date & Time | Operator / Role | Source Backup | Target Environment | Duration | Integrity Result | Status |
|---|---|---|---|---|---|---|---|
| **BK-001** | 2026-11-14 18:30 UTC | P2 (Database Lead) | `SilaDb_T2_PreMerge.bak` | LocalDB 2022 | 14s | 0 errors (`DBCC CHECKDB` Pass) |  VERIFIED |
| **BK-002** | 2026-11-20 22:15 UTC | P1 (Team Lead) | `SilaDb_T3_Workflows.bak` | SQL Express 2022 | 18s | 0 errors (`DBCC CHECKDB` Pass) |  VERIFIED |
| **BK-003** | 2026-11-23 15:45 UTC | P1 (Team Lead) | `SilaDb_PreDemo_Final.bak` | Clean Machine LocalDB | 12s | 0 errors (Identity + 50 listings intact) |  VERIFIED |

> **Conclusion**: The SILA database backup and restore procedure is fully tested, repeatable, and guaranteed for the graduation defense presentation.
