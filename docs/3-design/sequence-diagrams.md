<!--
═══════════ HANDOFF ═══════════════════════════════════
Task:        T1.4 | Owner: P4 (QA & Business Analyst) | Status: DONE
What exists: Three comprehensive Mermaid Sequence Diagrams modeling (1) User Registration, Email Confirmation & Admin Verification, (2) Material Order Placement & Atomic Stock Confirmation with RowVersion Concurrency Token, and (3) Manufacturing Request Multi-Round Negotiation.
What is left: // TODO(T4.1): Use sequence interactions to write end-to-end integration test mocks and assertions.
Depends on:  T1.1 (Data Model & Schema), T1.2 (Service Contracts)
Used by:     P1 (T2.1, T3.2), P2 (T2.2, T3.3), P4 (T2.4, T4.1)
How to test: Verify exact method signatures and transaction boundaries (IDbContextTransaction, CommitAsync, RollbackAsync); verify snapshot creation and audit logging.
Notes:       All interactions strictly separate UI Controllers from Service Business Logic.
═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Sequence Diagrams

> **Document:** Component Interaction & Object Sequence Models  
> **Task:** T1.4 | **Owner:** P4 (QA & Business Analyst)  
> **Academic Initiative:** DEPI — Full Stack .NET Track  
> **Last Updated:** 29 October 2026  

---

## 1. Sequence Diagram 1: Registration, Email Confirmation & Admin Verification

Models user onboarding, SMTP verification link issuance, document upload, and administrative approval:

```mermaid
sequenceDiagram
    autonumber
    actor User as Prospective User
    participant Browser
    participant AccountCtrl as AccountController
    participant UserMgr as UserManager<ApplicationUser>
    participant EmailSender as IEmailSender
    participant VerifCtrl as Profile / AdminController
    participant VerifSvc as IVerificationService
    participant Db as ApplicationDbContext
    actor Admin as System Admin

    User->>Browser: Fill Registration Form (Role, Org Name, Password)
    Browser->>AccountCtrl: POST /Account/Register(RegisterViewModel)
    AccountCtrl->>UserMgr: CreateAsync(ApplicationUser, Password)
    UserMgr-->>AccountCtrl: Success (User Created, Hashed)
    AccountCtrl->>Db: Organizations.Add(new Organization { VerificationStatus = Unverified })
    AccountCtrl->>Db: SaveChangesAsync()
    AccountCtrl->>UserMgr: GenerateEmailConfirmationTokenAsync(user)
    UserMgr-->>AccountCtrl: confirmationToken
    AccountCtrl->>EmailSender: SendEmailAsync(email, "Confirm Email", confirmationLink)
    AccountCtrl-->>Browser: Redirect to "Registration Pending Email Confirmation"

    Note over User,EmailSender: User accesses external email inbox and clicks link
    User->>Browser: Click Confirmation Link
    Browser->>AccountCtrl: GET /Account/ConfirmEmail(userId, token)
    AccountCtrl->>UserMgr: ConfirmEmailAsync(user, token)
    UserMgr-->>AccountCtrl: IdentityResult.Success
    AccountCtrl-->>Browser: View "Email Confirmed! Please Sign In."

    User->>Browser: Log in & Upload Role Verification Documents
    Browser->>VerifCtrl: POST /Profile/UploadVerification(DocumentUploadModel)
    VerifCtrl->>VerifSvc: SubmitVerificationAsync(orgId, file, docType)
    VerifSvc->>Db: VerificationDocuments.Add(...)
    VerifSvc->>Db: Organization.VerificationStatus = PendingReview
    VerifSvc->>Db: SaveChangesAsync()
    VerifSvc-->>VerifCtrl: Success
    VerifCtrl-->>Browser: View "Documents Submitted - Pending Admin Review"

    Note over Admin,VerifCtrl: Admin logs into review portal
    Admin->>Browser: View Pending Verifications Dashboard
    Browser->>VerifCtrl: GET /Admin/PendingVerifications
    VerifCtrl->>VerifSvc: GetPendingVerificationsAsync()
    VerifSvc-->>VerifCtrl: List<OrganizationVerificationDto>
    VerifCtrl-->>Browser: Render Pending Review List
    Admin->>Browser: Click Approve(orgId, note)
    Browser->>VerifCtrl: POST /Admin/ApproveOrganization(orgId)
    VerifCtrl->>VerifSvc: ApproveOrganizationAsync(orgId, note)
    VerifSvc->>Db: Organization.VerificationStatus = Verified
    VerifSvc->>Db: Notifications.Add(new Notification { Recipient = Owner, Message = "Verified!" })
    VerifSvc->>Db: SaveChangesAsync()
    VerifSvc-->>VerifCtrl: Success
    VerifCtrl-->>Browser: Refresh Dashboard (Approved)
```

---

## 2. Sequence Diagram 2: Material Order Placement & Atomic Supplier Confirmation

Models order placement with frozen snapshots, followed by atomic stock verification, rowversion concurrency check, inventory deduction, and audit logging during confirmation:

```mermaid
sequenceDiagram
    autonumber
    actor Buyer as Authenticated Buyer
    participant Browser
    participant OrderCtrl as OrdersController
    participant OrderSvc as IMaterialOrderService
    participant OwnSvc as IOwnershipService
    participant Db as ApplicationDbContext
    participant Tx as IDbContextTransaction
    actor Supplier as Target Supplier

    Buyer->>Browser: Add Items from Single Supplier to Cart & Checkout
    Browser->>OrderCtrl: POST /Orders/Create(CreateOrderViewModel)
    OrderCtrl->>OrderSvc: CreateOrderAsync(buyerOrgId, cartItems)
    Note over OrderSvc: BR-02: Verify all listings belong to same Supplier
    OrderSvc->>Db: MaterialListings.Where(id in cartItems.Keys)
    Db-->>OrderSvc: List<MaterialListing>
    
    Note over OrderSvc: BR-04: Freeze Name, Unit & Price Snapshots
    OrderSvc->>Db: MaterialOrders.Add(order with LineSnapshots, Status = PendingReview)
    OrderSvc->>Db: Notifications.Add(new Notification { Recipient = Supplier, Message = "New Order" })
    OrderSvc->>Db: SaveChangesAsync()
    OrderSvc-->>OrderCtrl: Created Order Id
    OrderCtrl-->>Browser: Redirect to Order Details (Status = PendingReview)

    Note over Supplier,OrderCtrl: Supplier opens order and clicks Confirm
    Supplier->>Browser: Click Confirm Order
    Browser->>OrderCtrl: POST /Orders/Confirm(orderId)
    OrderCtrl->>OwnSvc: EnsureSupplierOwnershipAsync(supplierOrgId, orderId)
    OwnSvc-->>OrderCtrl: Authorized

    OrderCtrl->>OrderSvc: ConfirmOrderAsync(orderId, supplierUserId)
    OrderSvc->>Db: BeginTransactionAsync()
    Db-->>OrderSvc: tx (IDbContextTransaction)

    Note over OrderSvc,Db: Atomic Inventory Lock & Validation (BR-03)
    OrderSvc->>Db: MaterialOrders.Include(Lines).FirstOrDefault(orderId)
    Db-->>OrderSvc: orderWithLines

    loop For each MaterialOrderLine in orderWithLines
        OrderSvc->>Db: MaterialListings.Find(line.MaterialListingId)
        Db-->>OrderSvc: listing
        alt listing.AvailableQuantity < line.Quantity
            OrderSvc->>Tx: RollbackAsync()
            OrderSvc-->>OrderCtrl: Throw InsufficientStockException("Out of stock")
            OrderCtrl-->>Browser: Display Friendly Error: "Stock depleted by concurrent checkout"
        else listing.AvailableQuantity >= line.Quantity
            OrderSvc->>listing: AvailableQuantity -= line.Quantity
            OrderSvc->>Db: StockMovements.Add(new StockMovement { QuantityChange = -line.Quantity })
        end
    end

    OrderSvc->>orderWithLines: Status = Confirmed
    OrderSvc->>Db: StatusHistories.Add(new StatusHistory { From = PendingReview, To = Confirmed })
    OrderSvc->>Db: Notifications.Add(new Notification { Recipient = Buyer, Message = "Order Confirmed!" })
    OrderSvc->>Db: SaveChangesAsync()
    OrderSvc->>Tx: CommitAsync()
    OrderSvc-->>OrderCtrl: OrderConfirmedResult
    OrderCtrl-->>Browser: Display Success & Status: Confirmed
```

---

## 3. Sequence Diagram 3: Manufacturing Request Negotiation Lifecycle

Models request submission, factory counter-offer, brand offer revision, and final factory acceptance:

```mermaid
sequenceDiagram
    autonumber
    actor Brand as Brand Owner
    participant Browser
    participant ReqCtrl as ManufacturingRequestsController
    participant ReqSvc as IManufacturingRequestService
    participant NotifSvc as INotificationService
    participant Db as ApplicationDbContext
    actor Factory as Target Factory Owner

    Brand->>Browser: Submit Request (Target Factory, Spec, Qty: 100, Offer: 200 EGP)
    Browser->>ReqCtrl: POST /ManufacturingRequests/Create(CreateRequestViewModel)
    ReqCtrl->>ReqSvc: CreateRequestAsync(brandOrgId, requestDto)
    ReqSvc->>Db: ManufacturingRequests.Add(Status = PendingApproval, CurrentOffer = 200)
    ReqSvc->>Db: RequestOffers.Add(RoundNumber = 1, Price = 200, OfferedBy = Brand)
    ReqSvc->>NotifSvc: SendNotificationAsync(FactoryUser, "New Manufacturing Request Received")
    ReqSvc->>Db: SaveChangesAsync()
    ReqSvc-->>ReqCtrl: RequestId
    ReqCtrl-->>Browser: View Request Details (Status = PendingApproval)

    Note over Factory,ReqCtrl: Factory evaluates request and sends Counter-Offer
    Factory->>Browser: Submit Counter-Offer (250 EGP, "Heavier denim requires more time")
    Browser->>ReqCtrl: POST /ManufacturingRequests/CounterOffer(requestId, 250, note)
    ReqCtrl->>ReqSvc: SubmitCounterOfferAsync(requestId, factoryOrgId, 250, note)
    ReqSvc->>Db: Request.Status = CounterOffered, CurrentOffer = 250
    ReqSvc->>Db: RequestOffers.Add(RoundNumber = 2, Price = 250, OfferedBy = Factory)
    ReqSvc->>Db: StatusHistories.Add(From = PendingApproval, To = CounterOffered)
    ReqSvc->>NotifSvc: SendNotificationAsync(BrandUser, "Counter-Offer Received: 250 EGP")
    ReqSvc->>Db: SaveChangesAsync()
    ReqSvc-->>ReqCtrl: Success
    ReqCtrl-->>Browser: View Request Details (Status = CounterOffered)

    Note over Brand,ReqCtrl: Brand evaluates and sends Revised Offer (D-11)
    Brand->>Browser: Submit Revised Offer (230 EGP, "Can meet at 230 for volume of 100")
    Browser->>ReqCtrl: POST /ManufacturingRequests/ReviseOffer(requestId, 230, note)
    ReqCtrl->>ReqSvc: SubmitRevisedOfferAsync(requestId, brandOrgId, 230, note)
    ReqSvc->>Db: Request.Status = PendingApproval, CurrentOffer = 230
    ReqSvc->>Db: RequestOffers.Add(RoundNumber = 3, Price = 230, OfferedBy = Brand)
    ReqSvc->>Db: StatusHistories.Add(From = CounterOffered, To = PendingApproval)
    ReqSvc->>NotifSvc: SendNotificationAsync(FactoryUser, "Brand Revised Offer: 230 EGP")
    ReqSvc->>Db: SaveChangesAsync()
    ReqSvc-->>ReqCtrl: Success
    ReqCtrl-->>Browser: View Request Details (Status = PendingApproval)

    Note over Factory,ReqCtrl: Factory agrees to 230 EGP and Accepts
    Factory->>Browser: Click Accept Offer
    Browser->>ReqCtrl: POST /ManufacturingRequests/Accept(requestId)
    ReqCtrl->>ReqSvc: AcceptRequestAsync(requestId, factoryOrgId)
    ReqSvc->>Db: Request.Status = Accepted
    ReqSvc->>Db: StatusHistories.Add(From = PendingApproval, To = Accepted)
    ReqSvc->>NotifSvc: SendNotificationAsync(BrandUser, "Manufacturing Request Accepted!")
    ReqSvc->>Db: SaveChangesAsync()
    ReqSvc-->>ReqCtrl: Success
    ReqCtrl-->>Browser: View Request Details (Status = Accepted [Terminal])
```
