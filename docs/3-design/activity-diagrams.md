<!--
═══════════ HANDOFF ═══════════════════════════════════
Task:        T1.4 | Owner: P4 (QA & Business Analyst) | Status: DONE
What exists: Three comprehensive Mermaid Activity Diagrams covering (1) Registration & Verification, (2) Material Ordering with Atomic Concurrency Control, and (3) Manufacturing Request with Multi-Round Negotiation.
What is left: // TODO(T4.1): Formulate integration tests mapping directly to activity decision branches.
Depends on:  T1.1 (Data Model & Schema), T1.2 (Service Contracts)
Used by:     All developers (P1, P2, P3, P4), QA Testers
How to test: Verify alignment with D-01..D-12, BR-03 (atomic confirmation), BR-05 (snapshot preservation), and D-11 (unbounded counter-offers).
Notes:       All activity paths detail database transaction boundaries and rollback scenarios.
═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Activity Diagrams

> **Document:** Business Process & Operational Activity Models  
> **Task:** T1.4 | **Owner:** P4 (QA & Business Analyst)  
> **Target System:** SILA B2B Apparel Platform  
> **Last Updated:** 29 October 2026  

---

## 1. Activity Diagram 1: Account Registration & Verification Workflow

Models the complete user onboarding process from role declaration through email confirmation, document submission, and administrative review:

```mermaid
flowchart TD
    Start([User Starts Registration]) --> SelectRole[Select Organization Type: Brand, Factory, or Supplier]
    SelectRole --> FillForm[Fill Profile: Name, Email, Password, Phone, Org Name]
    FillForm --> SubmitReg[Submit Registration Form]

    SubmitReg --> CheckValid{Server Validation & Unique Email?}
    CheckValid -- No --> ShowRegError[Display Validation Errors to User]
    ShowRegError --> FillForm

    CheckValid -- Yes --> CreateUser[Create ApplicationUser & Organization]
    CreateUser --> SetUnverified[Set VerificationStatus = Unverified]
    SetUnverified --> SendEmail[Generate Token & Send Confirmation Email via SMTP]
    SendEmail --> EmailSentPage[Display: 'Check Your Email To Confirm']

    EmailSentPage --> ClickLink[User Clicks Confirmation Link in Email]
    ClickLink --> ValidateToken{Token Valid & Not Expired?}
    ValidateToken -- No --> TokenError[Display Token Error / Resend Option]
    ValidateToken -- Yes --> ConfirmEmail[Set EmailConfirmed = True]
    ConfirmEmail --> LoginScreen[User Logs In]

    LoginScreen --> CheckRole[Determine Organization Type Requirements]
    
    CheckRole --> UploadDocs[User Uploads Role-Specific Verification Proofs]
    note1[Brand: Instagram/Portfolio link<br>Factory: Industrial License<br>Supplier: Commercial Registration] -.-> UploadDocs
    
    UploadDocs --> ValidateFiles{Valid MIME Type & Size <= 5MB?}
    ValidateFiles -- No --> FileError[Reject Upload & Prompt Valid Format]
    FileError --> UploadDocs

    ValidateFiles -- Yes --> SaveDocs[Store Documents Outside Git]
    SaveDocs --> UpdateStatus[Set VerificationStatus = PendingReview]
    UpdateStatus --> AdminQueue[Submission Listed in Admin Portal]

    AdminQueue --> AdminReview[Admin Inspects Proofs & Organization Details]
    AdminReview --> AdminDecision{Admin Decision?}
    
    AdminDecision -- Reject --> AdminNoteReject[Enter Mandatory Rejection Reason]
    AdminNoteReject --> SetRejected[Set VerificationStatus = Rejected]
    SetRejected --> NotifyUserReject[Send In-App Notification with Reason]
    NotifyUserReject --> EndReject([Account Restricted to Profile View])

    AdminDecision -- Approve --> AdminNoteApprove[Optional Approval Note]
    AdminNoteApprove --> SetVerified[Set VerificationStatus = Verified]
    SetVerified --> NotifyUserApprove[Send In-App Notification: 'Account Verified']
    NotifyUserApprove --> EndApprove([Full System Capabilities Unlocked])
```

---

## 2. Activity Diagram 2: Material Order & Atomic Stock Execution

Models order drafting, single-supplier partition enforcement, snapshot recording, and transactional concurrency handling during confirmation:

```mermaid
flowchart TD
    StartOrder([Buyer Starts Material Order]) --> BrowseCatalog[Browse Catalog with Search & Category Filters]
    BrowseCatalog --> SelectListings[Select Material Listings & Enter Quantities]
    
    SelectListings --> CheckSingleSupplier{All Listings From Same Supplier? BR-02}
    CheckSingleSupplier -- No --> AlertMixed[Prompt Error: Multiple Suppliers in Cart Prohibited]
    AlertMixed --> SelectListings

    CheckSingleSupplier -- Yes --> ValidateQty{Quantity > 0 & <= Available?}
    ValidateQty -- No --> AlertQty[Prompt Error: Invalid Quantity or Exceeds Stock]
    AlertQty --> SelectListings

    ValidateQty -- Yes --> ReviewCart[Review Order Summary: Quantities, Unit Prices, Total]
    ReviewCart --> SubmitOrder[Buyer Confirms & Submits Order]

    SubmitOrder --> FreezeSnapshots[Freeze Snapshots: Name, Unit, UnitPrice in Line Items]
    FreezeSnapshots --> SavePendingOrder[Create MaterialOrder: Status = PendingReview]
    SavePendingOrder --> NotifySupplier[Send In-App Notification to Supplier]
    NotifySupplier --> SupplierInspects[Supplier Reviews Pending Order]

    SupplierInspects --> SupplierChoice{Supplier Action?}

    SupplierChoice -- Cancel by Buyer --> CheckPending{Still in PendingReview? D-06}
    CheckPending -- Yes --> BuyerCancel[Set Status = Cancelled]
    BuyerCancel --> EndCancel([Order Cancelled - Zero Stock Impact])
    CheckPending -- No --> CancelBlocked[Block Cancellation: Already Processed]

    SupplierChoice -- Reject --> EnterRejectReason[Supplier Enters Mandatory Rejection Reason]
    EnterRejectReason --> SetRejected[Set Status = Rejected]
    SetRejected --> NotifyBuyerReject[Notify Buyer of Rejection with Reason]
    NotifyBuyerReject --> EndReject([Order Rejected - Zero Stock Impact])

    SupplierChoice -- Confirm --> BeginTx[Begin Atomic Database Transaction: BR-03]
    BeginTx --> LockCheckStock{Verify AvailableQuantity >= LineQuantity For All Lines?}
    
    LockCheckStock -- Insufficient Stock --> RollbackTx[Rollback Transaction]
    RollbackTx --> NotifyShortage[Alert Supplier: Stock Depleted by Concurrent Order]
    NotifyShortage --> EndShortage([Order Remains PendingReview])

    LockCheckStock -- Sufficient Stock --> DeductStock[Deduct Quantities from MaterialListings]
    DeductStock --> InsertStockMovement[Insert StockMovement Audit Records: BR-09]
    InsertStockMovement --> UpdateOrderConfirmed[Update MaterialOrder Status = Confirmed]
    UpdateOrderConfirmed --> AppendHistory[Append StatusHistory Audit Entry: BR-08]
    AppendHistory --> CommitTx[Commit Atomic Transaction]
    
    CommitTx --> NotifyBuyerConfirmed[Send In-App Notification: 'Order Confirmed']
    NotifyBuyerConfirmed --> DispatchOrder[Supplier Dispatches Material: Status = Dispatched]
    DispatchOrder --> DeliverOrder[Supplier Confirms Delivery: Status = Delivered]
    DeliverOrder --> EndDelivered([Order Lifecycle Complete])
```

---

## 3. Activity Diagram 3: Manufacturing Request & Negotiation Rounds

Models the complete multi-round bargaining lifecycle between a Brand Owner and a targeted Factory Owner:

```mermaid
flowchart TD
    StartReq([Brand Owner Starts Request]) --> BrowseFactories[Browse Verified Factories Directory]
    BrowseFactories --> SelectFactory[Select Target Factory: D-01]
    SelectFactory --> FillReqForm[Enter Product Specs, Fabric, Quantity, Offer Price]
    FillReqForm --> SubmitReq[Submit Manufacturing Request]

    SubmitReq --> CreateReq[Create ManufacturingRequest: Status = PendingApproval]
    CreateReq --> CreateRound1[Create RequestOffer: Round 1, OfferedBy = Brand]
    CreateRound1 --> NotifyFactory[Send In-App Notification to Factory Owner]

    NotifyFactory --> FactoryReviews[Factory Owner Reviews Specifications & Pricing]
    FactoryReviews --> FactoryAction{Factory Decision?}

    FactoryAction -- Cancel by Brand --> CheckReqPending{Request in PendingApproval or CounterOffered?}
    CheckReqPending -- Yes --> SetReqCancelled[Set Status = Cancelled]
    SetReqCancelled --> EndCancel([Request Cancelled])

    FactoryAction -- Reject --> FactoryReject[Factory Rejects Request with Optional Note]
    FactoryReject --> SetReqRejected[Set Status = Rejected]
    SetReqRejected --> NotifyBrandReject[Notify Brand Owner: Request Rejected]
    NotifyBrandReject --> EndReject([Negotiation Terminated])

    FactoryAction -- Accept --> FactoryAccept[Factory Accepts Proposed Price]
    FactoryAccept --> SetReqAccepted[Set Status = Accepted]
    SetReqAccepted --> NotifyBrandAccept[Notify Brand Owner: Request Accepted]
    NotifyBrandAccept --> EndAccepted([Contract Finalized - Ready for Offline Production])

    FactoryAction -- Counter-Offer --> EnterCounterPrice[Factory Enters Counter Unit Price & Reason]
    EnterCounterPrice --> UpdateCounterOffered[Set Status = CounterOffered]
    UpdateCounterOffered --> RecordRoundCounter[Create RequestOffer: Round N, OfferedBy = Factory]
    RecordRoundCounter --> NotifyBrandCounter[Send In-App Notification to Brand Owner]

    NotifyBrandCounter --> BrandReviewsCounter[Brand Owner Evaluates Counter-Offer]
    BrandReviewsCounter --> BrandChoice{Brand Decision?}

    BrandChoice -- Accept Counter --> BrandAccepts[Brand Accepts Factory Price]
    BrandAccepts --> SetReqAccepted

    BrandChoice -- Reject Counter --> BrandRejects[Brand Rejects Counter-Offer]
    BrandRejects --> SetReqRejected

    BrandChoice -- Revise Offer --> EnterRevisedPrice[Brand Enters Revised Unit Price & Note: D-11]
    EnterRevisedPrice --> UpdatePendingApproval[Set Status = PendingApproval]
    UpdatePendingApproval --> RecordRoundRevised[Create RequestOffer: Round N+1, OfferedBy = Brand]
    RecordRoundRevised --> NotifyFactory
```
