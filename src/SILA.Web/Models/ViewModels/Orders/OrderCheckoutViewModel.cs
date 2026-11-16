// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.2 | Owner: P1 | Status: DONE
// What exists: ViewModels for Material Order checkout and submission
// What is left: // TODO(T3.4): Wire into Sent/Received dashboard tabs
// Depends on:  T1.1 (MaterialListing, Organization), T3.1 (MaterialListingService)
// Used by:     OrdersController.Checkout, Views/Orders/Checkout.cshtml
// How to test: Browse material -> Click Order -> verify checkout summary with snapshot price in EGP
// Notes:       BR-01: Quantities are positive. BR-02: Single supplier. BR-04: Snapshot values.
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace SILA.Web.Models.ViewModels.Orders;

public class OrderCheckoutViewModel
{
    public int ListingId { get; set; }
    public string ListingName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int AvailableQuantity { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1 unit.")]
    public int Quantity { get; set; } = 1;

    public int SupplierOrgId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCity { get; set; } = string.Empty;
    public string SupplierPhone { get; set; } = string.Empty;

    public decimal EstimatedTotal => Quantity * UnitPrice;
    public bool IsOutOfStock => AvailableQuantity <= 0;
}

public class OrderCheckoutSubmitModel
{
    [Required]
    public int ListingId { get; set; }

    [Required]
    public int SupplierOrgId { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer (BR-01).")]
    public int Quantity { get; set; }
}
