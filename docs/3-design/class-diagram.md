<!--
═══════════ HANDOFF ═══════════════════════════════════
Task:        T1.4 | Owner: P4 (QA & Business Analyst) | Status: DONE
What exists: Comprehensive Domain Class Diagram (Mermaid classDiagram) and detailed structural descriptions of all entities, enums, relationships, attributes, and data constraints, strictly matching T1.1 physical schema.
What is left: // TODO(T4.1): Formulate unit tests validating entity constraints, relationships, and foreign key cascades.
Depends on:  T1.1 (Models/Entities/*, Models/Enums/*, ApplicationDbContext)
Used by:     All developers (P1, P2, P3, P4)
How to test: Verify that every entity property, type, and relationship matches Models/Entities/* in src/SILA.Web/.
Notes:       All decimal fields are typed as decimal(18,2) in EGP. Concurrency token RowVersion is placed on MaterialListing.
═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Domain Class Diagram

> **Document:** Domain Model & Object-Oriented Class Specifications  
> **Task:** T1.4 | **Owner:** P4 (QA & Business Analyst)  
> **Target Framework:** Entity Framework Core 8.0 / C# 12  
> **Last Updated:** 29 October 2026  

---

## 1. Domain Model Overview

The SILA object model is structured around Clean MVC principles. Domain entities encapsulate database persistence and navigational relationships. Financial values are strictly represented as `decimal(18,2)` in Egyptian Pounds (EGP). All entities maintain a `CreatedAt` audit timestamp, with mutable business records featuring `UpdatedAt`.

---

## 2. Mermaid Class Diagram

```mermaid
classDiagram
    %% Enumerations
    class OrganizationType {
        <<enumeration>>
        Brand = 1
        Factory = 2
        Supplier = 3
    }

    class VerificationStatus {
        <<enumeration>>
        Unverified = 1
        PendingReview = 2
        Verified = 3
        Rejected = 4
    }

    class ManufacturingRequestStatus {
        <<enumeration>>
        PendingApproval = 1
        CounterOffered = 2
        Accepted = 3
        Rejected = 4
        Cancelled = 5
    }

    class MaterialOrderStatus {
        <<enumeration>>
        PendingReview = 1
        Confirmed = 2
        Rejected = 3
        Dispatched = 4
        Delivered = 5
        Cancelled = 6
    }

    class StatusRecordType {
        <<enumeration>>
        Request = 1
        Order = 2
    }

    %% Domain Entities
    class ApplicationUser {
        +string Id
        +string FullName
        +string Email
        +bool EmailConfirmed
        +string PasswordHash
        +string PhoneNumber
        +Organization Organization
        +List~Notification~ Notifications
        +List~StatusHistory~ StatusHistories
    }

    class Organization {
        +int Id
        +string OwnerUserId
        +OrganizationType Type
        +string Name
        +string Description
        +string City
        +string ContactPhone
        +string LogoPath
        +string InstagramUrl
        +string PortfolioText
        +string Address
        +VerificationStatus VerificationStatus
        +string VerificationNote
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +ApplicationUser OwnerUser
        +List~VerificationDocument~ VerificationDocuments
        +List~MaterialListing~ MaterialListings
        +List~MaterialOrder~ MaterialOrdersAsBuyer
        +List~MaterialOrder~ MaterialOrdersAsSupplier
        +List~ManufacturingRequest~ ManufacturingRequestsAsBrand
        +List~ManufacturingRequest~ ManufacturingRequestsAsFactory
        +List~RequestOffer~ RequestOffers
    }

    class VerificationDocument {
        +int Id
        +int OrganizationId
        +string DocumentType
        +string FilePath
        +DateTime UploadedAt
        +DateTime CreatedAt
        +Organization Organization
    }

    class MaterialListing {
        +int Id
        +int SupplierOrganizationId
        +string Name
        +string Category
        +string Description
        +string Unit
        +decimal UnitPrice
        +int AvailableQuantity
        +bool IsActive
        +byte[] RowVersion
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +Organization SupplierOrganization
        +List~MaterialOrderLine~ MaterialOrderLines
        +List~StockMovement~ StockMovements
    }

    class MaterialOrder {
        +int Id
        +int BuyerOrganizationId
        +int SupplierOrganizationId
        +MaterialOrderStatus Status
        +decimal TotalAmount
        +string RejectionReason
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +Organization BuyerOrganization
        +Organization SupplierOrganization
        +List~MaterialOrderLine~ Lines
        +List~StockMovement~ StockMovements
    }

    class MaterialOrderLine {
        +int Id
        +int MaterialOrderId
        +int MaterialListingId
        +string MaterialNameSnapshot
        +string UnitSnapshot
        +decimal UnitPriceSnapshot
        +int Quantity
        +decimal LineTotal
        +MaterialOrder MaterialOrder
        +MaterialListing MaterialListing
    }

    class ManufacturingRequest {
        +int Id
        +int BrandOrganizationId
        +int FactoryOrganizationId
        +string ProductType
        +string DesignSpecification
        +string MaterialDescription
        +int Quantity
        +decimal CurrentOfferPrice
        +string AttachmentPath
        +string Notes
        +ManufacturingRequestStatus Status
        +string DecisionNote
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +Organization BrandOrganization
        +Organization FactoryOrganization
        +List~RequestOffer~ Offers
    }

    class RequestOffer {
        +int Id
        +int ManufacturingRequestId
        +int OfferedByOrganizationId
        +decimal UnitPrice
        +string Note
        +int RoundNumber
        +DateTime CreatedAt
        +ManufacturingRequest ManufacturingRequest
        +Organization OfferedByOrganization
    }

    class StatusHistory {
        +int Id
        +StatusRecordType RecordType
        +int RecordId
        +string FromStatus
        +string ToStatus
        +string ChangedByUserId
        +string Note
        +DateTime ChangedAt
        +ApplicationUser ChangedByUser
    }

    class StockMovement {
        +int Id
        +int MaterialListingId
        +int MaterialOrderId
        +int QuantityChange
        +DateTime CreatedAt
        +MaterialListing MaterialListing
        +MaterialOrder MaterialOrder
    }

    class Notification {
        +int Id
        +string RecipientUserId
        +string Message
        +string LinkUrl
        +bool IsRead
        +DateTime CreatedAt
        +ApplicationUser RecipientUser
    }

    %% Relationships
    ApplicationUser "1" -- "0..1" Organization : owns
    ApplicationUser "1" -- "0..*" Notification : receives
    ApplicationUser "1" -- "0..*" StatusHistory : records

    Organization "1" -- "0..*" VerificationDocument : submits
    Organization "1" -- "0..*" MaterialListing : catalogs (Supplier)
    Organization "1" -- "0..*" MaterialOrder : places (Buyer)
    Organization "1" -- "0..*" MaterialOrder : receives (Supplier)
    Organization "1" -- "0..*" ManufacturingRequest : initiates (Brand)
    Organization "1" -- "0..*" ManufacturingRequest : receives (Factory)
    Organization "1" -- "0..*" RequestOffer : offers

    MaterialOrder "1" -- "1..*" MaterialOrderLine : contains
    MaterialListing "1" -- "0..*" MaterialOrderLine : referenced
    MaterialListing "1" -- "0..*" StockMovement : tracks
    MaterialOrder "1" -- "0..*" StockMovement : generates

    ManufacturingRequest "1" -- "1..*" RequestOffer : negotiates

    Organization ..> OrganizationType : typed by
    Organization ..> VerificationStatus : status
    ManufacturingRequest ..> ManufacturingRequestStatus : status
    MaterialOrder ..> MaterialOrderStatus : status
    StatusHistory ..> StatusRecordType : categorizes
```

---

## 3. Structural Integrity & Relationship Analysis

1. **User to Organization Mapping (1:1):** `ApplicationUser` owns exactly one `Organization` via a unique foreign key index on `OwnerUserId`.
2. **Snapshot Immutability (BR-04):** `MaterialOrderLine` references `MaterialListingId` as a foreign key (`DeleteBehavior.Restrict`), but stores frozen snapshots of name, unit, and unit price. Deleting or modifying a catalog listing has zero retrospective impact on past orders.
3. **Concurrency Token (`RowVersion`):** `MaterialListing` includes a SQL Server timestamp/rowversion byte array column (`RowVersion`) mapped as `[Timestamp]`. During `IMaterialOrderService.ConfirmOrderAsync`, EF Core verifies that the row has not changed between reading available stock and executing deduction.
4. **Non-Negative Stock Constraint (BR-01):** Physical SQL constraint `CHECK (AvailableQuantity >= 0)` guarantees that simultaneous orders cannot force stock negative.
5. **Polymorphic Status Audit Logging (BR-08):** `StatusHistory` references either a `ManufacturingRequest` or a `MaterialOrder` using the pair `(RecordType, RecordId)`, accompanied by immutable actor references and timestamps.

