// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for central ownership and authorization checks (BR-07)
// What is left: // TODO(T2.4): Implement OwnershipService verifying user's organization against record owners/addressees
// Depends on:  T1.1 (ApplicationUser, Organization, ManufacturingRequest, MaterialOrder, MaterialListing)
// Used by:     T2.4, all controllers and services enforcing BR-07
// How to test: Build project; stub throws NotImplementedException until implemented in T2.4
// Notes:       P4 owns implementation in T2.4
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class OwnershipService : IOwnershipService
{
    private readonly ApplicationDbContext _context;

    public OwnershipService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int?> GetUserOrganizationIdAsync(string userId)
    {
        // TODO(T2.4): Return OrganizationId for the specified user
        throw new NotImplementedException("TODO(T2.4): Implement OwnershipService.GetUserOrganizationIdAsync");
    }

    public Task<bool> CanAccessManufacturingRequestAsync(string userId, int requestId)
    {
        // TODO(T2.4): Verify user's organization is BrandOrganizationId or FactoryOrganizationId
        throw new NotImplementedException("TODO(T2.4): Implement OwnershipService.CanAccessManufacturingRequestAsync");
    }

    public Task<bool> CanAccessMaterialOrderAsync(string userId, int orderId)
    {
        // TODO(T2.4): Verify user's organization is BuyerOrganizationId or SupplierOrganizationId
        throw new NotImplementedException("TODO(T2.4): Implement OwnershipService.CanAccessMaterialOrderAsync");
    }

    public Task<bool> CanModifyMaterialListingAsync(string userId, int listingId)
    {
        // TODO(T2.4): Verify user's organization owns the listing
        throw new NotImplementedException("TODO(T2.4): Implement OwnershipService.CanModifyMaterialListingAsync");
    }

    public Task<bool> CanAccessOrganizationAsync(string userId, int organizationId)
    {
        // TODO(T2.4): Verify user is OwnerUserId of the organization
        throw new NotImplementedException("TODO(T2.4): Implement OwnershipService.CanAccessOrganizationAsync");
    }
}

