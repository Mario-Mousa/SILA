// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T4.1 | Owner: P4 (QA Lead) | Status: DONE
// What exists: Record-Level Authorization Negative Tests (TC-AUTHZ-01 to TC-AUTHZ-10 - BR-07)
// What is left: // TODO(T4.4): Include QA test suite in final project documentation
// Depends on:  IOwnershipService, OrdersController, ManufacturingRequestsController, ApplicationDbContext
// Used by:     SILA QA automated verification for zero data leakage (BR-07)
// How to test: dotnet test --filter FullyQualifiedName~AuthorizationNegativeTests
// Notes:       Tests both domain service level (IOwnershipService) and HTTP controller level (ForbidResult / 403)
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SILA.Tests.Common;
using SILA.Web.Controllers;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.ManufacturingRequests;
using SILA.Web.Services.Implementations;
using SILA.Web.Services.Interfaces;
using Xunit;

namespace SILA.Tests.Authorization;

public class AuthorizationNegativeTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private ApplicationDbContext _context = null!;

    private ApplicationUser _brand1User = null!;
    private ApplicationUser _brand2User = null!;
    private ApplicationUser _factory1User = null!;
    private ApplicationUser _factory2User = null!;
    private ApplicationUser _supplier1User = null!;
    private ApplicationUser _supplier2User = null!;
    private ApplicationUser _unverifiedBrandUser = null!;

    private Organization _brand1Org = null!;
    private Organization _brand2Org = null!;
    private Organization _factory1Org = null!;
    private Organization _factory2Org = null!;
    private Organization _supplier1Org = null!;
    private Organization _supplier2Org = null!;
    private Organization _unverifiedBrandOrg = null!;

    private MaterialListing _supplier1Listing = null!;
    private MaterialOrder _materialOrder1 = null!;
    private ManufacturingRequest _mfgRequest1 = null!;

    public AuthorizationNegativeTests()
    {
        _connectionString = TestDatabaseHelper.CreateUniqueConnectionString();
    }

    public async Task InitializeAsync()
    {
        _context = await TestDatabaseHelper.CreateDatabaseAsync(_connectionString);

        // 1. Seed Identity Users
        _brand1User = new ApplicationUser { Id = "brand-user-1", UserName = "brand1@sila.com", Email = "brand1@sila.com", FullName = "Brand One", EmailConfirmed = true };
        _brand2User = new ApplicationUser { Id = "brand-user-2", UserName = "brand2@sila.com", Email = "brand2@sila.com", FullName = "Brand Two", EmailConfirmed = true };
        _factory1User = new ApplicationUser { Id = "factory-user-1", UserName = "factory1@sila.com", Email = "factory1@sila.com", FullName = "Factory One", EmailConfirmed = true };
        _factory2User = new ApplicationUser { Id = "factory-user-2", UserName = "factory2@sila.com", Email = "factory2@sila.com", FullName = "Factory Two", EmailConfirmed = true };
        _supplier1User = new ApplicationUser { Id = "supplier-user-1", UserName = "supplier1@sila.com", Email = "supplier1@sila.com", FullName = "Supplier One", EmailConfirmed = true };
        _supplier2User = new ApplicationUser { Id = "supplier-user-2", UserName = "supplier2@sila.com", Email = "supplier2@sila.com", FullName = "Supplier Two", EmailConfirmed = true };
        _unverifiedBrandUser = new ApplicationUser { Id = "unver-brand-user", UserName = "unverified@sila.com", Email = "unverified@sila.com", FullName = "Unverified Brand", EmailConfirmed = true };

        _context.Users.AddRange(_brand1User, _brand2User, _factory1User, _factory2User, _supplier1User, _supplier2User, _unverifiedBrandUser);
        await _context.SaveChangesAsync();

        // 2. Seed Organizations
        _brand1Org = new Organization { Name = "Brand Alpha", Type = OrganizationType.Brand, City = "Cairo", ContactPhone = "01011111111", OwnerUserId = _brand1User.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _brand2Org = new Organization { Name = "Brand Beta", Type = OrganizationType.Brand, City = "Giza", ContactPhone = "01022222222", OwnerUserId = _brand2User.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _factory1Org = new Organization { Name = "Factory Alpha", Type = OrganizationType.Factory, City = "Alexandria", ContactPhone = "01033333333", OwnerUserId = _factory1User.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _factory2Org = new Organization { Name = "Factory Beta", Type = OrganizationType.Factory, City = "El-Mahalla", ContactPhone = "01044444444", OwnerUserId = _factory2User.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _supplier1Org = new Organization { Name = "Supplier Alpha", Type = OrganizationType.Supplier, City = "Port Said", ContactPhone = "01055555555", OwnerUserId = _supplier1User.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _supplier2Org = new Organization { Name = "Supplier Beta", Type = OrganizationType.Supplier, City = "Cairo", ContactPhone = "01066666666", OwnerUserId = _supplier2User.Id, VerificationStatus = VerificationStatus.Verified, CreatedAt = DateTime.UtcNow };
        _unverifiedBrandOrg = new Organization { Name = "Unverified Brand", Type = OrganizationType.Brand, City = "Mansoura", ContactPhone = "01077777777", OwnerUserId = _unverifiedBrandUser.Id, VerificationStatus = VerificationStatus.Unverified, CreatedAt = DateTime.UtcNow };

        _context.Organizations.AddRange(_brand1Org, _brand2Org, _factory1Org, _factory2Org, _supplier1Org, _supplier2Org, _unverifiedBrandOrg);
        await _context.SaveChangesAsync();

        // 3. Seed Material Listing
        _supplier1Listing = new MaterialListing
        {
            SupplierOrganizationId = _supplier1Org.Id,
            Name = "Egyptian Cotton Poplin",
            Category = "Fabrics",
            Unit = "meter",
            UnitPrice = 180.00m,
            AvailableQuantity = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MaterialListings.Add(_supplier1Listing);
        await _context.SaveChangesAsync();

        // 4. Seed Material Order between Brand1 (Buyer) and Supplier1 (Seller)
        _materialOrder1 = new MaterialOrder
        {
            BuyerOrganizationId = _brand1Org.Id,
            SupplierOrganizationId = _supplier1Org.Id,
            Status = MaterialOrderStatus.PendingReview,
            TotalAmount = 1800.00m,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<MaterialOrderLine>
            {
                new()
                {
                    MaterialListingId = _supplier1Listing.Id,
                    MaterialNameSnapshot = _supplier1Listing.Name,
                    UnitSnapshot = _supplier1Listing.Unit,
                    UnitPriceSnapshot = _supplier1Listing.UnitPrice,
                    Quantity = 10,
                    LineTotal = 1800.00m,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        _context.MaterialOrders.Add(_materialOrder1);
        await _context.SaveChangesAsync();

        // 5. Seed Manufacturing Request between Brand1 and Factory1
        _mfgRequest1 = new ManufacturingRequest
        {
            BrandOrganizationId = _brand1Org.Id,
            FactoryOrganizationId = _factory1Org.Id,
            ProductType = "Men's Polo Shirts",
            DesignSpecification = "100% pique cotton polo, slim fit",
            MaterialDescription = "Pique Cotton 220 GSM",
            Quantity = 200,
            CurrentOfferPrice = 120.00m,
            Status = ManufacturingRequestStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow,
            Offers = new List<RequestOffer>
            {
                new()
                {
                    OfferedByOrganizationId = _brand1Org.Id,
                    UnitPrice = 120.00m,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        _context.ManufacturingRequests.Add(_mfgRequest1);
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
    public async Task TC_AUTHZ_01_InitiatingBrand_ViewsOwnManufacturingRequest_Allowed()
    {
        var ownership = new OwnershipService(_context);
        bool hasAccess = await ownership.CanAccessManufacturingRequestAsync(_brand1User.Id, _mfgRequest1.Id);
        Assert.True(hasAccess, "Brand 1 must be permitted to view their own request (TC-AUTHZ-01).");
    }

    [Fact]
    public async Task TC_AUTHZ_02_TargetFactory_ViewsAddressedManufacturingRequest_Allowed()
    {
        var ownership = new OwnershipService(_context);
        bool hasAccess = await ownership.CanAccessManufacturingRequestAsync(_factory1User.Id, _mfgRequest1.Id);
        Assert.True(hasAccess, "Target Factory 1 must be permitted to view requests addressed to them (TC-AUTHZ-02).");
    }

    [Fact]
    public async Task TC_AUTHZ_03_ThirdPartyBrand_ViewsManufacturingRequest_Forbidden()
    {
        var ownership = new OwnershipService(_context);
        bool hasAccess = await ownership.CanAccessManufacturingRequestAsync(_brand2User.Id, _mfgRequest1.Id);
        Assert.False(hasAccess, "Brand 2 must NOT access Brand 1's request (TC-AUTHZ-03).");

        // Controller HTTP verification
        var controller = new ManufacturingRequestsController(
            new ManufacturingRequestService(_context, new StatusHistoryService(_context), new NotificationService(_context)),
            new OrganizationService(_context),
            ownership);
        TestControllerHelper.AttachUserContext(controller, _brand2User.Id, "Brand");

        var result = await controller.Details(_mfgRequest1.Id);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task TC_AUTHZ_04_ForeignFactory_ViewsManufacturingRequest_Forbidden()
    {
        var ownership = new OwnershipService(_context);
        bool hasAccess = await ownership.CanAccessManufacturingRequestAsync(_factory2User.Id, _mfgRequest1.Id);
        Assert.False(hasAccess, "Factory 2 must NOT access Factory 1's request (TC-AUTHZ-04).");

        // Controller HTTP verification
        var controller = new ManufacturingRequestsController(
            new ManufacturingRequestService(_context, new StatusHistoryService(_context), new NotificationService(_context)),
            new OrganizationService(_context),
            ownership);
        TestControllerHelper.AttachUserContext(controller, _factory2User.Id, "Factory");

        var result = await controller.Details(_mfgRequest1.Id);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task TC_AUTHZ_05_Buyer_ViewsPlacedMaterialOrder_Allowed()
    {
        var ownership = new OwnershipService(_context);
        bool hasAccess = await ownership.CanAccessMaterialOrderAsync(_brand1User.Id, _materialOrder1.Id);
        Assert.True(hasAccess, "Buyer (Brand 1) must be permitted to view their own placed order (TC-AUTHZ-05).");
    }

    [Fact]
    public async Task TC_AUTHZ_06_TargetSupplier_ViewsIncomingMaterialOrder_Allowed()
    {
        var ownership = new OwnershipService(_context);
        bool hasAccess = await ownership.CanAccessMaterialOrderAsync(_supplier1User.Id, _materialOrder1.Id);
        Assert.True(hasAccess, "Target Supplier 1 must be permitted to view incoming orders (TC-AUTHZ-06).");
    }

    [Fact]
    public async Task TC_AUTHZ_07_ThirdPartyBuyer_AccessesMaterialOrder_Forbidden()
    {
        var ownership = new OwnershipService(_context);
        bool hasAccess = await ownership.CanAccessMaterialOrderAsync(_brand2User.Id, _materialOrder1.Id);
        Assert.False(hasAccess, "Brand 2 must NOT access Brand 1's order (TC-AUTHZ-07).");

        // Controller HTTP verification for Details
        var controller = CreateOrdersController(ownership);
        TestControllerHelper.AttachUserContext(controller, _brand2User.Id, "Brand");

        var detailsResult = await controller.Details(_materialOrder1.Id);
        Assert.IsType<ForbidResult>(detailsResult);

        // Controller HTTP verification for Confirm attempt
        var confirmResult = await controller.Confirm(_materialOrder1.Id);
        Assert.IsType<ForbidResult>(confirmResult);
    }

    [Fact]
    public async Task TC_AUTHZ_08_OwningSupplier_CanModifyMaterialListing_Allowed()
    {
        var ownership = new OwnershipService(_context);
        bool canModify = await ownership.CanModifyMaterialListingAsync(_supplier1User.Id, _supplier1Listing.Id);
        Assert.True(canModify, "Owning Supplier must be permitted to modify listing (TC-AUTHZ-08).");
    }

    [Fact]
    public async Task TC_AUTHZ_09_ForeignSupplier_AttemptsToModifyListing_Forbidden()
    {
        var ownership = new OwnershipService(_context);
        bool canModify = await ownership.CanModifyMaterialListingAsync(_supplier2User.Id, _supplier1Listing.Id);
        Assert.False(canModify, "Supplier 2 must NOT be permitted to modify Supplier 1's listing (TC-AUTHZ-09).");
    }

    [Fact]
    public async Task TC_AUTHZ_10_User_AttemptsToAccessOtherOrganizationProfile_Forbidden()
    {
        var ownership = new OwnershipService(_context);
        bool canAccess = await ownership.CanAccessOrganizationAsync(_brand1User.Id, _brand2Org.Id);
        Assert.False(canAccess, "User of Brand 1 cannot access or edit Brand 2 profile (TC-AUTHZ-10).");
    }

    [Fact]
    public async Task CrossOrg_ManufacturingRequest_OfferDecisions_ReturnForbidden()
    {
        var ownership = new OwnershipService(_context);
        var mfgService = new ManufacturingRequestService(_context, new StatusHistoryService(_context), new NotificationService(_context));
        var orgService = new OrganizationService(_context);
        var controller = new ManufacturingRequestsController(mfgService, orgService, ownership);

        // Foreign factory attempts to Accept
        TestControllerHelper.AttachUserContext(controller, _factory2User.Id, "Factory");
        var acceptResult = await controller.Accept(new RequestDecisionViewModel { RequestId = _mfgRequest1.Id });
        Assert.IsType<ForbidResult>(acceptResult);

        // Foreign factory attempts to CounterOffer
        var counterResult = await controller.CounterOffer(new OfferActionViewModel { RequestId = _mfgRequest1.Id, OfferPrice = 130m, Note = "Foreign counter" });
        Assert.IsType<ForbidResult>(counterResult);

        // Foreign brand attempts to Cancel
        TestControllerHelper.AttachUserContext(controller, _brand2User.Id, "Brand");
        var cancelResult = await controller.Cancel(new RequestDecisionViewModel { RequestId = _mfgRequest1.Id });
        Assert.IsType<ForbidResult>(cancelResult);
    }

    [Fact]
    public async Task UnverifiedAccount_CannotCreateManufacturingRequest()
    {
        var mfgService = new ManufacturingRequestService(_context, new StatusHistoryService(_context), new NotificationService(_context));

        // Unverified Brand attempting to create a request must throw InvalidOperationException
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mfgService.CreateRequestAsync(
                _unverifiedBrandOrg.Id,
                _factory1Org.Id,
                "Linen Trousers",
                "Wide leg linen trousers",
                "100% French Linen 180 GSM",
                100,
                200.00m,
                null,
                null,
                _unverifiedBrandUser.Id));

        Assert.Contains("Verified", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Brand_CannotTargetUnverifiedFactory_ForManufacturingRequest()
    {
        // Create an unverified factory
        var unverFactoryUser = new ApplicationUser { Id = "unver-fac-user", UserName = "unverfac@sila.com", Email = "unverfac@sila.com", FullName = "Unverified Factory" };
        _context.Users.Add(unverFactoryUser);
        await _context.SaveChangesAsync();

        var unverFactoryOrg = new Organization
        {
            Name = "Unverified Factory Org",
            Type = OrganizationType.Factory,
            City = "Suez",
            ContactPhone = "01088889999",
            OwnerUserId = unverFactoryUser.Id,
            VerificationStatus = VerificationStatus.Unverified,
            CreatedAt = DateTime.UtcNow
        };
        _context.Organizations.Add(unverFactoryOrg);
        await _context.SaveChangesAsync();

        var mfgService = new ManufacturingRequestService(_context, new StatusHistoryService(_context), new NotificationService(_context));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mfgService.CreateRequestAsync(
                _brand1Org.Id,
                unverFactoryOrg.Id,
                "Cotton Shirts",
                "Classic fit",
                "Cotton 100%",
                50,
                150.00m,
                null,
                null,
                _brand1User.Id));

        Assert.Contains("Verified factory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private OrdersController CreateOrdersController(IOwnershipService ownership)
    {
        var orderService = new MaterialOrderService(_context, new StatusHistoryService(_context), new NotificationService(_context));
        var statusHistory = new StatusHistoryService(_context);

        // Dummy userManager
        var userStore = new DummyUserStore();
        var userManager = new UserManager<ApplicationUser>(userStore, null!, null!, null!, null!, null!, null!, null!, null!);

        return new OrdersController(orderService, ownership, statusHistory, _context, userManager);
    }

    private class DummyUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
    }
}
