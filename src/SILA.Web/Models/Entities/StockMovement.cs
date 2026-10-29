// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: StockMovement entity tracking inventory deductions and restocks
// What is left: // TODO(T3.2): Insert StockMovement row within the atomic confirmation transaction
// Depends on:  MaterialListing, MaterialOrder
// Used by:     T3.2
// How to test: Database migration creates StockMovements table with FKs to Listing and Order
// Notes:       BR-09: Written in the same transaction as order confirmation
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Models.Entities;

public class StockMovement
{
    public int Id { get; set; }

    public int MaterialListingId { get; set; }
    public MaterialListing MaterialListing { get; set; } = null!;

    public int MaterialOrderId { get; set; }
    public MaterialOrder MaterialOrder { get; set; } = null!;

    public int QuantityChange { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
