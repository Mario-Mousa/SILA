// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T4.1 | Owner: P4 (QA Lead) | Status: DONE
// What exists: Concurrency verification test suite (TC-CONC-01 - BR-01 & BR-03)
// What is left: // TODO(T4.4): Include QA test suite in final project documentation
// Depends on:  MaterialOrderService, ApplicationDbContext, LocalDB
// Used by:     SILA QA automated test verification
// How to test: dotnet test --filter FullyQualifiedName~ConcurrencyVerificationTests
// Notes:       Simulates two simultaneous orders of 15 units against 20 available (TC-CONC-01).
//              Verifies exactly one transaction commits, remaining stock = 5, and stock never drops below 0.
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Tests.Common;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Implementations;
using Xunit;

namespace SILA.Tests.Concurrency;

public class ConcurrencyVerificationTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private ApplicationDbContext _setupContext = null!;

    public ConcurrencyVerificationTests()
    {
        _connectionString = TestDatabaseHelper.CreateUniqueConnectionString();
    }

    public async Task InitializeAsync()
    {
        _setupContext = await TestDatabaseHelper.CreateDatabaseAsync(_connectionString);
    }

    public async Task DisposeAsync()
    {
        if (_setupContext != null)
        {
            await _setupContext.DisposeAsync();
        }
        await TestDatabaseHelper.DestroyDatabaseAsync(_connectionString);
    }

    [Fact]
    public async Task TC_CONC_01_TwoSimultaneousOrders_AgainstLimitedStock_ExactlyOneSucceedsAndStockNeverNegative()
    {
        // ─── ARRANGE ─────────────────────────────────────────────────────────────
        // Create Identity users first to satisfy FK_Organizations_AspNetUsers_OwnerUserId
        var userSupp = new ApplicationUser
        {
            Id = "supp-user-01",
            UserName = "supplier1@sila.com",
            Email = "supplier1@sila.com",
            FullName = "Supplier User",
            EmailConfirmed = true
        };

        var userBrand1 = new ApplicationUser
        {
            Id = "brand-user-01",
            UserName = "brand1@sila.com",
            Email = "brand1@sila.com",
            FullName = "Brand User 1",
            EmailConfirmed = true
        };

        var userBrand2 = new ApplicationUser
        {
            Id = "brand-user-02",
            UserName = "brand2@sila.com",
            Email = "brand2@sila.com",
            FullName = "Brand User 2",
            EmailConfirmed = true
        };

        _setupContext.Users.AddRange(userSupp, userBrand1, userBrand2);
        await _setupContext.SaveChangesAsync();

        // Create supplier and buyers
        var supplier = new Organization
        {
            Name = "Al-Amal Textiles",
            Type = OrganizationType.Supplier,
            City = "El-Mahalla",
            ContactPhone = "01011112222",
            OwnerUserId = "supp-user-01",
            VerificationStatus = VerificationStatus.Verified,
            CreatedAt = DateTime.UtcNow
        };

        var buyer1 = new Organization
        {
            Name = "Cairo Modern Brands",
            Type = OrganizationType.Brand,
            City = "Cairo",
            ContactPhone = "01033334444",
            OwnerUserId = "brand-user-01",
            VerificationStatus = VerificationStatus.Verified,
            CreatedAt = DateTime.UtcNow
        };

        var buyer2 = new Organization
        {
            Name = "Alexandria Apparel Co",
            Type = OrganizationType.Brand,
            City = "Alexandria",
            ContactPhone = "01055556666",
            OwnerUserId = "brand-user-02",
            VerificationStatus = VerificationStatus.Verified,
            CreatedAt = DateTime.UtcNow
        };

        _setupContext.Organizations.AddRange(supplier, buyer1, buyer2);
        await _setupContext.SaveChangesAsync();

        // BR-01 & BR-03: Listing with initial stock of 20 units
        var listing = new MaterialListing
        {
            SupplierOrganizationId = supplier.Id,
            Name = "100% Egyptian Giza Cotton",
            Category = "Fabrics",
            Unit = "meter",
            UnitPrice = 250.00m,
            AvailableQuantity = 20, // Only 20 meters in stock
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _setupContext.MaterialListings.Add(listing);
        await _setupContext.SaveChangesAsync();

        // Create Order A for 15 units (PendingReview)
        var orderA = new MaterialOrder
        {
            BuyerOrganizationId = buyer1.Id,
            SupplierOrganizationId = supplier.Id,
            Status = MaterialOrderStatus.PendingReview,
            TotalAmount = 15 * 250.00m,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<MaterialOrderLine>
            {
                new()
                {
                    MaterialListingId = listing.Id,
                    MaterialNameSnapshot = listing.Name,
                    UnitSnapshot = listing.Unit,
                    UnitPriceSnapshot = listing.UnitPrice,
                    Quantity = 15,
                    LineTotal = 15 * 250.00m,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        // Create Order B for 15 units (PendingReview)
        var orderB = new MaterialOrder
        {
            BuyerOrganizationId = buyer2.Id,
            SupplierOrganizationId = supplier.Id,
            Status = MaterialOrderStatus.PendingReview,
            TotalAmount = 15 * 250.00m,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<MaterialOrderLine>
            {
                new()
                {
                    MaterialListingId = listing.Id,
                    MaterialNameSnapshot = listing.Name,
                    UnitSnapshot = listing.Unit,
                    UnitPriceSnapshot = listing.UnitPrice,
                    Quantity = 15,
                    LineTotal = 15 * 250.00m,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _setupContext.MaterialOrders.AddRange(orderA, orderB);
        await _setupContext.SaveChangesAsync();

        var orderAId = orderA.Id;
        var orderBId = orderB.Id;
        var listingId = listing.Id;
        var supplierOrgId = supplier.Id;

        // ─── ACT: Simulate simultaneous confirmations using isolated DbContext instances ─
        var optionsA = TestDatabaseHelper.CreateOptions(_connectionString);
        var optionsB = TestDatabaseHelper.CreateOptions(_connectionString);

        using var contextA = new ApplicationDbContext(optionsA);
        using var contextB = new ApplicationDbContext(optionsB);

        var serviceA = new MaterialOrderService(
            contextA,
            new StatusHistoryService(contextA),
            new NotificationService(contextA));

        var serviceB = new MaterialOrderService(
            contextB,
            new StatusHistoryService(contextB),
            new NotificationService(contextB));

        // Use Task.WhenAll to trigger parallel execution
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var taskA = Task.Run(async () =>
        {
            await barrier.Task;
            return await serviceA.ConfirmOrderAsync(orderAId, supplierOrgId, "supp-user-01");
        });

        var taskB = Task.Run(async () =>
        {
            await barrier.Task;
            return await serviceB.ConfirmOrderAsync(orderBId, supplierOrgId, "supp-user-01");
        });

        // Release both tasks simultaneously
        barrier.SetResult();

        var results = await Task.WhenAll(taskA, taskB);
        bool resultA = results[0];
        bool resultB = results[1];

        // ─── ASSERT ─────────────────────────────────────────────────────────────
        // Exactly ONE order confirmation must succeed (BR-03)
        int successCount = (resultA ? 1 : 0) + (resultB ? 1 : 0);
        Assert.True(successCount == 1, $"Expected exactly 1 confirmation to succeed, but {successCount} succeeded. (A: {resultA}, B: {resultB})");

        // Verify state in a completely fresh DbContext
        var verifyOptions = TestDatabaseHelper.CreateOptions(_connectionString);
        await using var verifyContext = new ApplicationDbContext(verifyOptions);

        var refreshedListing = await verifyContext.MaterialListings.AsNoTracking().SingleAsync(l => l.Id == listingId);
        var refreshedOrderA = await verifyContext.MaterialOrders.AsNoTracking().SingleAsync(o => o.Id == orderAId);
        var refreshedOrderB = await verifyContext.MaterialOrders.AsNoTracking().SingleAsync(o => o.Id == orderBId);

        // Remaining stock must be exactly 5 (20 - 15) (BR-01, BR-03)
        Assert.Equal(5, refreshedListing.AvailableQuantity);
        Assert.True(refreshedListing.AvailableQuantity >= 0, "Stock must NEVER be negative (BR-01).");

        // The successful order must be Confirmed, while the failing one remains PendingReview
        if (resultA)
        {
            Assert.Equal(MaterialOrderStatus.Confirmed, refreshedOrderA.Status);
            Assert.Equal(MaterialOrderStatus.PendingReview, refreshedOrderB.Status);
        }
        else
        {
            Assert.Equal(MaterialOrderStatus.PendingReview, refreshedOrderA.Status);
            Assert.Equal(MaterialOrderStatus.Confirmed, refreshedOrderB.Status);
        }

        // Verify stock movement was recorded for the winning order only (BR-09)
        var stockMovements = await verifyContext.StockMovements
            .AsNoTracking()
            .Where(m => m.MaterialListingId == listingId)
            .ToListAsync();

        Assert.Single(stockMovements);
        Assert.Equal(-15, stockMovements[0].QuantityChange);

        var winningOrderId = resultA ? orderAId : orderBId;
        Assert.Equal(winningOrderId, stockMovements[0].MaterialOrderId);

        // Verify status history was recorded for the winning order only (BR-08)
        var historyRows = await verifyContext.StatusHistories
            .AsNoTracking()
            .Where(h => (h.RecordType == StatusRecordType.Order && h.RecordId == winningOrderId) && h.ToStatus == nameof(MaterialOrderStatus.Confirmed))
            .ToListAsync();

        Assert.Single(historyRows);
    }
}
