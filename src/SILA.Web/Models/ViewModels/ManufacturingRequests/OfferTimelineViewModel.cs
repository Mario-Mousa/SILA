// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.3 | Owner: P2 | Status: DONE
// What exists: Display-only offer round model for the Manufacturing Request negotiation timeline.
// What is left: // TODO(T4.3): Review displayed date format in the responsive UI pass.
// Depends on:  T1.1 (RequestOffer).
// Used by:     ManufacturingRequestDetailsViewModel, _OfferTimeline.cshtml.
// How to test: Build the web project and inspect a request with multiple offers.
// Notes:       Offers are rendered oldest to newest.
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Models.ViewModels.ManufacturingRequests;

public sealed class OfferTimelineViewModel
{
    public int RoundNumber { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}
