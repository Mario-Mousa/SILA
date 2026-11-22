// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T4.1 | Owner: P4 (QA Lead) | Status: DONE
// What exists: Business Rule Validation Tests (BR-01, BR-02, BR-04, BR-05, BR-06, BR-11, Part 4.4)
// What is left: // TODO(T4.4): Include QA test suite in final project documentation
// Depends on:  MaterialOrderService, ManufacturingRequestService, ApplicationDbContext
// Used by:     SILA QA automated verification suite
// How to test: dotnet test --filter FullyQualifiedName~BusinessRuleValidationTests
// Notes:       Enforces single-supplier partitioning, snapshot immutability, hard-delete prevention, and strict state machines
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Tests.Common;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Implementations;
using Xunit;

namespace SILA.Tests.BusinessRules;

public class BusinessRuleValidationTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private ApplicationDbContext _context = null!;

    private Organization _buyerOrg = null!;
    private Organization _factoryOrg = null!;
    private Organization _supplierOrg1 = null!;
    private Organization _supplierOrg2 = null!;

    private ApplicationUser _buyerUser = null!;
    private ApplicationUser _factoryUser = null!;
    private ApplicationUser _supplierUser1 = null!;
    private ApplicationUser _supplierUser2 = null!;

    public BusinessRuleValidationTests()
    {
        _connectionString = TestDatabaseHelper.CreateUniqueConnectionString();
    }

    public async Task InitializeAsync()
    {
        _context = await TestDatabaseHelper.CreateDatabaseAsync(_connectionString);

        _buyerUser = new ApplicationUser { Id = "br-buyer-01", UserName = "brbuyer@sila.com", Email = "brbuyer@sila.com", FullName = "Buyer Brand", EmailConfirmed = true };
        _factoryUser = new ApplicationUser { Id = "br-factory-01", UserName = "brfactory@sila.com", Email = "brfactory@sila.com", FullName = "Factory Owner", EmailConfirmed = true };
        _supplierUser1 = new ApplicationUser { Id = "br-supp-01", UserName = "brsupp1@sila.com", Email = "brsupp1@sila.com", FullName = "Supplier One", EmailConfirmed = true };
        _supplierUser2 = new ApplicationUser { Id = "br-supp-02", UserName = "brsupp2@sila.com", Email = "brsupp2@sila.com", FullName = "Supplier Two", EmailConfirmed = true };

        _context.Users.AddRange(_buyerUser, _factoryUser, _supplierUser1, _supplierUser2);
        await _context.SaveChangesAsync();

        _buyerOrg = new Organization { Name = "Brand Buyer Co", Type = OrganizationType.Brand, City = "Cairo", ContactPhone = "01000000001", OwnerUserId = _buyerUser.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _factoryOrg = new Organization { Name = "Elite Garments Factory", Type = OrganizationType.Factory, City = "Alexandria", ContactPhone = "01000000002", OwnerUserId = _factoryUser.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _supplierOrg1 = new Organization { Name = "Delta Yarns", Type = OrganizationType.Supplier, City = "El-Mahalla", ContactPhone = "01000000003", OwnerUserId = _supplierUser1.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _supplierOrg2 = new Organization { Name = "Nile Silks", Type = OrganizationType.Supplier, City = "Cairo", ContactPhone = "01000000004", OwnerUserId = _supplierUser2.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };

        _context.Organizations.AddRange(_buyerOrg, _factoryOrg, _supplierOrg1, _supplierOrg2);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
        await TestDatabaseHelper.DestroyDatabaseAsync(_connectionString);
    }

    [Fact]
    public async Task TC_RULE_01_BR_01_NonPositiveQuantities_RejectedWithArgumentException()
    {
        var orderService = CreateOrderService();

        var listing = new MaterialListing
        {
            SupplierOrganizationId = _supplierOrg1.Id,
            Name = "Denim Fabric 12oz",
            Category = "Fabrics",
            Unit = "meter",
            UnitPrice = 200m,
            AvailableQuantity = 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MaterialListings.Add(listing);
        await _context.SaveChangesAsync();

        // Quantity 0
        var exZero = await Assert.ThrowsAsync<ArgumentException>(() =>
            orderService.CreateOrderAsync(_buyerOrg.Id, _supplierOrg1.Id, new[] { (listing.Id, 0) }));
        Assert.Contains("positive", exZero.Message, StringComparison.OrdinalIgnoreCase);

        // Quantity -5
        var exNegative = await Assert.ThrowsAsync<ArgumentException>(() =>
            orderService.CreateOrderAsync(_buyerOrg.Id, _supplierOrg1.Id, new[] { (listing.Id, -5) }));
        Assert.Contains("positive", exNegative.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_RULE_02_BR_02_MultipleSuppliersInCart_StrictlyBlocked()
    {
        var orderService = CreateOrderService();

        // Listing from Supplier 1
        var listing1 = new MaterialListing
        {
            SupplierOrganizationId = _supplierOrg1.Id,
            Name = "Carded Cotton Yarn",
            Category = "Yarns",
            Unit = "kg",
            UnitPrice = 120m,
            AvailableQuantity = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Listing from Supplier 2
        var listing2 = new MaterialListing
        {
            SupplierOrganizationId = _supplierOrg2.Id,
            Name = "Mulberry Raw Silk",
            Category = "Fabrics",
            Unit = "meter",
            UnitPrice = 450m,
            AvailableQuantity = 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.MaterialListings.AddRange(listing1, listing2);
        await _context.SaveChangesAsync();

        // Attempt checkout specifying Supplier 1, but including listing from Supplier 2
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orderService.CreateOrderAsync(
                _buyerOrg.Id,
                _supplierOrg1.Id,
                new[] { (listing1.Id, 5), (listing2.Id, 2) }));

        // BR-02 error message verification
        Assert.Contains("one supplier only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_RULE_03_BR_04_SnapshotImmutability_ListingUpdateDoesNotMutateHistoricalOrderLines()
    {
        var orderService = CreateOrderService();

        var listing = new MaterialListing
        {
            SupplierOrganizationId = _supplierOrg1.Id,
            Name = "Natural Linen Fabric",
            Category = "Fabrics",
            Unit = "meter",
            UnitPrice = 150.00m,
            AvailableQuantity = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MaterialListings.Add(listing);
        await _context.SaveChangesAsync();

        // 1. Create order capturing snapshot: 10 meters @ 150.00 EGP = 1500.00 EGP
        var order = await orderService.CreateOrderAsync(
            _buyerOrg.Id,
            _supplierOrg1.Id,
            new[] { (listing.Id, 10) });

        Assert.Equal(1500.00m, order.TotalAmount);
        var initialLine = order.Lines.First();
        Assert.Equal("Natural Linen Fabric", initialLine.MaterialNameSnapshot);
        Assert.Equal(150.00m, initialLine.UnitPriceSnapshot);
        Assert.Equal(1500.00m, initialLine.LineTotal);

        // 2. Supplier mutates listing in catalog: price increases to 250.00 EGP, name changed, deactivated (BR-05)
        listing.Name = "Natural Linen Fabric Premium";
        listing.UnitPrice = 250.00m;
        listing.IsActive = false;
        await _context.SaveChangesAsync();

        // 3. Re-read historical order from database via fresh query
        var historicalOrder = await orderService.GetByIdAsync(order.Id);
        Assert.NotNull(historicalOrder);
        Assert.Equal(1500.00m, historicalOrder.TotalAmount);

        var historicalLine = historicalOrder.Lines.First();
        Assert.Equal("Natural Linen Fabric", historicalLine.MaterialNameSnapshot);
        Assert.Equal("meter", historicalLine.UnitSnapshot);
        Assert.Equal(150.00m, historicalLine.UnitPriceSnapshot);
        Assert.Equal(1500.00m, historicalLine.LineTotal);
    }

    [Fact]
    public async Task TC_RULE_04_BR_05_HardDeletionOfReferencedListing_BlockedByDeleteBehaviorRestrict()
    {
        var orderService = CreateOrderService();

        var listing = new MaterialListing
        {
            SupplierOrganizationId = _supplierOrg1.Id,
            Name = "Organic Wool Spun",
            Category = "Yarns",
            Unit = "kg",
            UnitPrice = 300m,
            AvailableQuantity = 40,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MaterialListings.Add(listing);
        await _context.SaveChangesAsync();

        // Order references the listing
        await orderService.CreateOrderAsync(
            _buyerOrg.Id,
            _supplierOrg1.Id,
            new[] { (listing.Id, 5) });

        // Supplier attempts hard deletion of the listing from a fresh DbContext
        await using var deleteContext = new ApplicationDbContext(TestDatabaseHelper.CreateOptions(_connectionString));
        var listingToDelete = await deleteContext.MaterialListings.SingleAsync(l => l.Id == listing.Id);
        deleteContext.MaterialListings.Remove(listingToDelete);

        // EF Core / SQL Server must reject with DbUpdateException due to DeleteBehavior.Restrict
        await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
    }

    [Fact]
    public async Task TC_RULE_06_BR_06_OutOfStockListing_CannotBeOrdered()
    {
        var orderService = CreateOrderService();

        var listing = new MaterialListing
        {
            SupplierOrganizationId = _supplierOrg1.Id,
            Name = "Sold Out Viscose Rayon",
            Category = "Fabrics",
            Unit = "meter",
            UnitPrice = 90m,
            AvailableQuantity = 0, // Out of stock
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MaterialListings.Add(listing);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orderService.CreateOrderAsync(_buyerOrg.Id, _supplierOrg1.Id, new[] { (listing.Id, 1) }));

        Assert.Contains("out of stock", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_TRANS_01_BR_11_MaterialOrder_StrictStateTransitionsAndRejections()
    {
        var orderService = CreateOrderService();

        var listing = new MaterialListing
        {
            SupplierOrganizationId = _supplierOrg1.Id,
            Name = "Twill Weave Cotton",
            Category = "Fabrics",
            Unit = "meter",
            UnitPrice = 110m,
            AvailableQuantity = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MaterialListings.Add(listing);
        await _context.SaveChangesAsync();

        // Create Order 1 (PendingReview)
        var order1 = await orderService.CreateOrderAsync(
            _buyerOrg.Id,
            _supplierOrg1.Id,
            new[] { (listing.Id, 10) });

        Assert.Equal(MaterialOrderStatus.PendingReview, order1.Status);

        // Illegal: Confirmed -> Cancelled (Confirmed orders CANNOT be cancelled)
        // First confirm it legally
        bool confirmed = await orderService.ConfirmOrderAsync(order1.Id, _supplierOrg1.Id, _supplierUser1.Id);
        Assert.True(confirmed);

        // Now attempt cancellation: must fail
        bool cancelAfterConfirm = await orderService.CancelOrderAsync(order1.Id, _buyerOrg.Id, _buyerUser.Id);
        Assert.False(cancelAfterConfirm, "Confirmed order must never be cancelled (Part 4.4).");

        // Illegal: Confirmed -> Delivered directly (cannot skip Dispatched)
        bool deliverSkippingDispatch = await orderService.DeliverOrderAsync(order1.Id, _supplierOrg1.Id, _supplierUser1.Id);
        Assert.False(deliverSkippingDispatch, "Cannot transition directly from Confirmed to Delivered skipping Dispatched.");

        // Legal: Confirmed -> Dispatched
        bool dispatched = await orderService.DispatchOrderAsync(order1.Id, _supplierOrg1.Id, _supplierUser1.Id);
        Assert.True(dispatched);

        // Legal: Dispatched -> Delivered
        bool delivered = await orderService.DeliverOrderAsync(order1.Id, _supplierOrg1.Id, _supplierUser1.Id);
        Assert.True(delivered);

        // Post-Delivered: Delivered is final. Attempting to Cancel or Dispatch must fail
        bool cancelAfterDelivery = await orderService.CancelOrderAsync(order1.Id, _buyerOrg.Id, _buyerUser.Id);
        Assert.False(cancelAfterDelivery, "Delivered is a final state; cannot be cancelled.");

        // Create Order 2 for rejection test
        var order2 = await orderService.CreateOrderAsync(
            _buyerOrg.Id,
            _supplierOrg1.Id,
            new[] { (listing.Id, 5) });

        // Legal: PendingReview -> Rejected with reason
        bool rejected = await orderService.RejectOrderAsync(order2.Id, _supplierOrg1.Id, _supplierUser1.Id, "Production line fully booked.");
        Assert.True(rejected);

        // Illegal: Rejected -> Confirmed (Rejected is final)
        bool confirmAfterReject = await orderService.ConfirmOrderAsync(order2.Id, _supplierOrg1.Id, _supplierUser1.Id);
        Assert.False(confirmAfterReject, "Rejected is a final state; cannot be confirmed.");
    }

    [Fact]
    public async Task TC_TRANS_02_ManufacturingRequest_StrictNegotiationLifecycleAndImmutability()
    {
        var mfgService = new ManufacturingRequestService(
            _context,
            new TestStatusHistoryService(_context),
            new NotificationService(_context));

        // 1. Brand creates request: Round 1 (Initial offer 100 EGP)
        var request = await mfgService.CreateRequestAsync(
            _buyerOrg.Id,
            _factoryOrg.Id,
            "Oxford Shirts",
            "Button-down collar, regular fit",
            "Oxford Weave 100% Cotton",
            300,
            100.00m,
            null,
            "Target delivery Q1",
            _buyerUser.Id);

        Assert.Equal(ManufacturingRequestStatus.PendingApproval, request.Status);
        Assert.Equal(100.00m, request.CurrentOfferPrice);

        // 2. Factory counter-offers: Round 2 (Counter offer 125 EGP)
        bool countered = await mfgService.CounterOfferAsync(
            request.Id,
            _factoryOrg.Id,
            _factoryUser.Id,
            125.00m,
            "Fabric and wash costs require 125 EGP.");

        Assert.True(countered);
        var reqAfterCounter = await mfgService.GetByIdAsync(request.Id);
        Assert.NotNull(reqAfterCounter);
        Assert.Equal(ManufacturingRequestStatus.CounterOffered, reqAfterCounter.Status);
        Assert.Equal(125.00m, reqAfterCounter.CurrentOfferPrice);

        // 3. Brand revises offer: Round 3 (Revised offer 115 EGP)
        bool revised = await mfgService.ReviseOfferAsync(
            request.Id,
            _buyerOrg.Id,
            _buyerUser.Id,
            115.00m,
            "We can meet at 115 EGP for bulk order.");

        Assert.True(revised);
        var reqAfterRevise = await mfgService.GetByIdAsync(request.Id);
        Assert.NotNull(reqAfterRevise);
        Assert.Equal(ManufacturingRequestStatus.PendingApproval, reqAfterRevise.Status);
        Assert.Equal(115.00m, reqAfterRevise.CurrentOfferPrice);

        // 4. Factory accepts offer: Final state Accepted
        bool accepted = await mfgService.AcceptRequestAsync(
            request.Id,
            _factoryOrg.Id,
            _factoryUser.Id,
            "Offer of 115 EGP accepted.");

        Assert.True(accepted);
        var reqFinal = await mfgService.GetByIdAsync(request.Id);
        Assert.NotNull(reqFinal);
        Assert.Equal(ManufacturingRequestStatus.Accepted, reqFinal.Status);

        // Verify offer timeline has 3 distinct negotiation rounds
        var offers = (await mfgService.GetOfferHistoryAsync(request.Id)).ToList();
        Assert.Equal(3, offers.Count);

        // 5. Illegal transition after Accepted: cannot Cancel
        bool cancelAfterAccept = await mfgService.CancelRequestAsync(request.Id, _buyerOrg.Id, _buyerUser.Id);
        Assert.False(cancelAfterAccept, "Accepted request is final and cannot be cancelled.");
    }

    private MaterialOrderService CreateOrderService()
    {
        return new MaterialOrderService(
            _context,
            new StatusHistoryService(_context),
            new NotificationService(_context));
    }
}
