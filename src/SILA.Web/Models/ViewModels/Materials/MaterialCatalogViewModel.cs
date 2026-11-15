// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.1 | Owner: P3 | Status: DONE
// What exists: Material Catalog ViewModel for public & buyer material browsing with search, filter, and pagination
// What is left: // TODO(T3.2): P1 wires direct "Place Material Order" from catalog cards
// Depends on:  T1.1 (MaterialListing entity)
// Used by:     MaterialsController.Index, Views/Materials/Index.cshtml
// How to test: Visit /Materials; test keyword search, category filter, and pagination
// Notes:       D-08: keyword search & category filter. D-10: Currency is EGP.
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Models.ViewModels.Materials;

public class MaterialCatalogViewModel
{
    public IEnumerable<MaterialListing> Listings { get; set; } = new List<MaterialListing>();

    public string? SearchTerm { get; set; }

    public string? SelectedCategory { get; set; }

    public IEnumerable<string> Categories { get; set; } = new List<string>();

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalCount { get; set; } = 0;

    public int PageSize { get; set; } = 12;
}
