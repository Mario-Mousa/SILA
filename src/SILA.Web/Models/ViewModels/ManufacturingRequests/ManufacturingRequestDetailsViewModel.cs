// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.3 | Owner: P2 | Status: DONE
// What exists: View model for a request details page, including offer history and role/state action flags.
// What is left: // TODO(T4.3): Review timeline copy and responsive behavior.
// Depends on:  T1.1 (ManufacturingRequestStatus, RequestOffer), T2.4 (record access checks).
// Used by:     ManufacturingRequestsController, Views/ManufacturingRequests/Details.cshtml.
// How to test: Build the web project and load a request as each party.
// Notes:       Views receive display data only, not EF entities.
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.ManufacturingRequests;

public sealed class ManufacturingRequestDetailsViewModel
{
    public int Id { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string FactoryName { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
    public string DesignSpecification { get; init; } = string.Empty;
    public string MaterialDescription { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal CurrentOfferPrice { get; init; }
    public string? AttachmentPath { get; init; }
    public string? Notes { get; init; }
    public string? DecisionNote { get; init; }
    public ManufacturingRequestStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<OfferTimelineViewModel> Offers { get; init; } = Array.Empty<OfferTimelineViewModel>();
    public bool CanFactoryRespond { get; init; }
    public bool CanBrandRespondToCounterOffer { get; init; }
    public bool CanBrandCancel { get; init; }
}
