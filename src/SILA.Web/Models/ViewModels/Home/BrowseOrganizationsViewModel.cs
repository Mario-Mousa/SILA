// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.3 | Owner: P3 | Status: DONE
// What exists: ViewModel for public directory browsing (Suppliers & Factories) with search, filter, and pagination
// What is left: // TODO(T3.1): Integrate material catalog links from supplier profile
// Depends on:  T1.1 (Organization, OrganizationType, VerificationStatus)
// Used by:     HomeController (Suppliers, Factories actions), Views/Home/Suppliers.cshtml, Views/Home/Factories.cshtml
// How to test: Navigate to /Home/Suppliers or /Home/Factories; test keyword search, city filter, and pagination
// Notes:       Only organizations with VerificationStatus == Verified must be queried and displayed
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.Home;

public class BrowseOrganizationsViewModel
{
    public OrganizationType OrganizationType { get; set; }

    public IEnumerable<Organization> Organizations { get; set; } = new List<Organization>();

    public string? SearchTerm { get; set; }

    public string? SelectedCity { get; set; }

    public IEnumerable<string> AvailableCities { get; set; } = new List<string>();

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalCount { get; set; } = 0;

    public int PageSize { get; set; } = 9;
}

