<!--
═══════════ HANDOFF ═══════════════════════════════════
Task:        T1.4 | Owner: P4 (QA & Business Analyst) | Status: DONE
What exists: Data Flow Diagrams (DFD) including Context Level 0 Diagram and Level 1 Functional Decomposition Diagram with external entities, processes, data stores, and data flows.
What is left: // TODO(T4.1): Formulate boundary data-flow security tests verifying D1..D5 store isolation.
Depends on:  T1.1 (Data Model & Schema), T1.2 (Service Contracts)
Used by:     All developers (P1, P2, P3, P4), Academic Reviewers
How to test: Verify data store mappings (D1..D5) match physical SQL Server tables; verify external entities match glossary.
Notes:       Process boundaries clearly isolate Material Orders from Manufacturing Requests.
═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Data Flow Diagrams (DFD)

> **Document:** Context Diagram (Level 0) & Detailed DFD (Level 1)  
> **Task:** T1.4 | **Owner:** P4 (QA & Business Analyst)  
> **Academic Initiative:** DEPI — Full Stack .NET Track  
> **Last Updated:** 29 October 2026  

---

## 1. Context Diagram (DFD Level 0)

The Level 0 Context Diagram depicts the boundaries of the SILA platform, showing external actors and the major information flows exchanged with the centralized system:

```mermaid
flowchart TD
    %% External Entities
    Brand["fa:fa-user-tie Brand Owner"]
    Factory["fa:fa-industry Factory Owner"]
    Supplier["fa:fa-boxes-stacked Supplier"]
    Admin["fa:fa-shield-halved Admin User"]
    SMTP["fa:fa-envelope SMTP Email Server"]

    %% Central Process
    SILA(("0.0<br><b>SILA B2B Fashion<br>Platform System</b>"))

    %% Data Flows: Brand
    Brand -- "Reg Data, Org Profile, Verification Proof" --> SILA
    Brand -- "Manufacturing Requests & Offers" --> SILA
    Brand -- "Material Orders" --> SILA
    SILA -- "Counter-Offers, Request Status & Notifications" --> Brand
    SILA -- "Order Summaries, Catalog Data & Delivery Updates" --> Brand

    %% Data Flows: Factory
    Factory -- "Reg Data, Industrial License, Org Profile" --> SILA
    Factory -- "Accept / Reject / Counter-Offer Decisions" --> SILA
    Factory -- "Material Orders (as Buyer)" --> SILA
    SILA -- "Incoming Manufacturing Requests & Revised Offers" --> Factory
    SILA -- "Order Confirmations & Catalog Data" --> Factory

    %% Data Flows: Supplier
    Supplier -- "Reg Data, Tax Registration, Org Profile" --> SILA
    Supplier -- "Material Listings (Prices, Quantities)" --> SILA
    Supplier -- "Order Confirmations, Rejections, Dispatch/Delivery" --> SILA
    SILA -- "Incoming Material Orders & Cancellation Notices" --> Supplier

    %% Data Flows: Admin
    Admin -- "Verification Approvals & Rejection Notes" --> SILA
    SILA -- "Pending Organization Review Queue & Documents" --> Admin

    %% Data Flows: SMTP
    SILA -- "Email Confirmation Links & Tokens" --> SMTP
    SMTP -- "Delivered Verification Emails" --> Brand
    SMTP -- "Delivered Verification Emails" --> Factory
    SMTP -- "Delivered Verification Emails" --> Supplier
```

---

## 2. Level 1 Data Flow Diagram (Process Decomposition)

The Level 1 DFD decomposes the system into five core functional sub-processes interacting with five centralized data stores:

```mermaid
flowchart TD
    %% External Entities
    Brand["Brand Owner"]
    Factory["Factory Owner"]
    Supplier["Supplier"]
    Admin["Admin"]
    SMTP["SMTP Server"]

    %% Data Stores
    D1[("D1: Users & Organizations<br>(AspNetUsers, Organizations, VerificationDocs)")]
    D2[("D2: Material Catalog<br>(MaterialListings)")]
    D3[("D3: Material Orders<br>(MaterialOrders, Lines, StockMovements)")]
    D4[("D4: Manufacturing Requests<br>(ManufacturingRequests, RequestOffers)")]
    D5[("D5: Audit & Alerts<br>(StatusHistories, Notifications)")]

    %% Process 1.0: Auth & Org Management
    P1(("1.0<br>Identity, Auth &<br>Verification"))
    Brand & Factory & Supplier -- "Registration & Login Data" --> P1
    Brand & Factory & Supplier -- "Verification Documents" --> P1
    P1 -- "User & Org Records" --> D1
    D1 -- "Read User & Verification Status" --> P1
    P1 -- "Confirmation Token" --> SMTP
    Admin -- "Approve / Reject Action" --> P1
    P1 -- "Audit Event" --> D5

    %% Process 2.0: Catalog Management
    P2(("2.0<br>Material Catalog<br>Management"))
    Supplier -- "Listing Data (Price, Unit, Stock)" --> P2
    P2 -- "Write Listings" --> D2
    D2 -- "Read Active Listings" --> P2
    P2 -- "Browse & Search Results" --> Brand & Factory

    %% Process 3.0: Material Orders & Inventory
    P3(("3.0<br>Material Order Processing<br>& Atomic Inventory"))
    Brand & Factory -- "Order Submission (Cart Items)" --> P3
    D2 -- "Check Stock & Freeze Snapshots" --> P3
    P3 -- "Write Order & Line Snapshots" --> D3
    Supplier -- "Confirm / Reject Decision" --> P3
    P3 -- "Atomic Stock Deduct & StockMovement" --> D2 & D3
    P3 -- "Status Change & Alerts" --> D5
    P3 -- "Order Confirmation Notice" --> Brand & Factory & Supplier

    %% Process 4.0: Manufacturing Requests
    P4(("4.0<br>Manufacturing Request<br>& Negotiation Engine"))
    Brand -- "New Request & Initial Offer" --> P4
    P4 -- "Create Request & Round 1 Offer" --> D4
    Factory -- "Accept / Reject / Counter-Offer" --> P4
    Brand -- "Accept / Reject / Revised Offer" --> P4
    P4 -- "Update Status & Record Offer Rounds" --> D4
    P4 -- "Trigger Request Alerts" --> D5
    P4 -- "Negotiation Updates" --> Brand & Factory

    %% Process 5.0: Audit & Alerts
    P5(("5.0<br>Audit Trail &<br>Notification Dispatch"))
    D5 -- "Fetch Unread Notifications" --> P5
    D5 -- "Fetch Status History" --> P5
    P5 -- "Notification Bell & Badge Count" --> Brand & Factory & Supplier
    P5 -- "Audit Trail History View" --> Brand & Factory & Supplier & Admin
```

---

## 3. Data Dictionary & Store Definitions

| Store ID | Physical Entity Mapping | Description & Governance |
|---|---|---|
| **D1** | `AspNetUsers`, `Organizations`, `VerificationDocuments` | Stores account credentials, hashed passwords, contact details, organization type, and uploaded verification files. Governed by `IVerificationService`. |
| **D2** | `MaterialListings` | Catalog of fabrics and materials. Enforces concurrency token (`RowVersion`) and check constraint `AvailableQuantity >= 0`. Governed by `IMaterialListingService`. |
| **D3** | `MaterialOrders`, `MaterialOrderLines`, `StockMovements` | Contains frozen point-in-time order lines, statuses, totals in EGP, and immutable stock deduction records. Governed by `IMaterialOrderService`. |
| **D4** | `ManufacturingRequests`, `RequestOffers` | Stores garment manufacturing contracts, target factory references, specs, and complete multi-round price negotiation history. Governed by `IManufacturingRequestService`. |
| **D5** | `StatusHistories`, `Notifications` | Append-only system audit ledger recording all state transitions across both workflows (BR-08) and targeted user alerts (BR-10). Governed by `IStatusHistoryService` and `INotificationService`. |

