// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: RequestOffer entity tracking negotiation rounds (offers & counter-offers)
// What is left: // TODO(T3.3): Append RequestOffer on counter-offer and revised-offer actions
// Depends on:  ManufacturingRequest, Organization
// Used by:     T3.3
// How to test: Database migration creates RequestOffers table with FKs to Request and Offering Organization
// Notes:       D-11: Tracks full history of offer prices per round
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Models.Entities;

public class RequestOffer
{
    public int Id { get; set; }

    public int ManufacturingRequestId { get; set; }
    public ManufacturingRequest ManufacturingRequest { get; set; } = null!;

    public int OfferedByOrganizationId { get; set; }
    public Organization OfferedByOrganization { get; set; } = null!;

    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
    public int RoundNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
