// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.1 | Owner: P3 | Status: DONE
// What exists: Material Details ViewModel for viewing single listing specs, stock status, and buyer/supplier actions
// What is left: // TODO(T3.2): P1 wires "Place Material Order" CTA for Buyer roles
// Depends on:  T1.1 (MaterialListing, Organization)
// Used by:     MaterialsController.Details, Views/Materials/Details.cshtml
// How to test: Visit /Materials/Details/{id} as Buyer, Supplier, or Visitor -> verify appropriate CTAs and stock badges
// Notes:       BR-06: Out of stock indicator at 0. D-10: Currency is EGP.
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Models.ViewModels.Materials;

public class MaterialDetailsViewModel
{
    public MaterialListing Listing { get; set; } = null!;

    public bool CanOrder { get; set; }

    public bool IsOwnerSupplier { get; set; }

    public bool IsOutOfStock => Listing.AvailableQuantity <= 0;
}
