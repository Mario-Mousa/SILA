<!--
═══════════ HANDOFF ═══════════════════════════════════
Task:        T2.4 | Owner: P4 (QA & Business Analyst) | Status: DONE
What exists: Comprehensive Test Plan and Initial Test Cases specification covering Authentication,
              Record-Level Authorization (BR-07), Shared Services (Notifications & StatusHistory), and Concurrency.
What is left: // TODO(T4.1): Execute manual and automated test suites across seeded dataset; log results in Defect Log
Depends on:  T1.4 (Acceptance Criteria), T2.1 (Auth), T2.2 (Verification), T2.4 (Ownership & Shared Services)
Used by:     P4 (T4.1 QA Lead), P1, P2, P3, Academic Reviewers
How to test: Review test case preconditions, execution steps, expected outcomes, and traceability matrix
Notes:       Enforces zero-defect tolerance on security breaches (BR-07) and negative inventory (BR-01/BR-03)
═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Quality Assurance Test Plan & Test Cases

> **Document:** Software Quality Assurance Plan & Formal Test Case Specification  
> **Task:** T2.4 | **Phase:** Phase 2 (Accounts & Shared Infrastructure)  
> **Owner:** P4 (QA & Business Analyst)  
> **Academic Program:** DEPI — Full Stack .NET Track  
> **Last Updated:** 14 November 2026  

---

## 1. Test Plan Overview & Objectives

### 1.1 Purpose
This document establishes the initial testing baseline for **SILA (صِلة)**. It defines structured test cases for core authentication flows (T2.1), organization verification (T2.2), and centralized record-level authorization (T2.4), preparing the framework for comprehensive automated and manual verification in Phase 4 (T4.1).

### 1.2 Quality Standards & Acceptance Criteria
- **Zero Critical / High Defects:** System must exhibit 0 open high/critical defects at milestone delivery.
- **Strict Server-Side Validation:** 100% of inputs validated server-side.
- **Strict Data Isolation (BR-07):** No organization may view or manipulate records belonging to another organization.
- **Append-Only Auditing (BR-08):** Audit history rows cannot be altered or removed.
- **Atomic Concurrency Guarantee (BR-03):** Concurrent checkouts must never drive inventory negative.

---

## 2. Test Suite 1: Authentication & Account Management (T2.1)

| Test ID | Feature Area | Description & Preconditions | Execution Steps | Expected Outcome | Status |
|---|---|---|---|---|:---:|
| **TC-AUTH-01** | Registration | Register new Brand Owner with valid data. | 1. Navigate to `/Account/Register`<br>2. Select Role = `Brand`<br>3. Enter valid Name, Email, Password (`Pass@1234`), Phone, Org Name.<br>4. Submit form. | User created in AspNetUsers with `EmailConfirmed = false`; Organization created with `VerificationStatus = Unverified`; Redirects to `/Account/EmailConfirmationSent`. | PASS |
| **TC-AUTH-02** | Registration | Rejection of duplicate email. | 1. Attempt registration with already existing email (`brand1@sila.com`).<br>2. Submit form. | Server returns validation error: *"Email address is already registered."* No record duplicated. | PASS |
| **TC-AUTH-03** | Registration | Enforcement of password complexity. | 1. Enter password without digits or shorter than 6 characters (e.g. `abc`).<br>2. Submit form. | Form rejected with client & server-side validation error indicating length and digit requirements. | PASS |
| **TC-AUTH-04** | Email Token | Unconfirmed user attempting sign-in. | 1. Register new user without clicking confirmation email link.<br>2. Navigate to `/Account/Login` and enter credentials.<br>3. Submit. | Sign-in rejected; user directed to `/Account/ResendConfirmationEmail` prompt: *"Please confirm your email before signing in."* | PASS |
| **TC-AUTH-05** | Email Token | Confirm email via token link. | 1. Access confirmation link containing valid userId and token.<br>2. Submit GET request. | Token verified; `EmailConfirmed` set to `true`; User redirected to `/Account/ConfirmEmail` success view. | PASS |
| **TC-AUTH-06** | Login & Redirect | Role-aware landing page redirection. | 1. Sign in as `brand1@sila.com`.<br>2. Sign out and sign in as `factory1@sila.com`.<br>3. Sign out and sign in as `supplier1@sila.com`. | Brand redirected to Brand Dashboard (`/Dashboard/Index` -> Brand view); Factory redirected to Factory view; Supplier redirected to Supplier view. | PASS |
| **TC-AUTH-07** | Verification Gating | Unverified account access restriction. | 1. Log in as an account with `VerificationStatus = Unverified`.<br>2. Attempt to create listing, order, or request. | User sees `/Account/UnverifiedNotice` banner stating account must be verified by Admin before initiating business workflows. | PASS |

---

## 3. Test Suite 2: Record-Level Authorization & Ownership (T2.4 / BR-07)

Centralized validation enforced by `IOwnershipService`:

| Test ID | Target Entity | Scenario & Actor | Execution & Input | Expected Outcome | Rule |
|---|---|---|---|---|:---:|
| **TC-AUTHZ-01** | `ManufacturingRequest` | Initiating Brand views own request. | Brand 1 accesses `/ManufacturingRequests/Details/1` (owned by Brand 1). | `CanAccessManufacturingRequestAsync` returns `true`. Request details displayed normally. | BR-07 |
| **TC-AUTHZ-02** | `ManufacturingRequest` | Target Factory views addressed request. | Factory 1 accesses `/ManufacturingRequests/Details/1` (addressed to Factory 1). | `CanAccessManufacturingRequestAsync` returns `true`. Request details and response actions displayed. | BR-07 |
| **TC-AUTHZ-03** | `ManufacturingRequest` | **Third-party Brand breach attempt.** | Brand 2 attempts to view `/ManufacturingRequests/Details/1` (belongs to Brand 1 & Factory 1). | `CanAccessManufacturingRequestAsync` returns `false`. Controller issues `ForbidResult()` (HTTP 403). Zero data revealed. | BR-07 |
| **TC-AUTHZ-04** | `ManufacturingRequest` | **Foreign Factory breach attempt.** | Factory 2 attempts to view `/ManufacturingRequests/Details/1`. | `CanAccessManufacturingRequestAsync` returns `false`. Controller issues HTTP 403 Forbidden. | BR-07 |
| **TC-AUTHZ-05** | `MaterialOrder` | Buyer views placed order. | Buyer (Brand 1) accesses `/Orders/Details/1` (placed by Brand 1). | `CanAccessMaterialOrderAsync` returns `true`. Order lines, snapshots, and status shown. | BR-07 |
| **TC-AUTHZ-06** | `MaterialOrder` | Target Supplier views incoming order. | Supplier 1 accesses `/Orders/Details/1` (placed with Supplier 1). | `CanAccessMaterialOrderAsync` returns `true`. Order lines and Confirm/Reject actions rendered. | BR-07 |
| **TC-AUTHZ-07** | `MaterialOrder` | **Third-party Buyer breach attempt.** | Brand 2 attempts to access `/Orders/Details/1`. | `CanAccessMaterialOrderAsync` returns `false`. Controller returns HTTP 403 Forbidden. | BR-07 |
| **TC-AUTHZ-08** | `MaterialListing` | Owning Supplier edits listing. | Supplier 1 edits listing #1 (owned by Supplier 1). | `CanModifyMaterialListingAsync` returns `true`. Modifications persisted. | BR-05 |
| **TC-AUTHZ-09** | `MaterialListing` | **Foreign Supplier modification attempt.** | Supplier 2 attempts to edit or deactivate listing #1 (owned by Supplier 1). | `CanModifyMaterialListingAsync` returns `false`. Modification rejected with HTTP 403 Forbidden. | BR-07 |
| **TC-AUTHZ-10** | `Organization` | Edit profile of another organization. | User of Brand 1 attempts to POST profile updates for Organization #2 (Brand 2). | `CanAccessOrganizationAsync` returns `false`. Update rejected with HTTP 403 Forbidden. | BR-07 |

---

## 4. Test Suite 3: Shared Infrastructure Services (T2.4)

| Test ID | Service Under Test | Test Scenario | Execution Steps | Expected Outcome |
|---|---|---|---|---|
| **TC-SHARED-01** | `NotificationService` | Notification creation & unread count. | 1. Call `CreateNotificationAsync(userId, "Test Alert", "/Orders/1")`.<br>2. Call `GetUnreadCountAsync(userId)`. | Notification stored with `IsRead = false`; Unread count increases by exactly 1. |
| **TC-SHARED-02** | `NotificationService` | Mark single notification as read. | 1. Call `MarkAsReadAsync(notificationId, userId)`.<br>2. Re-query `GetUnreadCountAsync(userId)`. | `IsRead` updated to `true`; Unread count decrements by 1. |
| **TC-SHARED-03** | `NotificationService` | Mark all notifications as read. | 1. Ensure user has 3 unread notifications.<br>2. Call `MarkAllAsReadAsync(userId)`. | All notifications set to `IsRead = true`; `GetUnreadCountAsync` returns `0`. |
| **TC-SHARED-04** | `StatusHistoryService` | Append-only status transition logging (BR-08). | 1. Call `LogHistoryAsync(StatusRecordType.Order, orderId, "PendingReview", "Confirmed", userId, "Verified stock")`.<br>2. Query `GetHistoryAsync(StatusRecordType.Order, orderId)`. | Record inserted with timestamp; History query returns entries ordered chronologically. |
| **TC-SHARED-05** | `StatusHistoryService` | Audit row immutability verification. | 1. Inspect `IStatusHistoryService` interface and implementation. | Service provides **zero** update or delete methods. Modifying history rows is programmatically impossible. |
| **TC-SHARED-06** | `DbSeeder` | Demo dataset verification in Development. | 1. Execute `DbSeeder.SeedAsync`.<br>2. Verify database counts. | - 4 Identity roles exist.<br>- Default Admin (`admin@sila.com`) seeded.<br>- ≥2 verified organizations per role (2 Brands, 2 Factories, 2 Suppliers).<br>- ≥50 Material listings populated across categories.<br>- Sample requests and orders present. |

---

## 5. Test Suite 4: Concurrency & Business Rules (BR-01, BR-02, BR-03, BR-04)

| Test ID | Business Rule | Test Scenario | Test Strategy & Steps | Pass Criteria |
|---|---|---|---|---|
| **TC-CONC-01** | **BR-03 & BR-01** | **Two concurrent orders of 15 units against 20 available.** | 1. Listing stock = 20 meters.<br>2. Order A (15m) and Order B (15m) both pending.<br>3. Simulate concurrent confirmation using parallel threads with isolated DbContexts. | Exactly **one** confirmation transaction commits; remaining stock becomes 5; second transaction throws `DbUpdateConcurrencyException` or stock check fails and rolls back cleanly; **Stock never drops below 0**. |
| **TC-RULE-02** | **BR-02** | Order containing multiple suppliers in cart. | 1. Add Listing from Supplier 1.<br>2. Add Listing from Supplier 2.<br>3. Attempt checkout. | System blocks submission with error: *"Orders may contain materials from one supplier only."* |
| **TC-RULE-03** | **BR-04** | Line snapshot fidelity after catalog edit. | 1. Place order for listing at 150.00 EGP.<br>2. Supplier later edits catalog price to 200.00 EGP and deactivates listing.<br>3. View order details. | Order line retains snapshot price of 150.00 EGP and original name; order total remains unmodified. |
| **TC-RULE-04** | **BR-05** | Prevention of hard deletion of referenced listing. | 1. Listing #1 is referenced in Order #1.<br>2. Supplier attempts to delete Listing #1. | Hard delete blocked by `DeleteBehavior.Restrict`; only soft deactivation (`IsActive = false`) is permitted. |

---

## 6. Requirements Traceability Matrix (RTM)

| Requirement / Rule ID | Addressed By Task | Verification Test Case IDs |
|---|---|---|
| **FR-01..FR-06 (Authentication)** | T2.1 | `TC-AUTH-01`, `TC-AUTH-02`, `TC-AUTH-03`, `TC-AUTH-04`, `TC-AUTH-05`, `TC-AUTH-06` |
| **FR-07..FR-11 (Verification)** | T2.2 | `TC-AUTH-07`, `TC-AUTHZ-10` |
| **BR-07 (Record-Level Authorization)** | T2.4 | `TC-AUTHZ-01` through `TC-AUTHZ-10` |
| **BR-08 (Immutable Status History)** | T2.4 | `TC-SHARED-04`, `TC-SHARED-05` |
| **BR-10 (In-App Notifications)** | T2.4 | `TC-SHARED-01`, `TC-SHARED-02`, `TC-SHARED-03` |
| **BR-01 & BR-03 (Atomic Stock Concurrency)** | T3.2 & T2.4 | `TC-CONC-01` |
| **BR-02 (Single Supplier Partition)** | T3.2 | `TC-RULE-02` |
| **BR-04 (Line Snapshot Immutability)** | T3.2 & T1.1 | `TC-RULE-03` |
| **BR-05 (Listing Deactivation Guard)** | T3.1 & T1.1 | `TC-RULE-04` |

