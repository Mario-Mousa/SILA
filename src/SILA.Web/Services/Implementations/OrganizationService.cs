// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.2 | Owner: P2 | Status: DONE
// What exists: Full implementation of IOrganizationService: profiles retrieval, role-specific profile updates, verified directory lookups with pagination and search, and verification check helper
// What is left: none
// Depends on:  T1.1 (Organization, VerificationDocument, Enums, ApplicationDbContext)
// Used by:     ProfileController, AdminController, T2.3 (Browse Factories/Suppliers), T3.1, T3.3
// How to test: Query profiles by Id / UserId; update profile fields; verify GetFactoriesAsync/GetSuppliersAsync only return Verified organizations
// Notes:       Public browse methods strictly enforce VerificationStatus == Verified (D-12, Part 4.1)
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;

    public OrganizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Organization?> GetByIdAsync(int id)
    {
        return await _context.Organizations
            .Include(o => o.OwnerUser)
            .Include(o => o.VerificationDocuments)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Organization?> GetByUserIdAsync(string userId)
    {
        return await _context.Organizations
            .Include(o => o.OwnerUser)
            .Include(o => o.VerificationDocuments)
            .FirstOrDefaultAsync(o => o.OwnerUserId == userId);
    }

    public async Task<bool> UpdateProfileAsync(Organization organization)
    {
        var existing = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organization.Id);
        if (existing == null)
        {
            return false;
        }

        // Common profile fields
        existing.Name = organization.Name.Trim();
        existing.City = organization.City.Trim();
        existing.ContactPhone = organization.ContactPhone.Trim();
        existing.Description = organization.Description?.Trim();

        if (!string.IsNullOrWhiteSpace(organization.LogoPath))
        {
            existing.LogoPath = organization.LogoPath;
        }

        // Role-specific fields
        switch (existing.Type)
        {
            case OrganizationType.Brand:
                existing.InstagramUrl = organization.InstagramUrl?.Trim();
                existing.PortfolioText = organization.PortfolioText?.Trim();
                break;

            case OrganizationType.Factory:
                existing.Address = organization.Address?.Trim();
                break;

            case OrganizationType.Supplier:
                // Supplier only uses description, phone, city
                break;
        }

        existing.UpdatedAt = DateTime.UtcNow;

        _context.Organizations.Update(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(IEnumerable<Organization> Items, int TotalCount)> GetFactoriesAsync(
        string? search = null,
        string? city = null,
        int page = 1,
        int pageSize = 10)
    {
        // Public directory query: ONLY verified Factory organizations (Part 4.1, T2.3 dependency)
        var query = _context.Organizations
            .AsNoTracking()
            .Where(o => o.Type == OrganizationType.Factory && o.VerificationStatus == VerificationStatus.Verified);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(term) || (o.Description != null && o.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityTerm = city.Trim().ToLower();
            query = query.Where(o => o.City.ToLower().Contains(cityTerm));
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Organization> Items, int TotalCount)> GetSuppliersAsync(
        string? search = null,
        string? city = null,
        int page = 1,
        int pageSize = 10)
    {
        // Public directory query: ONLY verified Supplier organizations (Part 4.1, T2.3 dependency)
        var query = _context.Organizations
            .AsNoTracking()
            .Where(o => o.Type == OrganizationType.Supplier && o.VerificationStatus == VerificationStatus.Verified);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(term) || (o.Description != null && o.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityTerm = city.Trim().ToLower();
            query = query.Where(o => o.City.ToLower().Contains(cityTerm));
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> IsVerifiedAsync(int organizationId)
    {
        return await _context.Organizations
            .AnyAsync(o => o.Id == organizationId && o.VerificationStatus == VerificationStatus.Verified);
    }
}
