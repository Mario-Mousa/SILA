# SILA (صِلة) — Logical and Physical Schema Description

> **Document:** Database Schema Specification & Normalization Notes  
> **Task:** T1.1 | **Owner:** P2 (Database Engineer)  
> **Target Database Engine:** Microsoft SQL Server (LocalDB / Express / Azure SQL)  
> **Framework:** Entity Framework Core (Code-First Migrations)  
> **Last Updated:** 29 October 2026  

---

## 1. Executive Summary & Design Principles

The SILA database schema is engineered specifically to model the B2B fashion manufacturing and raw material supply chain in Egypt. It adheres strictly to the architectural constraints outlined in the project foundation:

1. **Clean Separation of Concerns:**
   - **Identity Layer:** Managed by ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.).
   - **Domain Layer:** High-cohesion domain models for Organizations, Listings, Orders, Manufacturing Requests, and Audit records.
2. **Strict Financial & Quantity Integrity:**
   - All monetary figures (`UnitPrice`, `TotalAmount`, `LineTotal`, `CurrentOfferPrice`) are strictly typed as `decimal(18,2)` in Egyptian Pounds (EGP). Floating-point arithmetic (`float`/`double`) is prohibited.
   - Negative inventory is blocked at the physical database layer via a check constraint: `[AvailableQuantity] >= 0`.
   - Concurrency conflicts during concurrent checkouts/confirmations are guarded by SQL Server `rowversion` (`byte[] RowVersion`).
3. **Immutability of Orders & Audits:**
   - Material order lines snapshot prices, units, and listing names at the exact time of order placement (`MaterialNameSnapshot`, `UnitPriceSnapshot`, `UnitSnapshot`). Subsequent price revisions or catalog deactivations do not alter historical records.
   - Status transitions are recorded into an append-only `StatusHistories` ledger.
4. **Referential Integrity:**
   - All foreign keys default to `ON DELETE NO ACTION` (`DeleteBehavior.Restrict`). No cascading deletes are permitted to prevent accidental loss of operational history.

---

## 2. Normalization Analysis (1NF, 2NF, 3NF & Intentional Denormalization)

### First Normal Form (1NF)
- Every table possesses a defined Primary Key (`Id` identity column for domain tables; string GUID for Identity users).
- All columns store atomic, scalar values (no multivalued attributes, arrays, or delimited strings).
- Column ordering is independent of data semantics.

### Second Normal Form (2NF)
- The schema is in 1NF.
- All non-key attributes are fully functionally dependent on the entire primary key. In all tables with synthetic single-column primary keys (`Id`), partial dependency is mathematically impossible.

### Third Normal Form (3NF)
- The schema is in 2NF.
- All non-key attributes are non-transitively dependent on the primary key ($X \to Y$ where $Y$ is non-key implies $X$ is a superkey).

### Deliberate Denormalization (Snapshot Pattern — BR-04)
- **Table:** `MaterialOrderLines` contains `MaterialNameSnapshot`, `UnitSnapshot`, and `UnitPriceSnapshot`.
- **Rationale:** If `MaterialOrderLines` merely joined with `MaterialListings` for name and price, any subsequent price adjustment or renaming by the supplier would retroactively alter the financial balance of finalized historical orders. The snapshot columns create an immutable point-in-time value, preserving accounting integrity.

---

## 3. Physical Schema Specification & Data Dictionary

### 3.1 `Organizations`
Represents the business profile for a Brand Owner, Factory Owner, or Raw Material Supplier.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Unique organization identifier |
| `OwnerUserId` | `nvarchar(450)` | No | FK to `AspNetUsers(Id)`, Unique Index | Identity user who owns this organization (1:1) |
| `Type` | `int` | No | Enum: 1=Brand, 2=Factory, 3=Supplier | Role category of organization |
| `Name` | `nvarchar(150)` | No | Indexed | Registered trade or company name |
| `City` | `nvarchar(100)` | No | Required | City or governorate in Egypt |
| `ContactPhone` | `nvarchar(30)` | No | Required | Official contact telephone number |
| `Description` | `nvarchar(1000)` | Yes | — | Overview of products, capability, or raw materials |
| `LogoPath` | `nvarchar(300)` | Yes | — | Relative path to uploaded organization logo |
| `InstagramUrl` | `nvarchar(300)` | Yes | Brand specific | Instagram or social portfolio URL |
| `PortfolioText` | `nvarchar(2000)` | Yes | Brand specific | Past collections and portfolio description |
| `Address` | `nvarchar(300)` | Yes | Factory specific | Physical factory workshop address |
| `VerificationStatus` | `int` | No | Enum: 1=Unverified, 2=PendingReview, 3=Verified, 4=Rejected | Admin verification stage |
| `VerificationNote` | `nvarchar(500)` | Yes | — | Admin review notes or rejection feedback |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Creation timestamp (UTC) |
| `UpdatedAt` | `datetime2` | Yes | — | Last update timestamp (UTC) |

**Indexes:**
- `IX_Organizations_OwnerUserId` (Unique)
- `IX_Organizations_Type`
- `IX_Organizations_VerificationStatus`

---

### 3.2 `VerificationDocuments`
Stores uploaded verification proof files for Admin approval.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Document record identifier |
| `OrganizationId` | `int` | No | FK to `Organizations(Id)` | Organization submitting verification |
| `DocumentType` | `nvarchar(50)` | No | Required | E.g. "TaxCard", "CommercialRegister", "License" |
| `FilePath` | `nvarchar(500)` | No | Required | Secure storage path outside `wwwroot` |
| `UploadedAt` | `datetime2` | No | Default `GETUTCDATE()` | Upload timestamp (UTC) |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Record creation timestamp (UTC) |

**Indexes:**
- `IX_VerificationDocuments_OrganizationId`

---

### 3.3 `MaterialListings`
Catalog items listed by Raw Material Suppliers (fabrics, buttons, zippers, accessories).

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Material listing identifier |
| `SupplierOrganizationId` | `int` | No | FK to `Organizations(Id)` | Supplier owning this listing |
| `Name` | `nvarchar(150)` | No | Required | Name of material (e.g., "12oz Denim Twill") |
| `Category` | `nvarchar(100)` | No | Indexed | Material category (e.g., "Cotton", "Denim") |
| `Description` | `nvarchar(2000)` | Yes | — | Technical specs, composition, finish |
| `Unit` | `nvarchar(30)` | No | Required | Measurement unit (e.g., "meter", "kg", "yard") |
| `UnitPrice` | `decimal(18,2)` | No | Required | Unit price in EGP |
| `AvailableQuantity` | `int` | No | `CHECK ([AvailableQuantity] >= 0)` | Available physical stock |
| `IsActive` | `bit` | No | Default 1, Indexed | Active in catalog (0 = deactivated) |
| `RowVersion` | `rowversion` | No | Concurrency Token | Optimistic concurrency token |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Listing creation timestamp (UTC) |
| `UpdatedAt` | `datetime2` | Yes | — | Last modification timestamp (UTC) |

**Indexes & Constraints:**
- `CK_MaterialListing_AvailableQuantity_NonNegative` (`[AvailableQuantity] >= 0`)
- `IX_MaterialListings_SupplierOrganizationId`
- `IX_MaterialListings_Category`
- `IX_MaterialListings_IsActive`

---

### 3.4 `MaterialOrders`
Purchases of raw materials placed by Buyers (Brand Owners or Factory Owners) to Suppliers.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Order identifier |
| `BuyerOrganizationId` | `int` | No | FK to `Organizations(Id)` | Purchasing brand or factory |
| `SupplierOrganizationId` | `int` | No | FK to `Organizations(Id)` | Supplier fulfilling the order |
| `Status` | `int` | No | Enum: 1=PendingReview, 2=Confirmed, 3=Rejected, 4=Dispatched, 5=Delivered, 6=Cancelled | Current workflow status |
| `TotalAmount` | `decimal(18,2)` | No | Required | Calculated total in EGP |
| `RejectionReason` | `nvarchar(500)` | Yes | Required if Status=Rejected | Reason supplier declined order |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Order submission timestamp (UTC) |
| `UpdatedAt` | `datetime2` | Yes | — | Status change timestamp (UTC) |

**Indexes:**
- `IX_MaterialOrders_BuyerOrganizationId`
- `IX_MaterialOrders_SupplierOrganizationId`
- `IX_MaterialOrders_Status`

---

### 3.5 `MaterialOrderLines`
Individual line items inside a `MaterialOrder`.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Line item identifier |
| `MaterialOrderId` | `int` | No | FK to `MaterialOrders(Id)` | Parent order |
| `MaterialListingId` | `int` | No | FK to `MaterialListings(Id)` | Reference to original listing |
| `MaterialNameSnapshot` | `nvarchar(150)` | No | Required | Material name at time of order |
| `UnitSnapshot` | `nvarchar(30)` | No | Required | Measurement unit at time of order |
| `UnitPriceSnapshot` | `decimal(18,2)` | No | Required | Unit price at time of order |
| `Quantity` | `int` | No | Required | Quantity ordered |
| `LineTotal` | `decimal(18,2)` | No | Required | `Quantity * UnitPriceSnapshot` |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Creation timestamp (UTC) |

**Indexes:**
- `IX_MaterialOrderLines_MaterialOrderId`
- `IX_MaterialOrderLines_MaterialListingId`

---

### 3.6 `StockMovements`
Immutable inventory movement audit log.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Movement record identifier |
| `MaterialListingId` | `int` | No | FK to `MaterialListings(Id)` | Affected listing |
| `MaterialOrderId` | `int` | No | FK to `MaterialOrders(Id)` | Associated order triggering change |
| `QuantityChange` | `int` | No | Required | Negative for deductions, positive for returns |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Transaction timestamp (UTC) |

**Indexes:**
- `IX_StockMovements_MaterialListingId`
- `IX_StockMovements_MaterialOrderId`

---

### 3.7 `ManufacturingRequests`
Production agreements initiated by Brand Owners targeting a specific Clothing Factory.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Request identifier |
| `BrandOrganizationId` | `int` | No | FK to `Organizations(Id)` | Requesting brand owner |
| `FactoryOrganizationId` | `int` | No | FK to `Organizations(Id)` | Target factory (D-01) |
| `ProductType` | `nvarchar(100)` | No | Required | Product category (e.g., "Jeans", "Hoodie") |
| `DesignSpecification` | `nvarchar(2000)` | No | Required | Tech-pack notes, sizing, stitching instructions |
| `MaterialDescription` | `nvarchar(2000)` | No | Required | Fabric specifications or requirements |
| `Quantity` | `int` | No | Required | Number of units to produce |
| `CurrentOfferPrice` | `decimal(18,2)` | No | Required | Current unit offer price in EGP |
| `AttachmentPath` | `nvarchar(500)` | Yes | — | Link or relative path to tech pack attachment |
| `Notes` | `nvarchar(1000)` | Yes | — | Additional brand notes or timeline targets |
| `Status` | `int` | No | Enum: 1=PendingApproval, 2=CounterOffered, 3=Accepted, 4=Rejected, 5=Cancelled | Current negotiation state |
| `DecisionNote` | `nvarchar(1000)` | Yes | — | Factory decision note or reason |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Submission timestamp (UTC) |
| `UpdatedAt` | `datetime2` | Yes | — | Last transition timestamp (UTC) |

**Indexes:**
- `IX_ManufacturingRequests_BrandOrganizationId`
- `IX_ManufacturingRequests_FactoryOrganizationId`
- `IX_ManufacturingRequests_Status`

---

### 3.8 `RequestOffers`
Audit of negotiation rounds (offers, counter-offers, revised offers) for a Manufacturing Request.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Offer round identifier |
| `ManufacturingRequestId` | `int` | No | FK to `ManufacturingRequests(Id)` | Parent request |
| `OfferedByOrganizationId` | `int` | No | FK to `Organizations(Id)` | Organization making the offer |
| `UnitPrice` | `decimal(18,2)` | No | Required | Proposed price per unit in EGP |
| `Note` | `nvarchar(500)` | Yes | — | Justification or clarification note |
| `RoundNumber` | `int` | No | Required | Sequence round number (1, 2, 3...) |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Offer timestamp (UTC) |

**Indexes:**
- `IX_RequestOffers_ManufacturingRequestId`
- `IX_RequestOffers_OfferedByOrganizationId`

---

### 3.9 `StatusHistories`
Universal append-only audit trail for all order and request status transitions.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Audit entry identifier |
| `RecordType` | `int` | No | Enum: 1=Request, 2=Order | Entity type |
| `RecordId` | `int` | No | Required | Foreign entity ID (`ManufacturingRequestId` or `MaterialOrderId`) |
| `FromStatus` | `nvarchar(50)` | No | Required | Prior status name |
| `ToStatus` | `nvarchar(50)` | No | Required | New status name |
| `ChangedByUserId` | `nvarchar(450)` | No | FK to `AspNetUsers(Id)` | User executing the status transition |
| `Note` | `nvarchar(500)` | Yes | — | Decision note or reason |
| `ChangedAt` | `datetime2` | No | Default `GETUTCDATE()` | Transition timestamp (UTC) |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Creation timestamp (UTC) |

**Indexes:**
- `IX_StatusHistories_RecordType_RecordId` (Composite Index)
- `IX_StatusHistories_ChangedByUserId`

---

### 3.10 `Notifications`
Targeted in-app notifications for users.

| Column | Data Type | Nullable | Constraints & Defaults | Description |
|---|---|---|---|---|
| `Id` | `int` | No | PK, `IDENTITY(1,1)` | Notification identifier |
| `RecipientUserId` | `nvarchar(450)` | No | FK to `AspNetUsers(Id)` | Target recipient |
| `Message` | `nvarchar(500)` | No | Required | Notification message text |
| `LinkUrl` | `nvarchar(300)` | Yes | — | Target URL or action route |
| `IsRead` | `bit` | No | Default 0 | Read flag |
| `CreatedAt` | `datetime2` | No | Default `GETUTCDATE()` | Event timestamp (UTC) |

**Indexes:**
- `IX_Notifications_RecipientUserId_IsRead` (Composite Index)

---

## 4. Verification and Concurrency Strategies

### 4.1 Stock Concurrency Guard (BR-03)
When confirming an order, the system executes an atomic conditional update:
```sql
UPDATE [MaterialListings]
SET [AvailableQuantity] = [AvailableQuantity] - @QuantityOrdered
WHERE [Id] = @ListingId AND [AvailableQuantity] >= @QuantityOrdered;
```
If the number of affected rows equals 0, the transaction immediately rolls back, guaranteeing that race conditions between multiple buyers cannot produce negative stock or overselling.

### 4.2 Check Constraint Defense in Depth
Even if application logic were bypassed, SQL Server enforces:
```sql
CONSTRAINT [CK_MaterialListing_AvailableQuantity_NonNegative] CHECK ([AvailableQuantity] >= 0)
```
Any operation attempting to reduce `AvailableQuantity` below zero will abort with a database violation error.
