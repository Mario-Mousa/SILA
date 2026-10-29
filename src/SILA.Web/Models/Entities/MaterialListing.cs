// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: MaterialListing entity with RowVersion concurrency token and stock attributes
// What is left: // TODO(T3.1): Implement listing CRUD, deactivate, and catalog browse
//               // TODO(T3.2): Use conditional update and RowVersion during order confirmation
// Depends on:  Organization
// Used by:     T3.1, T3.2, T4.1
// How to test: Database migration creates MaterialListings table with check constraint AvailableQuantity >= 0
// Notes:       BR-05: Referenced listings cannot be hard-deleted, only deactivated
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace SILA.Web.Models.Entities;

public class MaterialListing
{
    public int Id { get; set; }

    public int SupplierOrganizationId { get; set; }
    public Organization SupplierOrganization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MaterialOrderLine> OrderLines { get; set; } = new List<MaterialOrderLine>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
