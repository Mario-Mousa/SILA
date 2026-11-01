// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for Organization profiles and directory queries
// What is left: // TODO(T2.2): Implement OrganizationService with profile updates and directory filters
// Depends on:  T1.1 (Organization entity, OrganizationType enum)
// Used by:     T2.2, T2.3, T3.1, T3.3
// How to test: Build project; stub throws NotImplementedException until implemented in T2.2
// Notes:       P2 owns implementation in T2.2
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;

    public OrganizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Organization?> GetByIdAsync(int id)
    {
        // TODO(T2.2): Implement query by Id with related verification status
        throw new NotImplementedException("TODO(T2.2): Implement OrganizationService.GetByIdAsync");
    }

    public Task<Organization?> GetByUserIdAsync(string userId)
    {
        // TODO(T2.2): Implement query by OwnerUserId
        throw new NotImplementedException("TODO(T2.2): Implement OrganizationService.GetByUserIdAsync");
    }

    public Task<bool> UpdateProfileAsync(Organization organization)
    {
        // TODO(T2.2): Implement organization profile update with role-specific fields
        throw new NotImplementedException("TODO(T2.2): Implement OrganizationService.UpdateProfileAsync");
    }

    public Task<(IEnumerable<Organization> Items, int TotalCount)> GetFactoriesAsync(string? search = null, string? city = null, int page = 1, int pageSize = 10)
    {
        // TODO(T2.2): Implement paginated query for verified Factory organizations
        throw new NotImplementedException("TODO(T2.2): Implement OrganizationService.GetFactoriesAsync");
    }

    public Task<(IEnumerable<Organization> Items, int TotalCount)> GetSuppliersAsync(string? search = null, string? city = null, int page = 1, int pageSize = 10)
    {
        // TODO(T2.2): Implement paginated query for verified Supplier organizations
        throw new NotImplementedException("TODO(T2.2): Implement OrganizationService.GetSuppliersAsync");
    }

    public Task<bool> IsVerifiedAsync(int organizationId)
    {
        // TODO(T2.2): Implement verification check against VerificationStatus.Verified
        throw new NotImplementedException("TODO(T2.2): Implement OrganizationService.IsVerifiedAsync");
    }
}

