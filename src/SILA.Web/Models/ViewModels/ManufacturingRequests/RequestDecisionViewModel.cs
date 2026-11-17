// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.3 | Owner: P2 | Status: DONE
// What exists: Optional note model for accepting or rejecting a Manufacturing Request.
// What is left: // TODO(T4.3): Review validation presentation.
// Depends on:  T1.1 (request lifecycle).
// Used by:     ManufacturingRequestsController decision endpoints and Details.cshtml.
// How to test: Build the web project and submit a decision with a note.
// Notes:       An empty note is allowed by the lifecycle rules.
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace SILA.Web.Models.ViewModels.ManufacturingRequests;

public sealed class RequestDecisionViewModel
{
    [Range(1, int.MaxValue)]
    public int RequestId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
