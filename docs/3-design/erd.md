# SILA (صِلة) — Entity Relationship Diagram (ERD)

> **Document:** Entity Relationship Diagram (Conceptual, Logical & Physical Relationships)  
> **Task:** T1.1 | **Owner:** P2 (Database Engineer)  
> **Status:** APPROVED & MATCHED TO CODE (ApplicationDbContext v1)  
> **Last Updated:** 29 October 2026  

---

## 1. Overview & Core Data Model Principles

SILA is a B2B platform tailored for the Egyptian fashion industry, connecting three primary organization types: **Brand Owner**, **Factory Owner**, and **Supplier**, with an internal lightweight **Admin** for account verification.

### Core Architectural Rules Reflected in ERD:
1. **One Organization per User Account:** `ApplicationUser` has a strict 1-to-1 relationship with `Organization`.
2. **Two Isolated Lifecycles:**
   - **Manufacturing Request Workflow:** Connects `Brand` with `Factory` (`ManufacturingRequest` + `RequestOffer` rounds). Completely isolated from material inventory.
   - **Material Order Workflow:** Connects `Buyer` (Brand or Factory) with `Supplier` (`MaterialOrder` + `MaterialOrderLine` + `StockMovement`).
3. **Data Immutability & Snapshots:** Order lines store frozen snapshots (`MaterialNameSnapshot`, `UnitSnapshot`, `UnitPriceSnapshot`) to guarantee historic fidelity against future catalog edits.
4. **Auditability & Traceability:** Every status change writes an immutable `StatusHistory` record. Stock deductions write `StockMovement` records atomically.

---

## 2. Mermaid Entity Relationship Diagram

```mermaid
erDiagram
    APPLICATION_USER ||--o| ORGANIZATION : "owns (1:1)"
    APPLICATION_USER ||--o{ NOTIFICATION : "receives"
    APPLICATION_USER ||--o{ STATUS_HISTORY : "changed by"

    ORGANIZATION ||--o{ VERIFICATION_DOCUMENT : "submits"
    ORGANIZATION ||--o{ MATERIAL_LISTING : "supplies (as Supplier)"
    ORGANIZATION ||--o{ MATERIAL_ORDER : "places (as Buyer)"
    ORGANIZATION ||--o{ MATERIAL_ORDER : "fulfills (as Supplier)"
    ORGANIZATION ||--o{ MANUFACTURING_REQUEST : "initiates (as Brand)"
    ORGANIZATION ||--o{ MANUFACTURING_REQUEST : "targets (as Factory)"
    ORGANIZATION ||--o{ REQUEST_OFFER : "proposes"

    MATERIAL_ORDER ||--|{ MATERIAL_ORDER_LINE : "contains"
    MATERIAL_LISTING ||--o{ MATERIAL_ORDER_LINE : "referenced in"

    MATERIAL_LISTING ||--o{ STOCK_MOVEMENT : "tracked by"
    MATERIAL_ORDER ||--o{ STOCK_MOVEMENT : "generates"

    MANUFACTURING_REQUEST ||--o{ REQUEST_OFFER : "negotiated via"

    APPLICATION_USER {
        string Id PK
        string FullName
        string Email
        bool EmailConfirmed
        string PasswordHash
        string PhoneNumber
    }

    ORGANIZATION {
        int Id PK
        string OwnerUserId FK "Unique 1:1"
        int Type "Enum: 1=Brand, 2=Factory, 3=Supplier"
        string Name
        string Description
        string City
        string ContactPhone
        string LogoPath
        string InstagramUrl "Brand only"
        string PortfolioText "Brand only"
        string Address "Factory only"
        int VerificationStatus "Enum: 1=Unverified, 2=PendingReview, 3=Verified, 4=Rejected"
        string VerificationNote
        datetime CreatedAt
        datetime UpdatedAt
    }

    VERIFICATION_DOCUMENT {
        int Id PK
        int OrganizationId FK
        string DocumentType
        string FilePath
        datetime UploadedAt
        datetime CreatedAt
    }

    MATERIAL_LISTING {
        int Id PK
        int SupplierOrganizationId FK
        string Name
        string Category
        string Description
        string Unit
        decimal UnitPrice "decimal(18,2)"
        int AvailableQuantity "CHECK >= 0"
        bool IsActive
        byte_array RowVersion "rowversion concurrency token"
        datetime CreatedAt
        datetime UpdatedAt
    }

    MATERIAL_ORDER {
        int Id PK
        int BuyerOrganizationId FK
        int SupplierOrganizationId FK
        int Status "Enum: 1=PendingReview, 2=Confirmed, 3=Rejected, 4=Dispatched, 5=Delivered, 6=Cancelled"
        decimal TotalAmount "decimal(18,2)"
        string RejectionReason
        datetime CreatedAt
        datetime UpdatedAt
    }

    MATERIAL_ORDER_LINE {
        int Id PK
        int MaterialOrderId FK
        int MaterialListingId FK
        string MaterialNameSnapshot
        string UnitSnapshot
        decimal UnitPriceSnapshot "decimal(18,2)"
        int Quantity
        decimal LineTotal "decimal(18,2)"
        datetime CreatedAt
    }

    STOCK_MOVEMENT {
        int Id PK
        int MaterialListingId FK
        int MaterialOrderId FK
        int QuantityChange "Negative for order deduction"
        datetime CreatedAt
    }

    MANUFACTURING_REQUEST {
        int Id PK
        int BrandOrganizationId FK
        int FactoryOrganizationId FK
        string ProductType
        string DesignSpecification
        string MaterialDescription
        int Quantity
        decimal CurrentOfferPrice "decimal(18,2)"
        string AttachmentPath
        string Notes
        int Status "Enum: 1=PendingApproval, 2=CounterOffered, 3=Accepted, 4=Rejected, 5=Cancelled"
        string DecisionNote
        datetime CreatedAt
        datetime UpdatedAt
    }

    REQUEST_OFFER {
        int Id PK
        int ManufacturingRequestId FK
        int OfferedByOrganizationId FK
        decimal UnitPrice "decimal(18,2)"
        string Note
        int RoundNumber
        datetime CreatedAt
    }

    STATUS_HISTORY {
        int Id PK
        int RecordType "Enum: 1=Request, 2=Order"
        int RecordId
        string FromStatus
        string ToStatus
        string ChangedByUserId FK
        string Note
        datetime ChangedAt
        datetime CreatedAt
    }

    NOTIFICATION {
        int Id PK
        string RecipientUserId FK
        string Message
        string LinkUrl
        bool IsRead
        datetime CreatedAt
    }
```

---

## 3. Relationship Cardinality & Integrity Rules

| Source Entity | Target Entity | Cardinality | Foreign Key | Delete Rule | Description |
|---|---|---|---|---|---|
| `AspNetUsers` | `Organizations` | 1 — 0..1 | `Organizations.OwnerUserId` | **RESTRICT** | Each user account owns exactly one business profile; organization cannot be orphaned. |
| `Organizations` | `VerificationDocuments` | 1 — 0..* | `VerificationDocuments.OrganizationId` | **RESTRICT** | Proof documents submitted during verification review. |
| `Organizations` | `MaterialListings` | 1 — 0..* | `MaterialListings.SupplierOrganizationId` | **RESTRICT** | Material catalog listings owned by a Supplier organization. |
| `Organizations` | `MaterialOrders` | 1 — 0..* | `MaterialOrders.BuyerOrganizationId` | **RESTRICT** | Orders submitted by a Buyer (Brand or Factory). |
| `Organizations` | `MaterialOrders` | 1 — 0..* | `MaterialOrders.SupplierOrganizationId` | **RESTRICT** | Orders fulfilled by a Supplier. All lines must belong to this supplier (BR-02). |
| `MaterialOrders` | `MaterialOrderLines` | 1 — 1..* | `MaterialOrderLines.MaterialOrderId` | **RESTRICT** | One or more items purchased in a single order. |
| `MaterialListings` | `MaterialOrderLines` | 1 — 0..* | `MaterialOrderLines.MaterialListingId` | **RESTRICT** | Historical link. Deletion blocked (BR-05); listings are deactivated instead. |
| `MaterialListings` | `StockMovements` | 1 — 0..* | `StockMovements.MaterialListingId` | **RESTRICT** | Audit trail of inventory reductions. |
| `MaterialOrders` | `StockMovements` | 1 — 0..* | `StockMovements.MaterialOrderId` | **RESTRICT** | Trace which confirmed order produced which stock reduction. |
| `Organizations` | `ManufacturingRequests` | 1 — 0..* | `ManufacturingRequests.BrandOrganizationId` | **RESTRICT** | Production request submitted by a Brand. |
| `Organizations` | `ManufacturingRequests` | 1 — 0..* | `ManufacturingRequests.FactoryOrganizationId` | **RESTRICT** | Target factory selected by the Brand (D-01). |
| `ManufacturingRequests` | `RequestOffers` | 1 — 0..* | `RequestOffers.ManufacturingRequestId` | **RESTRICT** | Offer and counter-offer negotiation rounds. |
| `Organizations` | `RequestOffers` | 1 — 0..* | `RequestOffers.OfferedByOrganizationId` | **RESTRICT** | Party that proposed the unit price for this round. |
| `AspNetUsers` | `Notifications` | 1 — 0..* | `Notifications.RecipientUserId` | **RESTRICT** | Targeted in-app alerts for state-changing events. |
| `AspNetUsers` | `StatusHistories` | 1 — 0..* | `StatusHistories.ChangedByUserId` | **RESTRICT** | User who executed the status transition. |
