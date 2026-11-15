// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.1 | Owner: P3 | Status: DONE
// What exists: Material listing form ViewModel for Create and Edit actions with client and server validation
// What is left: // TODO(T3.2): P1 reads listing details during order placement
// Depends on:  T1.1 (MaterialListing entity)
// Used by:     MaterialsController (Create/Edit actions), Views/Materials/Create.cshtml, Views/Materials/Edit.cshtml
// How to test: Submit form with invalid data -> verify validation errors; submit valid data -> listing saved
// Notes:       BR-01: Unit price > 0, Available quantity >= 0. D-10: Currency is EGP.
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace SILA.Web.Models.ViewModels.Materials;

public class MaterialListingFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Material name is required.")]
    [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    [Display(Name = "Material Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(60, ErrorMessage = "Category cannot exceed 60 characters.")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Unit of measurement is required.")]
    [StringLength(30, ErrorMessage = "Unit cannot exceed 30 characters.")]
    [Display(Name = "Unit of Measurement")]
    public string Unit { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit price is required.")]
    [Range(0.01, 1000000.00, ErrorMessage = "Unit price must be strictly greater than 0 (BR-01).")]
    [Display(Name = "Unit Price (EGP)")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Available quantity is required.")]
    [Range(0, 10000000, ErrorMessage = "Available quantity must be 0 or greater (BR-01).")]
    [Display(Name = "Available Quantity")]
    public int AvailableQuantity { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control during edits.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
