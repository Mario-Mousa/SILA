// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: MaterialOrder entity for Buyer-to-Supplier raw material purchases
// What is left: // TODO(T3.2): Implement order creation, atomic confirmation, dispatch, delivery, cancellation
// Depends on:  Organization, MaterialOrderStatus
// Used by:     T3.2, T3.4, T4.1
// How to test: Database migration creates MaterialOrders table with FKs to Buyer and Supplier Organizations
// Notes:       BR-02: Lines belong to one Supplier only. Status transitions follow Part 4.4.
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Enums;

namespace SILA.Web.Models.Entities;

public class MaterialOrder
{
    public int Id { get; set; }

    public int BuyerOrganizationId { get; set; }
    public Organization BuyerOrganization { get; set; } = null!;

    public int SupplierOrganizationId { get; set; }
    public Organization SupplierOrganization { get; set; } = null!;

    public MaterialOrderStatus Status { get; set; } = MaterialOrderStatus.PendingReview;

    public decimal TotalAmount { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MaterialOrderLine> Lines { get; set; } = new List<MaterialOrderLine>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
