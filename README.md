<!--
  ═══════════ HANDOFF ═══════════════════════════════════
  Task:        T4.2 | Owner: P1 | Status: DONE
  What exists: Professional, comprehensive, production-ready README for SILA B2B platform with full setup guide, seeded accounts, workflows, and demo instructions
  What is left: // TODO(T4.4): P2 links final documentation assets and presentation decks
  Depends on:  All Phase 1, Phase 2, Phase 3, and Phase 4 implementations
  Used by:     DEPI evaluation committee, developers, testers, new contributors
  How to test: Follow Section 3 step-by-step on a clean clone to restore, migrate, seed, and run the project
  Notes:       Part 0 Compliance: All accounts and passwords listed are strictly for local development and academic evaluation.
  ═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — B2B Egyptian Fashion Supply Chain Platform

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-10.0-512BD4?logo=csharp&logoColor=white)](https://learn.microsoft.com/ef/)
[![SQL Server](https://img.shields.io/badge/Database-SQL_Server_2022-CC292B?logo=microsoft-sql-server&logoColor=white)](https://www.microsoft.com/sql-server)
[![Bootstrap 5](https://img.shields.io/badge/Frontend-Bootstrap_5.3-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![License: Academic](https://img.shields.io/badge/DEPI-Full_Stack_.NET-153D35)](https://depi.gov.eg)

> **SILA (صِلة)** is a specialized enterprise B2B platform tailored for the Egyptian textile and fashion manufacturing ecosystem. It bridges the gap between **Brand Owners**, **Garment Manufacturing Factories**, and **Raw Material Suppliers** into a centralized, transparent digital marketplace.

Developed as the capstone graduation project for the **Digital Egypt Pioneers Initiative (DEPI) — Full Stack .NET Track**.

---

## 1. Executive Summary & Value Proposition

The Egyptian garment and textile industry represents one of the nation's most historic and economically vital sectors. However, procurement and contract manufacturing have traditionally suffered from fragmented communication, opaque pricing, high sourcing friction, and inventory overselling.

**SILA solves these challenges through:**
- **Verified Business Profiles**: Comprehensive verification workflow by platform administrators ensuring authentic Egyptian brands, licensed factories, and certified textile suppliers.
- **Atomic Stock Management**: Zero overselling guarantee with single-transaction stock deductions and optimistic concurrency control (**BR-03**).
- **Two Independent Core Workflows**: Clear architectural separation between physical **Material Orders** and multi-round **Manufacturing Requests**.
- **Enterprise Audit Trail**: Append-only status histories and in-app realtime notifications on every state transition.

---

## 2. Technology Stack & Architectural Principles

| Layer | Technologies & Frameworks | Key Highlights |
|---|---|---|
| **Runtime & Framework** | C# 14 / **.NET 10.0** / ASP.NET Core MVC | High-performance, cross-platform web application |
| **Data Access & ORM** | **Entity Framework Core 10.0** (Code-First) | Strongly-typed migrations, `RowVersion` concurrency tokens, check constraints |
| **Database Engine** | **Microsoft SQL Server** (LocalDB / Express / Azure SQL) | Relational integrity, ACID transactions, isolated test databases |
| **Security & Auth** | **ASP.NET Core Identity** | PBKDF2 hashed passwords, role-based security (`Admin`, `Brand`, `Factory`, `Supplier`), record-level ownership (**BR-07**) |
| **Frontend & UI** | **Razor Views**, HTML5, CSS3, JavaScript, **Bootstrap 5.3** | Custom design tokens (Forest `#153D35`, Sand `#F5F1E9`, Clay `#C66B4E`), responsive across 1920×1080, 1366×768, and 768×1024 |
| **Testing & Quality** | **xUnit**, FluentAssertions, Moq, Testcontainers | 46 comprehensive unit, integration, authorization, and concurrency test cases |

---

## 3. Getting Started from a Clean Clone

### 3.1 Prerequisites
Ensure the following tools are installed on your machine:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (verify with `dotnet --version`).
- **Microsoft SQL Server** (SQL Server 2019/2022, SQL Server Express, or `(localdb)\mssqllocaldb`).
- **EF Core Global Tool** (Install via: `dotnet tool install --global dotnet-ef`).
- Modern Web Browser (Google Chrome, Microsoft Edge, or Mozilla Firefox).

### 3.2 Step-by-Step Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Mario-Mousa/SILA.git
   cd SILA
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore src/SILA.sln
   ```

3. **Apply Database Migrations:**
   ```bash
   dotnet ef database update --project src/SILA.Web
   ```
   *Note: This creates the `SilaDb` database in your local SQL Server instance and executes all schema migrations.*

4. **Launch the Application:**
   ```bash
   dotnet run --project src/SILA.Web
   ```

5. **Access SILA in Your Browser:**
   - **HTTP URL**: [`http://localhost:5093`](http://localhost:5093)
   - **HTTPS URL**: [`https://localhost:7246`](https://localhost:7246)

*Upon initial startup in the `Development` environment, the built-in `DbSeeder` automatically initializes all Identity roles, 7 verified business organizations, 50+ raw material catalog listings, and realistic orders/requests.*

---

## 4. Default Seeded Accounts (For Evaluation & Demo)

All seeded accounts come pre-configured with confirmed emails and verified organizational status:

| Role | Email Address | Password | Contact Person | Organization Name | Location | Primary Actions in Platform |
|---|---|---|---|---|---|---|
| **Admin** | `admin@sila.com` | `Admin@123456` | Platform Admin | **SILA Operations** | Cairo | Review verification documents, approve/reject organizations |
| **Brand Owner** | `brand1@sila.com` | `Brand@123456` | Ahmed Mansour | **Nile Chic Apparel** | Cairo | Browse materials, order fabrics, submit manufacturing requests |
| **Brand Owner** | `brand2@sila.com` | `Brand@123456` | Laila El-Gohary | **Lotus Haute Couture** | Giza | Negotiate offer rounds with factories, track incoming deliveries |
| **Factory Owner**| `factory1@sila.com`| `Factory@123456`| Mahmoud Soliman | **Delta Garments Co.** | 10th of Ramadan | Review requests, submit counter-offers, purchase raw materials |
| **Factory Owner**| `factory2@sila.com`| `Factory@123456`| Nader El-Sayed | **Alex Modern Garments**| Borg El Arab | Manage production queue, negotiate request pricing |
| **Supplier** | `supplier1@sila.com`| `Supplier@123456`| Mostafa Radwan | **Misr Fine Weaving** | El-Mahalla | Publish fabrics/yarns, manage stock, confirm/dispatch orders |
| **Supplier** | `supplier2@sila.com`| `Supplier@123456`| Tarek Abdel-Rahman| **Al-Ahram Textiles** | Shubra El-Kheima| Catalog management, atomic order stock confirmation |

---

## 5. The Two Independent Core Workflows

SILA strictly segregates manufacturing negotiations from raw material purchasing:

### Workflow A: Manufacturing Request (Brand Owner ↔ Garment Factory)
```
[ Brand Owner: Draft & Submit Request ] 
                 │
                 ▼
     [ Status: Submitted / UnderReview ]
                 │
      ┌──────────┴────────────────────────┐
      ▼                                   ▼
 [ Factory Accepts ]               [ Factory Counters ]
      │                                   │
      ▼                                   ▼
 [ Status: Accepted ]          [ Negotiation Rounds ]
 (Ready for Production)        (Brand Accept / Reject / Revise)
```
- **Rules**: Zero stock impact; multiple negotiation rounds stored with actor, proposed price, and notes (`RequestOffer`); only the sender Brand and target Factory can access the record (**BR-07**).

### Workflow B: Raw Material Orders (Buyer ↔ Supplier)
```
[ Buyer (Brand/Factory): Browse Catalog (EGP) ]
                       │
                       ▼
         [ Submit Order: PendingReview ]
                       │
                       ▼
      [ Supplier Confirmation (Atomic Transaction) ]
        ├── Re-verify stock for every order line
        ├── Deduct AvailableQuantity (BR-03)
        ├── Record StockMovement audit row
        └── Transition to: Confirmed
                       │
                       ▼
     [ Dispatched ] ──────▶ [ Delivered ]
```
- **Rules**: Exactly one Supplier per order (**BR-02**); unit prices and units snapshotted per line (**BR-04**); out-of-stock badge when stock is 0 (**BR-06**); referenced listings can never be hard-deleted (**BR-05**).

---

## 6. Security, Configuration & Secrets Management (BR-00)

SILA strictly enforces that **zero secrets or production credentials are ever committed to version control**.

### 6.1 Local Development (User Secrets)
To configure local overrides without modifying tracked files:
```bash
dotnet user-secrets init --project src/SILA.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=SilaDb;Trusted_Connection=True;" --project src/SILA.Web
dotnet user-secrets set "Smtp:Password" "YourDevelopmentSmtpPassword" --project src/SILA.Web
```

### 6.2 Production Environment Variables
In production, configuration is supplied via environment variables:
- `ConnectionStrings__DefaultConnection`: Production SQL Server connection string.
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`: SMTP transport settings.

For full hosting instructions on IIS and Azure App Service, consult the [Production Deployment Guide](file:///docs/5-final/deployment-guide.md).

---

## 7. Testing & Quality Assurance

SILA features a comprehensive test suite in `tests/SILA.Tests/` covering negative authorization, business rule validation, shared services, and multi-threaded concurrency:

```bash
# Run all automated tests
dotnet test tests/SILA.Tests/SILA.Tests.csproj
```

**Key Verified Acceptance Criteria:**
- **Negative Authorization**: Non-owners attempting to access or modify orders, listings, or requests receive HTTP 403 Forbidden (**BR-07**).
- **Concurrency & Anti-Overselling**: Two simultaneous confirmations of 15 units against 20 available units result in exactly one success and one rejection; stock never becomes negative (**BR-03**).
- **Listing Soft-Deactivation**: Deactivated listings remain preserved for historical orders (**BR-05**).

---

## 8. Backup & Disaster Recovery

Backups are mandatory before any schema change and prior to presentations:
- **T-SQL Backup**:
  ```sql
  BACKUP DATABASE [SilaDb] TO DISK = N'C:\Backups\SILA\SilaDb_PreDemo.bak' WITH INIT, COMPRESSION;
  ```
- **Disaster Recovery Restore**: Step-by-step restoration procedures for clean machines are detailed in the [Database Backup & Restore Guide](file:///docs/5-final/backup-restore-guide.md).

---

## 9. Live Deployment & Pre-Demo Checklist

- **Live Staging URL**: `http://localhost:5093` (Local Staging) / `https://sila-b2b.azurewebsites.net` (Cloud Staging)
- **Pre-Demo Checklist**:
  1. [x] Clean solution build: `dotnet build src/SILA.sln` outputs 0 errors.
  2. [x] All 46 automated unit and concurrency tests passing.
  3. [x] Local SQL Server / LocalDB running and migrated up to date.
  4. [x] Seed data confirmed: 7 accounts, 50+ listings, active demo requests.
  5. [x] Browser bookmarks configured for Admin, Brand, Factory, and Supplier logins.
  6. [x] Offline fallback ready in case of conference Wi-Fi disconnection.

---

## 10. Project Structure & Repository Organization

```
SILA/
├── docs/
│   ├── 1-planning/             # Agile planning, Gantt schedule, team sprint logs
│   ├── 2-requirements/         # Functional requirements, business rules BR-01..BR-09, decisions D-01..D-12
│   ├── 3-design/               # ERD schema, wireframes, UI design system (Forest, Sand, Clay)
│   ├── 4-testing/              # Test plan, defect log, benchmark reports
│   └── 5-final/                # Deployment guide, backup guide, user manual, presentations
├── src/
│   ├── SILA.sln                # Main solution file
│   └── SILA.Web/               # Core ASP.NET Core 10 MVC application
│       ├── Controllers/        # Thin controllers enforcing BR-07
│       ├── Data/               # ApplicationDbContext, Migrations, Seed/DbSeeder.cs
│       ├── Models/             # Domain entities, enums, and strongly-typed ViewModels
│       ├── Services/           # Business services (MaterialListing, Orders, ManufacturingRequests, Ownership)
│       ├── Views/              # Razor views (Materials, Orders, ManufacturingRequests, Dashboard, History)
│       ├── wwwroot/            # Design assets, CSS, JS, client libraries
│       ├── web.config          # IIS production configuration and security headers
│       └── Program.cs          # Pipeline, Identity, DI, and middleware registration
├── tests/
│   └── SILA.Tests/             # Automated test suite (xUnit, concurrency, authorization)
├── README.md                   # This file
└── SILA_PROJECT_CONTEXT.md     # Single source of truth for architecture and task boundaries
```

---

## 11. Team & Project Ownership (DEPI Full Stack .NET)

| Persona / Team Member | Primary Ownership Areas | Core Tasks |
|---|---|---|
| **P1 — Team Lead** | Solution architecture, Identity auth, Material Orders, Deployment & README | T1.2, T2.1, T3.2, T4.1, T4.2 |
| **P2 — Database Lead** | Schema design, Migrations, Organization profiles, Manufacturing Requests, Final Docs | T1.1, T2.2, T3.3, T4.4 |
| **P3 — Frontend Lead** | UI/UX Guidelines, Wireframes, Shared Layout, Public browse, Material Catalog | T1.3, T2.3, T3.1, T4.3 |
| **P4 — QA & Coordination Lead**| Planning docs, Shared services, Sent/Received hub, Dashboards, Defect tracking | T1.4, T2.4, T3.4 |

---
*Copyright © 2026 SILA Project Team — Digital Egypt Pioneers Initiative (DEPI).*
