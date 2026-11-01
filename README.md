# SILA (صِلة) — B2B Fashion Supply Chain Platform

SILA (صِلة) is a B2B web platform designed for the Egyptian fashion industry, connecting **Brand Owners**, **Factory Owners**, and **Raw Material Suppliers** into a single centralized digital marketplace.

Developed as a graduation project for the **Digital Egypt Pioneers Initiative (DEPI) — Full Stack .NET Track**.

---

## 1. Project Overview & Workflows

SILA provides two completely independent workflows:
1. **Manufacturing Request Workflow**:
   - Brand Owners submit requests to a specific Factory with target specifications, quantities, and proposed unit prices.
   - Factories can Accept, Reject, or Counter-Offer with a revised unit price.
   - Brands can Accept, Reject, or Revise their offer.
2. **Material Order Workflow**:
   - Buyers (Brand Owners or Factory Owners) browse Supplier listings and submit orders at fixed catalog prices.
   - Orders are strictly single-supplier per order.
   - Stock deduction occurs **atomically upon Supplier confirmation**.
   - Suppliers transition orders: Confirmed → Dispatched → Delivered.

---

## 2. Technology Stack

- **Target Framework**: .NET 10.0 (C# / ASP.NET Core MVC with Razor Views)
- **Authentication & Authorization**: ASP.NET Core Identity with Role-Based Access Control (`Brand`, `Factory`, `Supplier`, `Admin`)
- **Database & ORM**: SQL Server (LocalDB / Express) with Entity Framework Core (Code-First Migrations)
- **Frontend**: HTML5, CSS3, JavaScript, Bootstrap 5
- **Email Service**: SMTP for account email verification only (all operational notifications are in-app)

---

## 3. Solution Structure

```
SILA/
├─ docs/
│  ├─ 1-planning/         # Agile management, sprints, Gantt chart
│  ├─ 2-requirements/     # Functional & non-functional requirements, decisions D-01..D-12
│  ├─ 3-design/           # ERD, schema specifications, wireframes, UI guidelines
│  ├─ 4-testing/          # Test plans, test cases, defect log, benchmark results
│  └─ 5-final/            # User manual, technical documentation, presentation, infographic
├─ src/
│  ├─ SILA.sln            # Visual Studio Solution
│  └─ SILA.Web/
│     ├─ Controllers/     # Thin MVC Controllers
│     ├─ Data/            # ApplicationDbContext, Migrations, Seed/DbSeeder.cs
│     ├─ Models/
│     │  ├─ Entities/     # Domain entities (PascalCase, singular)
│     │  ├─ Enums/        # System-wide enums and statuses
│     │  └─ ViewModels/   # Strongly-typed presentation models
│     ├─ Services/
│     │  ├─ Interfaces/   # Domain contracts (I...Service)
│     │  └─ Implementations/
│     ├─ Views/           # Razor views organized by feature area
│     ├─ wwwroot/         # Static assets (css, js, lib)
│     └─ Program.cs       # Application entry point, DI, pipeline configuration
├─ README.md
└─ PROJECT_CONTEXT.md     # Single source of truth for architecture and task boundaries
```

---

## 4. Getting Started from a Clean Clone

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or SQL Server Express)
- EF Core CLI tool: `dotnet tool install --global dotnet-ef` (if not already installed)

### 1. Clone the Repository
```bash
git clone https://github.com/Mario-Mousa/SILA.git
cd SILA
```

### 2. Configure Local Secrets (`dotnet user-secrets`)
Never store credentials or connection strings in Git. Initialize and configure local secrets:
```bash
dotnet user-secrets init --project src/SILA.Web

# Set your local database connection string:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=SilaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project src/SILA.Web

# Set your SMTP credentials for account verification emails:
dotnet user-secrets set "Smtp:Host" "smtp.example.com" --project src/SILA.Web
dotnet user-secrets set "Smtp:Port" "587" --project src/SILA.Web
dotnet user-secrets set "Smtp:Username" "your-email@example.com" --project src/SILA.Web
dotnet user-secrets set "Smtp:Password" "your-smtp-password" --project src/SILA.Web
dotnet user-secrets set "Smtp:FromEmail" "noreply@sila.com" --project src/SILA.Web
```

### 3. Apply Database Migrations
Run EF Core migrations to create the database schema and constraints:
```bash
dotnet ef database update --project src/SILA.Web
```

### 4. Run the Application
```bash
dotnet run --project src/SILA.Web
```
Once started, navigate to `https://localhost:7196` (or the port specified in terminal output).

---

## 5. Development Seed & Demo Accounts

On startup in the `Development` environment, `DbSeeder` automatically ensures all 4 system roles and the default administrative account exist:

| Role | Email | Password | Purpose |
|---|---|---|---|
| **Admin** | `admin@sila.com` | `Admin@123456` | Review & approve/reject organization verification documents |
| **Brand** | Seeded in T2.4 / Self-registered | Configured per account | Clothing brand submitting manufacturing requests & ordering materials |
| **Factory** | Seeded in T2.4 / Self-registered | Configured per account | Garment factory receiving manufacturing requests & counter-offering |
| **Supplier** | Seeded in T2.4 / Self-registered | Configured per account | Fabric & raw material supplier listing items & fulfilling orders |

*(Note: Admin sign-up is disabled by design. The Admin role is seeded and managed internally).*

---

## 6. Service Contracts (Part 5.4)

Business logic is encapsulated in the Services layer:
- `IOrganizationService`: Organization profiles and verified directory listings (T2.2)
- `IVerificationService`: Verification document submissions and Admin approvals (T2.2)
- `IOwnershipService`: Central record-level authorization checks enforcing BR-07 (T2.4)
- `INotificationService`: In-app notification creation, unread count, and reading (T2.4)
- `IStatusHistoryService`: Append-only immutable status audit history (T2.4)
- `IMaterialListingService`: Supplier raw material catalog CRUD and browsing (T3.1)
- `IMaterialOrderService`: Order lifecycle and atomic stock reservation (T3.2)
- `IManufacturingRequestService`: Request lifecycle, offer/counter-offer negotiation (T3.3)
- `IEmailSender`: Email confirmation service via SMTP (T2.1)

---

## 7. Git & Team Contribution Rules

1. **Branch Conventions**:
   - `main`: Production-ready release branch (tagged at phase ends).
   - `develop`: Main integration branch.
   - `feature/T<phase>-<n>-<name>`: Feature branch per task (e.g., `feature/T1-2-solution-skeleton`, `feature/T2-1-auth`).
2. **Pull Requests**:
   - Never commit directly to `main` or `develop`.
   - Open a PR into `develop` referencing the related issue (`Closes #<id>`).
   - Requires at least one teammate review before merge.
3. **Database Ownership**:
   - Only the **Database Owner (P2)** creates or edits EF Core migrations.
   - Other team members request schema changes by opening a GitHub issue with the `schema-change` label.
4. **HANDOFF Header**:
   - Every primary class or completed module must feature the standard `HANDOFF` header with status, dependencies, and testing instructions.
