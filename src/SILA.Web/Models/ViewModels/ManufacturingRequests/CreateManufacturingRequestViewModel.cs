// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.3 | Owner: P2 | Status: DONE
// What exists: Validated Brand Owner form model for creating one Manufacturing Request to one factory.
// What is left: // TODO(T4.3): Review copy and responsive form behavior with the full UI pass.
// Depends on:  T1.1 (request fields), T2.2 (verified factory directory).
// Used by:     ManufacturingRequestsController, Views/ManufacturingRequests/Create.cshtml.
// How to test: Build the web project and submit invalid/valid form values.
// Notes:       Server-side service validation remains authoritative.
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SILA.Web.Models.ViewModels.ManufacturingRequests;

public sealed class CreateManufacturingRequestViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a target factory.")]
    [Display(Name = "Target factory")]
    public int FactoryOrganizationId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Product type")]
    public string ProductType { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Design specification")]
    public string? DesignSpecification { get; set; }

    [Required, StringLength(2000)]
    [Display(Name = "Material or fabric description")]
    public string MaterialDescription { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive whole number.")]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Offer price per unit must be greater than zero.")]
    [Display(Name = "Offer price per unit (EGP)")]
    public decimal InitialOfferPrice { get; set; }

    [StringLength(500)]
    [Display(Name = "Design attachment link (optional)")]
    public string? AttachmentPath { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> VerifiedFactories { get; set; } = Array.Empty<SelectListItem>();
}
