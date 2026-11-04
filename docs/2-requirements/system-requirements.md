<!--
═══════════ HANDOFF ═══════════════════════════════════
Task:        T1.4 | Owner: P4 (QA & Business Analyst) | Status: DONE
What exists: Comprehensive System Requirements Specification (SRS) for SILA, incorporating Working Decisions D-01..D-12, isolated dual workflows, detailed functional and non-functional requirements, business rules BR-01..BR-11, and Given-When-Then acceptance criteria.
What is left: // TODO(T4.1): Formulate automated and manual test cases directly mapped to FRs and Acceptance Criteria.
Depends on:  T1.1 (Data Model & Schema), T1.2 (Service Contracts)
Used by:     All developers (P1, P2, P3, P4), Testers, Academic Reviewers
How to test: Review against SILA_PROJECT_CONTEXT.md to verify exact terminology (Part 2), complete coverage of D-01..D-12, business rules BR-01..BR-11, and no references to "Fashion Supply Hub".
Notes:       All requirements enforce server-side validation and record-level authorization.
═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — System Requirements Specification (SRS)

> **Document:** Software Requirements Specification (SRS)  
> **Task:** T1.4 | **Owner:** P4 (QA & Business Analyst)  
> **Target Framework:** ASP.NET Core 8.0 MVC / EF Core / SQL Server  
> **Academic Initiative:** DEPI (Digital Egypt Pioneers Initiative) — Full Stack .NET Track  
> **Last Updated:** 29 October 2026  

---

## 1. Introduction & Executive Overview

### 1.1 Purpose
This document specifies the complete functional and non-functional requirements for **SILA (صِلة)**. It serves as the definitive contractual baseline for the development team, QA engineers, and academic evaluators. All implementations across Phases 2, 3, and 4 must conform to the requirements specified herein.

### 1.2 System Purpose & Value Proposition
In the contemporary Egyptian fashion manufacturing landscape, communication between fashion brand designers, garment factories, and raw textile suppliers is fragmented across informal, unindexed channels (WhatsApp, phone calls, personal visits). This lack of structure produces critical operational failures:
- Untracked price negotiations and verbal agreements.
- Frequent fabric stockouts discovered only after cutting has commenced.
- Unverified businesses leading to transactional fraud and delivery delays.

**SILA (صِلة)** establishes a centralized, structured B2B web platform providing verified discovery, formal price-negotiated Manufacturing Requests, and atomic inventory-controlled Material Orders.

### 1.3 Project Identity & Academic Context
- **System Name:** SILA (صِلة). *(Formerly referenced in early exploratory drafts as "Fashion Supply Hub". That working title is entirely obsolete).*
- **Nature of System:** Academic demonstration and graduation capstone project developed under the DEPI program. 
- **Scale:** Academic prototype designed for rigorous correctness, transactional integrity, and role security, rather than production-scale distributed traffic.

---

## 2. Working Decisions (Decisions D-01 through D-12)

The requirements incorporate twelve foundational architectural and business decisions:

| Decision ID | Summary | Specific Requirement / Enforcement |
|---|---|---|
| **D-01** | **Targeted Manufacturing Requests** | A Brand Owner issues a Manufacturing Request to **one specifically selected factory**. Broadcast or public tender requests are excluded. |
| **D-02** | **Internal Admin Account** | A lightweight `Admin` role exists exclusively to review and approve/reject business account verification. Admin is provisioned via database seed; public registration is disabled. |
| **D-03** | **Negotiation Boundaries** | Factories may submit counter-offers on Manufacturing Requests. Suppliers operate with **fixed catalog unit prices**; raw materials have zero price negotiation. |
| **D-04** | **Restricted Email Usage** | SMTP email delivery is reserved strictly for **account email confirmation**. All operational alerts (order status, offers) are handled via in-app notifications. |
| **D-05** | **Atomic Inventory Deduction** | Material stock is checked at order placement and **deducted atomically only when the Supplier confirms** the order in a single database transaction. |
| **D-06** | **Cancellation Window** | A submitter (Buyer or Brand) may cancel a request or order **only while it remains in Pending status**. Confirmed orders or accepted requests cannot be cancelled. |
| **D-07** | **Supplier Delivery Progression** | Following order confirmation, the Supplier updates order status sequentially: `Confirmed` ➔ `Dispatched` ➔ `Delivered`. |
| **D-08** | **Catalog Search & Filtering** | Material catalog must support case-insensitive keyword searching (by material name) and category-based filtering. |
| **D-09** | **Permanent Listing Deactivation** | Deactivated material listings cannot be reactivated (dropped to maintain historical integrity and simplify state management). |
| **D-10** | **Monetary Standard** | The sole system currency is Egyptian Pounds (**EGP**), stored as `decimal(18,2)`. |
| **D-11** | **Unbounded Negotiation Cycles** | Brands and factories may iterate through multiple offer/counter-offer/revised offer rounds without an arbitrary system cap. |
| **D-12** | **Manual Verification Review** | Brand ownership is verified by manual Admin inspection of social media (e.g., Instagram profile) or portfolio links, acknowledged as an academic prototype limitation. |

---

## 3. Scope Boundaries

### 3.1 In-Scope Capabilities
1. Multi-role authentication (Brand Owner, Factory Owner, Supplier, Admin) via ASP.NET Core Identity.
2. Mandatory email address verification via SMTP prior to portal access.
3. Organization profiles and verification document upload with strict MIME-type and size checks.
4. Admin verification portal with Approve/Reject actions and reason logging.
5. Supplier raw material catalog management (creation, unit pricing, real-time quantity, deactivation).
6. Buyer material search, filtering, order placement with single-supplier validation.
7. Atomic order confirmation with stock deduction and concurrency handling via `RowVersion`.
8. Manufacturing Request creation, factory counter-offers, brand revisions, and accept/reject flows.
9. Unified role dashboards with a two-tab **Sent / Received** tracking interface.
10. Immutable status audit trail (`StatusHistory`) and stock transaction ledger (`StockMovement`).
11. In-app notification center with real-time unread badges.

### 3.2 Explicitly Out of Scope
The following capabilities are excluded from the academic implementation:
- Online payment gateway integration, credit card processing, or escrow wallets.
- Formal tax invoicing, commercial receipts, and automated PDF export.
- Logistics, shipping company API integration, and GPS tracking.
- Factory shop-floor production scheduling after a Manufacturing Request is accepted.
- SMS-based OTP authentication (to avoid telecommunications costs).
- Multiple user accounts per organization (1 Organization = 1 User = 1 Role).
- Bidding or quotation requests for raw material listings.

---

## 4. The Two Isolated Workflows

SILA strictly segregates manufacturing contracts from material purchases. They do not share database entities, statuses, transitions, or business rules:

| Workflow Attribute | **Manufacturing Request** Workflow | **Material Order** Workflow |
|---|---|---|
| **Primary Purpose** | Custom apparel production contract | Off-the-shelf raw textile purchase |
| **Initiating Actor** | Brand Owner | Buyer (Brand Owner or Factory Owner) |
| **Responding Actor** | Factory Owner (of target factory) | Supplier (owning the listings) |
| **Pricing Model** | Negotiated (Initial Offer ➔ Counter-Offer ➔ Revision) | Fixed catalog unit price (No negotiation) |
| **Inventory Impact** | **None** (zero stock impact) | **Atomic Deduction** upon Supplier confirmation |
| **Core Entity** | `ManufacturingRequest` & `RequestOffer` | `MaterialOrder` & `MaterialOrderLine` |
| **Status Lifecycle** | `PendingApproval`, `CounterOffered`, `Accepted`, `Rejected`, `Cancelled` | `PendingReview`, `Confirmed`, `Rejected`, `Dispatched`, `Delivered`, `Cancelled` |
| **Interdependence** | **Completely independent:** Accepting a request does not generate or reserve materials. | **Completely independent:** Purchasing materials does not bind any factory capacity. |

---

## 5. User Roles & Personas

| Role Name | Identity Role Key | System Description |
|---|---|---|
| **Brand Owner** | `Brand` | Operates an apparel label. Submits Manufacturing Requests to factories; purchases raw fabrics from suppliers. |
| **Factory Owner** | `Factory` | Operates a garment production facility. Receives manufacturing requests, negotiates pricing; purchases raw fabrics. |
| **Supplier** | `Supplier` | Operates a fabric or textile warehouse. Lists raw materials with fixed unit pricing; processes orders. |
| **Buyer** | *(Composite)* | Any authenticated Brand Owner or Factory Owner actively purchasing from a Supplier catalog. |
| **Admin** | `Admin` | System administrator responsible solely for reviewing verification proofs and approving/rejecting organizations. |

---

## 6. Functional Requirements

### 6.1 Authentication & Organization Onboarding
- **FR-01 (Role Selection):** The system shall provide an intuitive onboarding interface prompting the user to declare their organization type (`Brand`, `Factory`, or `Supplier`).
- **FR-02 (User Registration):** Registration shall capture Full Name, Email, Password, Phone Number, and Organization Name. The system shall atomically create the `ApplicationUser` and corresponding `Organization` record with `VerificationStatus = Unverified`.
- **FR-03 (Password Security):** Passwords must be hashed using PBKDF2 with SHA-256 (default ASP.NET Core Identity standard). Minimum 8 characters with at least one digit and one non-alphanumeric character.
- **FR-04 (Email Confirmation):** Upon registration, the system shall generate a secure, time-limited token and dispatch a confirmation link via SMTP. Unconfirmed accounts shall be blocked from accessing authenticated features.
- **FR-05 (Authentication & Redirection):** Upon sign-in, the system shall evaluate the user's role and redirect to the corresponding dashboard.
- **FR-06 (Access Denied):** Any attempt to access unauthorized role endpoints shall yield an HTTP 403 Forbidden page without leaking internal data.

### 6.2 Profiles & Verification
- **FR-07 (Role-Specific Profile Data):**
  - Brand Owners: Contact phone, portfolio text description, Instagram link.
  - Factory Owners: Physical facility address, city, contact phone, manufacturing capabilities.
  - Suppliers: Warehouse city, contact phone, material specializations.
- **FR-08 (Document Submission):** Organizations must upload verification documentation:
  - Factory: Industrial operating license (`.pdf`, `.jpg`, `.png`, max 5 MB).
  - Supplier: Commercial registration / tax card (`.pdf`, `.jpg`, `.png`, max 5 MB).
  - Brand: Link to established brand portfolio/social channel and work samples.
- **FR-09 (Verification Submission):** Submitting verification artifacts shall transition `VerificationStatus` from `Unverified` to `PendingReview`.
- **FR-10 (Admin Verification Dashboard):** Admin users shall view a list of organizations in `PendingReview`, inspect uploaded files and portfolio links, and execute **Approve** or **Reject** decisions with mandatory feedback notes.
- **FR-11 (Verification Enforcement):** Organizations with `VerificationStatus != Verified` shall be prohibited from publishing material listings, placing material orders, or issuing manufacturing requests.

### 6.3 Supplier Catalog Management
- **FR-12 (Catalog Item Creation):** Verified Suppliers shall create listings comprising: Name, Category, Description, Unit (e.g., Meter, Roll, Kg), Unit Price (> 0 EGP), and Available Quantity (≥ 0).
- **FR-13 (Listing Updates):** Suppliers may edit descriptions, unit prices, and available quantities of their own listings.
- **FR-14 (Listing Deactivation):** Suppliers may permanently deactivate a listing (`IsActive = false`). Deactivated listings must be hidden from search results and cart selection (BR-05).
- **FR-15 (Deletion Guard):** Hard deletion of material listings referenced in any existing order is strictly blocked (BR-05).
- **FR-16 (Public Catalog Browsing):** Authenticated Buyers can browse all active listings from verified suppliers, paginated with keyword search and category filters.
- **FR-17 (Out-of-Stock Indicator):** Listings with `AvailableQuantity == 0` must display an **Out of Stock** badge and prevent order placement (BR-06).

### 6.4 Material Order Workflow
- **FR-18 (Single-Supplier Restriction):** A Material Order can include line items from **exactly one Supplier**. Mixed supplier carts are rejected (BR-02).
- **FR-19 (Line Item Snapshots):** At order placement, the system must freeze snapshots of: `MaterialNameSnapshot`, `UnitSnapshot`, and `UnitPriceSnapshot` inside `MaterialOrderLine` (BR-04).
- **FR-20 (Order Total Calculation):** Order total must equal the exact sum of line totals ($\sum \text{Quantity} \times \text{SnapshotUnitPrice}$).
- **FR-21 (Order Creation):** Order is created in `PendingReview` status. A notification is dispatched to the Supplier.
- **FR-22 (Atomic Confirmation & Deduction):** Upon Supplier confirmation:
  1. Verify in the same transaction that `AvailableQuantity >= OrderQuantity` for every line.
  2. Deduct quantities from `MaterialListing`.
  3. Insert `StockMovement` records (BR-09).
  4. Transition status to `Confirmed`.
  5. If available quantity is insufficient for any line, roll back the transaction and return an error (BR-03).
- **FR-23 (Order Rejection):** The Supplier may reject a pending order with a mandatory rejection reason, transitioning the status to `Rejected`. No inventory is affected.
- **FR-24 (Buyer Cancellation):** The Buyer may cancel an order only while its status is `PendingReview` (D-06).
- **FR-25 (Delivery Progression):** Following confirmation, the Supplier updates status to `Dispatched`, and subsequently to `Delivered` upon physical receipt (D-07).

### 6.5 Manufacturing Request Workflow
- **FR-26 (Request Submission):** A verified Brand Owner submits a request targeting a verified Factory Owner, specifying: Product Type, Design Specification, Material Description, Quantity (integer > 0), Initial Offer Price per Unit (> 0 EGP), and optional design attachment URL.
- **FR-27 (Request Lifecycle State):** Initial submission sets status to `PendingApproval` and creates Round 1 in `RequestOffer`.
- **FR-28 (Factory Acceptance):** The Factory Owner may accept the current offer, transitioning status to `Accepted` (final).
- **FR-29 (Factory Rejection):** The Factory Owner may reject the request with an optional decision note, transitioning status to `Rejected` (final).
- **FR-30 (Factory Counter-Offer):** The Factory Owner may submit a counter-offer specifying a new unit price and note, transitioning status to `CounterOffered` and recording a new `RequestOffer` round.
- **FR-31 (Brand Negotiation Response):**
  - Brand accepts counter-offer ➔ transitions to `Accepted`.
  - Brand rejects counter-offer ➔ transitions to `Rejected`.
  - Brand submits revised offer ➔ records new `RequestOffer` round, transitions status back to `PendingApproval` for factory review.
- **FR-32 (Brand Cancellation):** The Brand Owner may cancel the request while in `PendingApproval` or `CounterOffered` status (D-06).

### 6.6 Cross-Cutting Services
- **FR-33 (Sent/Received Dashboard):** Each user role shall have a dedicated "Requests" center with two distinct tabs:
  - **Sent Tab:** Items initiated by the user (Brand: sent manufacturing requests + material orders; Factory: material orders).
  - **Received Tab:** Items received by the user (Factory: incoming manufacturing requests; Supplier: incoming material orders).
- **FR-34 (Status Audit Logging):** Every state transition across both workflows must insert an immutable `StatusHistory` record capturing: RecordType, RecordId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, and optional Note (BR-08).
- **FR-35 (In-App Notifications):** System generates in-app notifications upon each state transition, linking directly to the relevant record (BR-10).
- **FR-36 (Record-Level Authorization):** Centralized `IOwnershipService` validates that the executing user's organization is either the owner or the addressee of the accessed record before serving data or processing commands (BR-07).

---

## 7. Server-Side Business Rules

- **BR-01 (Scalar Sanity):** Quantities must be positive integers ($> 0$). Unit prices must be positive decimals ($> 0.00$). Stock quantities must be non-negative ($\ge 0$).
- **BR-02 (Single Supplier Partition):** A Material Order must contain line items originating from a single supplier organization.
- **BR-03 (Atomic Inventory Operations):** Stock validation, inventory decrement, status update, and audit logging must execute within an isolated database transaction (`IDbContextTransaction`).
- **BR-04 (Immutability via Snapshots):** Order line records must freeze catalog data at the moment of checkout. Subsequent edits to material listing names or prices must never alter past orders.
- **BR-05 (Soft Deletion / Deactivation):** Listings referenced by existing orders cannot be deleted from the database. Only deactivation (`IsActive = false`) is permitted.
- **BR-06 (Zero-Stock Barrier):** Material listings with `AvailableQuantity == 0` cannot be ordered.
- **BR-07 (Record-Level Authorization):** Client-side UI element hiding is insufficient. Every controller action and service method must verify organizational ownership via `IOwnershipService`. Unauthorized requests return HTTP 403.
- **BR-08 (Immutable Status History):** Status transitions must be written to an append-only audit table. Modifying or deleting audit records is architecturally prohibited.
- **BR-09 (Stock Movement Traceability):** Every inventory reduction on confirmation must record a corresponding `StockMovement` entry referencing the order and listing.
- **BR-10 (Event-Driven In-App Alerts):** All state changes dispatch targeted notifications to the impacted counterparty.
- **BR-11 (Transaction Rollback):** Any unhandled exception during business logic execution must trigger a complete database rollback before error reporting.

---

## 8. Non-Functional Requirements

### 8.1 Performance & Efficiency
- **NFR-01:** Under simulated volume (≥50 listings, ≥100 orders, ≥20 requests, 10 concurrent users), 95% of standard page loads must complete in under **3.0 seconds**.
- **NFR-02:** Database queries returning collections must utilize explicit pagination (`Skip()` / `Take()`) with maximum page size of 20 items.

### 8.2 Security & Data Protection
- **NFR-03:** Passwords must be hashed using PBKDF2/SHA-256. Plain text passwords must never be logged or persisted.
- **NFR-04:** Application secrets (database credentials, SMTP tokens) must reside outside source control using `dotnet user-secrets` in development and environment variables in deployment.
- **NFR-05:** Anti-forgery tokens (`[ValidateAntiForgeryToken]`) must be enforced on all HTTP POST/PUT/DELETE actions.
- **NFR-06:** File uploads must be strictly validated against allowed extensions (`.pdf`, `.png`, `.jpg`) and bounded to a 5 MB maximum file size. Uploaded files must be stored outside the source repository.

### 8.3 Concurrency & Reliability
- **NFR-07:** The database schema must enforce an optimistic concurrency token (`rowversion` on `MaterialListing`) to prevent dirty writes during concurrent checkouts.
- **NFR-08:** The database engine must enforce a physical check constraint `CHECK (AvailableQuantity >= 0)` on the `MaterialListings` table.

### 8.4 Usability & Viewport Compatibility
- **NFR-09:** UI layouts must render responsively and without horizontal scrolling across three standard desktop/tablet viewports: 1920×1080 (FHD), 1366×768 (Standard Laptop), and 768×1024 (Tablet Portrait). Mobile phone layouts are explicitly out of scope.
- **NFR-10:** Form validation failures must present field-specific error messages indicating the exact corrective action required.

---

## 9. Given-When-Then Acceptance Criteria

### Scenario AC-01: Concurrent Inventory Checkout (BR-03 & Concurrency)
- **Given** a material listing for "Heavy Denim" has an available stock of 20 meters.
- **When** Buyer A submits an order for 15 meters and Buyer B submits an order for 15 meters, and the Supplier attempts to confirm both orders simultaneously.
- **Then** exactly one order confirmation succeeds, deducting 15 meters and leaving 5 meters remaining;
- **And** the second confirmation transaction fails gracefully with a user-friendly error stating insufficient available stock;
- **And** the available stock in the database never falls below 0.

### Scenario AC-02: Record-Level Authorization Breach Attempt (BR-07)
- **Given** Brand Owner "Alpha Apparel" owns Manufacturing Request #42 addressed to Factory "Beta Textiles".
- **When** an authenticated Brand Owner "Gamma Fashion" attempts to view or post decisions to `/ManufacturingRequests/Details/42`.
- **Then** the server refuses the request with an HTTP 403 Forbidden response;
- **And** no proprietary request details, notes, or design links are disclosed to Gamma Fashion.

### Scenario AC-03: Manufacturing Request Counter-Offer Round (D-03 & D-11)
- **Given** Brand Owner submits a request for 100 pants with an initial offer of 200 EGP/unit (Status = `PendingApproval`).
- **When** the target Factory Owner submits a counter-offer of 250 EGP/unit with note "Heavy fabric requires reinforcement stitching".
- **Then** the request status transitions to `CounterOffered`;
- **And** a new record is added to `RequestOffer` with RoundNumber = 2;
- **And** an in-app notification is delivered to the Brand Owner;
- **And** the Brand Owner's interface unlocks actions: "Accept", "Reject", and "Send Revised Offer".

