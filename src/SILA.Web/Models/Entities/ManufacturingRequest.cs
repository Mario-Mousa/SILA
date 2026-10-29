// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: ManufacturingRequest entity for Brand-to-Factory manufacturing workflow
// What is left: // TODO(T3.3): Implement request creation, accept, reject, counter-offer, revise, cancel
// Depends on:  Organization, ManufacturingRequestStatus
// Used by:     T3.3, T3.4, T4.1
// How to test: Database migration creates ManufacturingRequests table with FKs to Brand and Factory
// Notes:       D-01: Addressed to exactly one target factory. No inventory/stock effects.
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Enums;

namespace SILA.Web.Models.Entities;

public class ManufacturingRequest
{
    public int Id { get; set; }

    public int BrandOrganizationId { get; set; }
    public Organization BrandOrganization { get; set; } = null!;

    public int FactoryOrganizationId { get; set; }
    public Organization FactoryOrganization { get; set; } = null!;

    public string ProductType { get; set; } = string.Empty;
    public string DesignSpecification { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public decimal CurrentOfferPrice { get; set; }
    public string? AttachmentPath { get; set; }
    public string? Notes { get; set; }

    public ManufacturingRequestStatus Status { get; set; } = ManufacturingRequestStatus.PendingApproval;
    public string? DecisionNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<RequestOffer> Offers { get; set; } = new List<RequestOffer>();
}
