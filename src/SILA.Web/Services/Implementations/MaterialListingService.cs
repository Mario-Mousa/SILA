// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.1 | Owner: P3 | Status: DONE
// What exists: Full implementation of IMaterialListingService: CRUD, deactivation, search/filter, and concurrency
// What is left: // TODO(T3.2): P1 connects atomic stock deduction & reservation during order confirmation
// Depends on:  T1.1 (MaterialListing, Organization, VerificationStatus), ApplicationDbContext
// Used by:     MaterialsController, T3.2 (MaterialOrderService)
// How to test: 
//   1. Call CreateListingAsync with positive price and stock -> verify listing is saved with IsActive=true.
//   2. Call DeactivateListingAsync -> verify IsActive is set to false (soft-deactivation, BR-05).
//   3. Call BrowseActiveListingsAsync -> verify search, category filter, and only active listings from verified suppliers return.
// Notes:       BR-01: UnitPrice > 0, AvailableQuantity >= 0. BR-05: Referenced listings can never be hard-deleted. BR-06: Out of stock at 0.
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class MaterialListingService : IMaterialListingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaterialListingService> _logger;

    public MaterialListingService(ApplicationDbContext context, ILogger<MaterialListingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MaterialListing?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;

        return await _context.MaterialListings
            .AsNoTracking()
            .Include(l => l.SupplierOrganization)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    /// <inheritdoc />
    public async Task<MaterialListing> CreateListingAsync(MaterialListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        // BR-01: Quantities are positive whole numbers. Unit price > 0. Available stock >= 0.
        if (listing.UnitPrice <= 0)
        {
            throw new ArgumentException("Unit price must be strictly greater than zero (BR-01).", nameof(listing.UnitPrice));
        }

        if (listing.AvailableQuantity < 0)
        {
            throw new ArgumentException("Available quantity cannot be negative (BR-01).", nameof(listing.AvailableQuantity));
        }

        if (string.IsNullOrWhiteSpace(listing.Name))
        {
            throw new ArgumentException("Material listing name is required.", nameof(listing.Name));
        }

        if (string.IsNullOrWhiteSpace(listing.Category))
        {
            throw new ArgumentException("Category is required.", nameof(listing.Category));
        }

        if (string.IsNullOrWhiteSpace(listing.Unit))
        {
            throw new ArgumentException("Unit of measurement is required.", nameof(listing.Unit));
        }

        listing.Name = listing.Name.Trim();
        listing.Category = listing.Category.Trim();
        listing.Description = listing.Description?.Trim();
        listing.Unit = listing.Unit.Trim();
        listing.IsActive = true;
        listing.CreatedAt = DateTime.UtcNow;
        listing.UpdatedAt = null;

        _context.MaterialListings.Add(listing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created MaterialListing {Id} '{Name}' for supplier org {OrgId}",
            listing.Id, listing.Name, listing.SupplierOrganizationId);

        return listing;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateListingAsync(MaterialListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        if (listing.Id <= 0) return false;

        // BR-01: Unit price > 0, Available stock >= 0.
        if (listing.UnitPrice <= 0 || listing.AvailableQuantity < 0)
        {
            return false;
        }

        var existing = await _context.MaterialListings.FirstOrDefaultAsync(l => l.Id == listing.Id);
        if (existing == null)
        {
            return false;
        }

        // Apply RowVersion original value for optimistic concurrency check
        if (listing.RowVersion != null && listing.RowVersion.Length > 0)
        {
            _context.Entry(existing).Property(p => p.RowVersion).OriginalValue = listing.RowVersion;
        }

        existing.Name = listing.Name.Trim();
        existing.Category = listing.Category.Trim();
        existing.Description = listing.Description?.Trim();
        existing.Unit = listing.Unit.Trim();
        existing.UnitPrice = listing.UnitPrice;
        existing.AvailableQuantity = listing.AvailableQuantity;
        existing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated MaterialListing {Id} '{Name}'", existing.Id, existing.Name);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected while updating MaterialListing {Id}", listing.Id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateListingAsync(int id, int supplierOrgId)
    {
        if (id <= 0 || supplierOrgId <= 0) return false;

        var existing = await _context.MaterialListings
            .FirstOrDefaultAsync(l => l.Id == id && l.SupplierOrganizationId == supplierOrgId);

        if (existing == null)
        {
            return false;
        }

        // BR-05: A listing referenced by any order can NEVER be hard-deleted; only deactivated.
        // Deactivated listings are hidden from catalog and blocked for new orders, but old orders stay readable.
        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Deactivated MaterialListing {Id} for supplier org {OrgId} (BR-05)", id, supplierOrgId);

        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MaterialListing>> GetSupplierListingsAsync(int supplierOrgId)
    {
        if (supplierOrgId <= 0) return Enumerable.Empty<MaterialListing>();

        return await _context.MaterialListings
            .AsNoTracking()
            .Include(l => l.SupplierOrganization)
            .Where(l => l.SupplierOrganizationId == supplierOrgId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<MaterialListing> Items, int TotalCount)> BrowseActiveListingsAsync(
        string? search = null,
        string? category = null,
        int page = 1,
        int pageSize = 12)
    {
        // Public Catalog query: ONLY active listings from Verified Supplier organizations (BR-05, D-08, Part 4.1)
        var query = _context.MaterialListings
            .AsNoTracking()
            .Include(l => l.SupplierOrganization)
            .Where(l => l.IsActive && l.SupplierOrganization.VerificationStatus == VerificationStatus.Verified);

        // Keyword search filter (Name, Description, Supplier Name)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l => l.Name.ToLower().Contains(term) ||
                                     (l.Description != null && l.Description.ToLower().Contains(term)) ||
                                     l.SupplierOrganization.Name.ToLower().Contains(term));
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(l => l.Category.ToLower() == cat);
        }

        int totalCount = await query.CountAsync();

        // In-stock items prioritized, then sorted by newest
        var items = await query
            .OrderByDescending(l => l.AvailableQuantity > 0)
            .ThenByDescending(l => l.CreatedAt)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        return await _context.MaterialListings
            .AsNoTracking()
            .Where(l => l.IsActive && l.SupplierOrganization.VerificationStatus == VerificationStatus.Verified && !string.IsNullOrWhiteSpace(l.Category))
            .Select(l => l.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}
