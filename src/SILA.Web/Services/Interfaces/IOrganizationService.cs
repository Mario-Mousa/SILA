// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for Organization profiles and directory queries
// What is left: // TODO(T2.2): Implement OrganizationService with profile updates and directory filters
// Depends on:  T1.1 (Organization entity, OrganizationType enum)
// Used by:     T2.2, T2.3, T3.1, T3.3
// How to test: Compile project; ensure DI registration compiles
// Notes:       Used by public browse and role-specific profile management
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for managing organization profiles and public directories.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Gets an organization by its primary key identifier.
    /// </summary>
    Task<Organization?> GetByIdAsync(int id);

    /// <summary>
    /// Gets an organization by its owner's ApplicationUser Id.
    /// </summary>
    Task<Organization?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Updates profile information for an organization.
    /// </summary>
    Task<bool> UpdateProfileAsync(Organization organization);

    /// <summary>
    /// Retrieves a paginated list of verified factories for public browsing.
    /// </summary>
    Task<(IEnumerable<Organization> Items, int TotalCount)> GetFactoriesAsync(string? search = null, string? city = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Retrieves a paginated list of verified suppliers for public browsing.
    /// </summary>
    Task<(IEnumerable<Organization> Items, int TotalCount)> GetSuppliersAsync(string? search = null, string? city = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Checks whether an organization is verified by Admin (VerificationStatus == Verified).
    /// </summary>
    Task<bool> IsVerifiedAsync(int organizationId);
}

