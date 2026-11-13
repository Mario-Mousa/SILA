// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.4 | Owner: P4 | Status: DONE
// What exists: Implemented OwnershipService enforcing BR-07 record-level authorization checks
// What is left: // TODO(T3.2): Use CanAccessMaterialOrderAsync in OrdersController
//               // TODO(T3.3): Use CanAccessManufacturingRequestAsync in ManufacturingRequestsController
// Depends on:  T1.1 (Entities), T1.2 (IOwnershipService)
// Used by:     All controllers and workflow services for record-level security (BR-07)
// How to test: Unit test CanAccess* methods with authorized and unauthorized organization IDs; verify forbidden handling
// Notes:       BR-07: Users can only access records where their organization is the owner or the addressee
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

/// <summary>
/// Service implementation for central record-level authorization and organizational ownership checks (BR-07).
/// Prevents cross-organization data leakage and unauthorized modifications across both workflows.
/// </summary>
public class OwnershipService : IOwnershipService
{
    private readonly ApplicationDbContext _context;

    public OwnershipService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<int?> GetUserOrganizationIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var organization = await _context.Organizations
            .AsNoTracking()
            .Where(o => o.OwnerUserId == userId)
            .Select(o => (int?)o.Id)
            .FirstOrDefaultAsync();

        return organization;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessManufacturingRequestAsync(string userId, int requestId)
    {
        var userOrgId = await GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue || requestId <= 0)
        {
            return false;
        }

        // BR-07: User's organization must be either the Brand (sender) or the Factory (recipient)
        var hasAccess = await _context.ManufacturingRequests
            .AsNoTracking()
            .AnyAsync(r => r.Id == requestId &&
                           (r.BrandOrganizationId == userOrgId.Value ||
                            r.FactoryOrganizationId == userOrgId.Value));

        return hasAccess;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessMaterialOrderAsync(string userId, int orderId)
    {
        var userOrgId = await GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue || orderId <= 0)
        {
            return false;
        }

        // BR-07: User's organization must be either the Buyer (purchaser) or the Supplier (seller)
        var hasAccess = await _context.MaterialOrders
            .AsNoTracking()
            .AnyAsync(o => o.Id == orderId &&
                           (o.BuyerOrganizationId == userOrgId.Value ||
                            o.SupplierOrganizationId == userOrgId.Value));

        return hasAccess;
    }

    /// <inheritdoc />
    public async Task<bool> CanModifyMaterialListingAsync(string userId, int listingId)
    {
        var userOrgId = await GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue || listingId <= 0)
        {
            return false;
        }

        // BR-05 & BR-07: Only the Supplier organization that owns the listing may modify or deactivate it
        var ownsListing = await _context.MaterialListings
            .AsNoTracking()
            .AnyAsync(l => l.Id == listingId && l.SupplierOrganizationId == userOrgId.Value);

        return ownsListing;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessOrganizationAsync(string userId, int organizationId)
    {
        var userOrgId = await GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue || organizationId <= 0)
        {
            return false;
        }

        // User can only view private profile data or edit an organization profile they own
        return userOrgId.Value == organizationId;
    }
}
