// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.3 | Owner: P2 | Status: DONE
// What exists: Validated input model for a Brand revised offer or Factory counter-offer.
// What is left: // TODO(T4.3): Review action wording and validation presentation.
// Depends on:  T1.1 (offer price and round rules).
// Used by:     ManufacturingRequestsController, Details.cshtml.
// How to test: Build the web project and submit a non-positive offer price.
// Notes:       Service validation also rejects non-positive prices.
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace SILA.Web.Models.ViewModels.ManufacturingRequests;

public sealed class OfferActionViewModel
{
    [Range(1, int.MaxValue)]
    public int RequestId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Offer price per unit must be greater than zero.")]
    [Display(Name = "Offer price per unit (EGP)")]
    public decimal OfferPrice { get; set; }

    [Required, StringLength(500)]
    [Display(Name = "Note to the other party")]
    public string Note { get; set; } = string.Empty;
}
