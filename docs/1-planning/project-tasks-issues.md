# SILA — 16 GitHub Issues Backlog & Task Breakdown

This document provides the exact titles, descriptions, assignees, and acceptance criteria for the 16 project tasks to populate the GitHub Project Board (`Backlog -> To Do -> In Progress -> In Review -> Testing -> Done`).

---

## Phase 1: Foundation & Design

### [T1.1] Data model and database foundation
- **Owner**: P2 (Database Engineer)
- **Assignee**: P2
- **Labels**: `phase-1`, `database`
- **You own**: `Models/Entities/*`, `Models/Enums/*`, `Data/ApplicationDbContext.cs`, `Data/Migrations/*`, `docs/3-design/erd*`, `docs/3-design/schema*`
- **Acceptance Criteria**:
  - Clean clone + `dotnet ef database update` creates all tables and constraints.
  - AvailableQuantity >= 0 check constraint enforced.
  - Concurrency token `RowVersion` on `MaterialListing`.
  - All statuses are Enums (zero hardcoded strings).

### [T1.2] Solution skeleton, Identity and service contracts
- **Owner**: P1 (Team Lead & Backend)
- **Assignee**: P1
- **Labels**: `phase-1`, `backend`, `architecture`
- **You own**: `Program.cs`, `Services/Interfaces/*`, `Services/Implementations/*` (empty shells), `Data/Seed/DbSeeder.cs`, `README.md`, `.github/*`
- **Acceptance Criteria**:
  - Solution builds and runs cleanly from fresh clone.
  - ASP.NET Core Identity configured with 4 roles (`Brand`, `Factory`, `Supplier`, `Admin`).
  - Default Admin account seeded in Development.
  - All 8 service interfaces defined with XML docs and DI registrations.

### [T1.3] UI/UX design and shared layout
- **Owner**: P3 (Frontend & UI/UX)
- **Assignee**: P3
- **Labels**: `phase-1`, `frontend`, `design`
- **You own**: `docs/3-design/wireframes*`, `docs/3-design/ui-guidelines*`, `Views/Shared/*`, `wwwroot/css/*`, `wwwroot/js/site.js`
- **Acceptance Criteria**:
  - Wireframes for all core screens.
  - Shared layout with role-aware top navigation.
  - Status badges matching enum names and colors.
  - Responsive layouts verified at 1920x1080, 1366x768, and 768x1024.

### [T1.4] Requirements update and design documents
- **Owner**: P4 (QA & Business Analyst)
- **Assignee**: P4
- **Labels**: `phase-1`, `documentation`, `analysis`
- **You own**: `docs/1-planning/*`, `docs/2-requirements/*`, `docs/3-design/*` (except ERD, schema, wireframes)
- **Acceptance Criteria**:
  - Planning document with sprints and Gantt chart.
  - Requirements document aligned with working decisions D-01..D-12.
  - Complete UML diagrams: Use Case, Activity, State, Sequence, DFD, Class, Architecture, Component, Deployment.

---

## Phase 2: Accounts & Shared Infrastructure

### [T2.1] Registration, login and email confirmation
- **Owner**: P1 (Team Lead & Backend)
- **Assignee**: P1
- **Labels**: `phase-2`, `backend`, `auth`
- **You own**: `Controllers/AccountController.cs`, `Views/Account/*`, `Services/EmailSender*`, `Models/ViewModels/Account/*`
- **Acceptance Criteria**:
  - Single-role registration creates user + Unverified organization atomically.
  - Confirmation email sent via SMTP; unconfirmed accounts blocked.
  - Role-specific redirection on login; access-denied handling.

### [T2.2] Profiles, verification documents and Admin approval
- **Owner**: P2 (Database Engineer & Backend)
- **Assignee**: P2
- **Labels**: `phase-2`, `backend`, `verification`
- **You own**: `Services/Implementations/OrganizationService.cs`, `VerificationService.cs`, `Controllers/ProfileController.cs`, `Controllers/AdminController.cs`, ViewModels
- **Acceptance Criteria**:
  - Role-specific profile editing and document upload with type/size validation.
  - Admin review dashboard for PendingReview organizations.
  - Unverified accounts cannot create listings, orders, or requests.

### [T2.3] Public pages, sign-up screens and Browse
- **Owner**: P3 (Frontend & UI/UX)
- **Assignee**: P3
- **Labels**: `phase-2`, `frontend`
- **You own**: `Views/Home/*`, `Views/Account/*`, `Views/Profile/*`, `Views/Admin/*`, `Controllers/HomeController.cs`
- **Acceptance Criteria**:
  - Public Home with Browse Suppliers and Browse Factories.
  - Only Verified organizations visible in public directory.
  - Role-choice entry screen for Login/Sign-up.
  - Client-side validation with field-level error messages.

### [T2.4] Shared services: ownership, notifications, status history, seed data
- **Owner**: P4 (QA & Shared Services)
- **Assignee**: P4
- **Labels**: `phase-2`, `backend`, `testing`
- **You own**: `Services/Implementations/OwnershipService.cs`, `NotificationService.cs`, `StatusHistoryService.cs`, `Controllers/NotificationsController.cs`, `Data/Seed/*`, `docs/4-testing/*`
- **Acceptance Criteria**:
  - `IOwnershipService` centralizes BR-07 enforcement.
  - In-app notification creation and nav bell unread count.
  - Append-only status history logging (BR-08).
  - Seed data generated (>=2 orgs per role, >=50 listings, sample records).

---

## Phase 3: Core Workflows

### [T3.1] Supplier catalog and material browsing
- **Owner**: P3 (Frontend & UI/UX)
- **Assignee**: P3
- **Labels**: `phase-3`, `frontend`, `materials`
- **You own**: `Services/Implementations/MaterialListingService.cs`, `Controllers/MaterialsController.cs`, `Views/Materials/*`, ViewModels
- **Acceptance Criteria**:
  - Supplier listing management (create, edit, soft-deactivate).
  - Permanent delete prevented if referenced in orders (BR-05).
  - Out of Stock indicator when AvailableQuantity == 0.
  - Buyer catalog search by keyword, category filter, and pagination.

### [T3.2] Material Orders and atomic stock
- **Owner**: P1 (Team Lead & Backend)
- **Assignee**: P1
- **Labels**: `phase-3`, `backend`, `orders`
- **You own**: `Services/Implementations/MaterialOrderService.cs`, `Controllers/OrdersController.cs`, `Views/Orders/*`, ViewModels
- **Acceptance Criteria**:
  - Single-supplier order constraint enforced (BR-02).
  - Order line snapshots for price, name, unit (BR-04).
  - Atomic stock check + deduction in single transaction upon confirmation (BR-03).
  - Full lifecycle: PendingReview -> Confirmed -> Dispatched -> Delivered (or Cancelled/Rejected).

### [T3.3] Manufacturing Requests with counter-offers
- **Owner**: P2 (Database Engineer & Backend)
- **Assignee**: P2
- **Labels**: `phase-3`, `backend`, `requests`
- **You own**: `Services/Implementations/ManufacturingRequestService.cs`, `Controllers/ManufacturingRequestsController.cs`, `Views/ManufacturingRequests/*`, ViewModels
- **Acceptance Criteria**:
  - Requests targeted to single verified factory (D-01).
  - Offer negotiation rounds recorded in `RequestOffer` (initial -> counter-offer -> revision).
  - Strict lifecycle transition validation according to Part 4.3 state table.
  - Zero stock impact.

### [T3.4] Sent/Received page, dashboards and history
- **Owner**: P4 (QA & Shared Services)
- **Assignee**: P4
- **Labels**: `phase-3`, `frontend`, `dashboards`
- **You own**: `Controllers/DashboardController.cs`, `Controllers/HistoryController.cs`, `Views/Dashboard/*`, `Views/History/*`, `Views/Notifications/*`, ViewModels
- **Acceptance Criteria**:
  - Unified Requests page with Sent and Received tabs per role.
  - Role-specific dashboard metrics and pending actions.
  - Audit history detail view showing status transitions, actors, and notes.

---

## Phase 4: Testing, Hardening & Delivery

### [T4.1] Testing and QA
- **Owner**: P4 (QA Lead)
- **Assignee**: P4
- **Labels**: `phase-4`, `qa`, `testing`
- **You own**: `docs/4-testing/*`, test project
- **Acceptance Criteria**:
  - Test Plan and comprehensive Test Cases.
  - Concurrency test (two orders of 15 units vs 20 available).
  - Authorization negative tests (cross-organization access blocked).
  - Usability test with >=3 representatives per role (>=80% success rate).
  - Zero open high/critical defects.

### [T4.2] Deployment, backup/restore and README
- **Owner**: P1 (Team Lead)
- **Assignee**: P1
- **Labels**: `phase-4`, `deployment`, `devops`
- **You own**: deployment configuration, `README.md`, `docs/5-final/deployment*`, release tags
- **Acceptance Criteria**:
  - Hosted environment live and connected to database.
  - Clean clone runs out-of-the-box using only README.
  - Database backup and restore verified.

### [T4.3] Polish, responsive design and validation messages
- **Owner**: P3 (Frontend Lead)
- **Assignee**: P3
- **Labels**: `phase-4`, `ui-polish`
- **You own**: `Views/*` styling, `wwwroot/*`
- **Acceptance Criteria**:
  - Unified terminology and enum badge colors across all pages.
  - Responsive validation at 1920x1080, 1366x768, and 768x1024.
  - Friendly validation error messages and empty states.

### [T4.4] Final documents, presentation and infographic
- **Owner**: P2 (Database & Documentation)
- **Assignee**: P2
- **Labels**: `phase-4`, `final-delivery`
- **You own**: `docs/5-final/*`
- **Acceptance Criteria**:
  - User Manual for all 3 roles + Admin.
  - Technical documentation with API & schema notes.
  - Presentation slides (PPT/PDF).
  - One-image infographic complying with DEPI Full Stack .NET criteria.

