// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.1 | Owner: P3 | Status: DONE
// What exists: Supplier Manage Listings ViewModel for dashboard table of own materials with status badges and quick actions
// What is left: // TODO(T3.2): P1 links incoming material orders directly from listing rows
// Depends on:  T1.1 (MaterialListing, Organization)
// Used by:     MaterialsController.Manage, Views/Materials/Manage.cshtml
// How to test: Log in as Supplier -> visit /Materials/Manage -> view list of materials and verification notice
// Notes:       BR-05: Deactivate action (soft-delete). BR-06: Out of stock indicator at 0.
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Models.ViewModels.Materials;

public class SupplierManageListingsViewModel
{
    public IEnumerable<MaterialListing> Listings { get; set; } = new List<MaterialListing>();

    public Organization? SupplierOrganization { get; set; }

    public bool IsVerified { get; set; }

    public int TotalCount => Listings.Count();

    public int ActiveCount => Listings.Count(l => l.IsActive);

    public int OutOfStockCount => Listings.Count(l => l.IsActive && l.AvailableQuantity == 0);
}

