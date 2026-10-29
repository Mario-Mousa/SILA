// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: MaterialOrderLine entity with immutable price and name snapshots
// What is left: // TODO(T3.2): Populate snapshots and compute LineTotal upon order submission
// Depends on:  MaterialOrder, MaterialListing
// Used by:     T3.2
// How to test: Database migration creates MaterialOrderLines table with snapshot fields
// Notes:       BR-04: Later edits to listing price/name never mutate existing order line snapshots
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Models.Entities;

public class MaterialOrderLine
{
    public int Id { get; set; }

    public int MaterialOrderId { get; set; }
    public MaterialOrder MaterialOrder { get; set; } = null!;

    public int MaterialListingId { get; set; }
    public MaterialListing MaterialListing { get; set; } = null!;

    public string MaterialNameSnapshot { get; set; } = string.Empty;
    public string UnitSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }

    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
