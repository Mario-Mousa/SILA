// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for MaterialListing CRUD and catalog search
// What is left: // TODO(T3.1): Implement MaterialListingService with CRUD, deactivation, search/filter, and concurrency
// Depends on:  T1.1 (MaterialListing entity, ApplicationDbContext)
// Used by:     T3.1, T3.2, MaterialsController
// How to test: Build project; stub throws NotImplementedException until implemented in T3.1
// Notes:       P3 owns implementation in T3.1
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class MaterialListingService : IMaterialListingService
{
    private readonly ApplicationDbContext _context;

    public MaterialListingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<MaterialListing?> GetByIdAsync(int id)
    {
        // TODO(T3.1): Retrieve listing by Id
        throw new NotImplementedException("TODO(T3.1): Implement MaterialListingService.GetByIdAsync");
    }

    public Task<MaterialListing> CreateListingAsync(MaterialListing listing)
    {
        // TODO(T3.1): Validate fields (unit price > 0, qty >= 0) and persist
        throw new NotImplementedException("TODO(T3.1): Implement MaterialListingService.CreateListingAsync");
    }

    public Task<bool> UpdateListingAsync(MaterialListing listing)
    {
        // TODO(T3.1): Update listing details with RowVersion concurrency check
        throw new NotImplementedException("TODO(T3.1): Implement MaterialListingService.UpdateListingAsync");
    }

    public Task<bool> DeactivateListingAsync(int id, int supplierOrgId)
    {
        // TODO(T3.1): Soft-deactivate listing (IsActive = false) preventing future orders (BR-05)
        throw new NotImplementedException("TODO(T3.1): Implement MaterialListingService.DeactivateListingAsync");
    }

    public Task<IEnumerable<MaterialListing>> GetSupplierListingsAsync(int supplierOrgId)
    {
        // TODO(T3.1): Return all listings for a supplier
        throw new NotImplementedException("TODO(T3.1): Implement MaterialListingService.GetSupplierListingsAsync");
    }

    public Task<(IEnumerable<MaterialListing> Items, int TotalCount)> BrowseActiveListingsAsync(
        string? search = null,
        string? category = null,
        int page = 1,
        int pageSize = 12)
    {
        // TODO(T3.1): Query active listings with search filter and pagination
        throw new NotImplementedException("TODO(T3.1): Implement MaterialListingService.BrowseActiveListingsAsync");
    }

    public Task<IEnumerable<string>> GetCategoriesAsync()
    {
        // TODO(T3.1): Return distinct categories from active listings
        throw new NotImplementedException("TODO(T3.1): Implement MaterialListingService.GetCategoriesAsync");
    }
}

