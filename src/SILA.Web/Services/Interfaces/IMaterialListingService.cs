// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for Supplier material catalog and Buyer browsing
// What is left: // TODO(T3.1): Implement MaterialListingService with CRUD, deactivation, search/filter, and concurrency
// Depends on:  T1.1 (MaterialListing entity, ApplicationDbContext)
// Used by:     T3.1, T3.2, MaterialsController
// How to test: Compile project; ensure DI registration compiles
// Notes:       BR-05: Listings with existing orders are deactivated, never hard-deleted. BR-06: Out of stock at 0
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for managing raw material listings and catalog browsing.
/// </summary>
public interface IMaterialListingService
{
    /// <summary>
    /// Gets a specific material listing by its identifier.
    /// </summary>
    Task<MaterialListing?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new material listing for a verified Supplier.
    /// </summary>
    Task<MaterialListing> CreateListingAsync(MaterialListing listing);

    /// <summary>
    /// Updates details of an existing listing (name, category, description, unit, unit price, stock).
    /// </summary>
    Task<bool> UpdateListingAsync(MaterialListing listing);

    /// <summary>
    /// Soft-deactivates a listing (IsActive = false) so it cannot be ordered further while preserving order history (BR-05).
    /// </summary>
    Task<bool> DeactivateListingAsync(int id, int supplierOrgId);

    /// <summary>
    /// Retrieves all listings owned by a specific supplier (both active and deactivated).
    /// </summary>
    Task<IEnumerable<MaterialListing>> GetSupplierListingsAsync(int supplierOrgId);

    /// <summary>
    /// Browses active material listings with optional keyword search, category filter, and pagination (D-08).
    /// </summary>
    Task<(IEnumerable<MaterialListing> Items, int TotalCount)> BrowseActiveListingsAsync(
        string? search = null,
        string? category = null,
        int page = 1,
        int pageSize = 12);

    /// <summary>
    /// Retrieves a list of distinct active categories for filtering the catalog.
    /// </summary>
    Task<IEnumerable<string>> GetCategoriesAsync();
}

