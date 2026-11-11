// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.3 | Owner: P3 | Status: DONE
// What exists: HomeController serving Home landing page, Role Choice screen, and public directories for Verified Suppliers & Factories
// What is left: // TODO(T3.1): Link verified supplier cards directly to their public material listings
// Depends on:  T1.1 (Organization, OrganizationType, VerificationStatus), T2.2 (IOrganizationService)
// Used by:     Public visitors, Brand Owners, Factory Owners, Suppliers
// How to test: 
//   1. Visit / -> verify Hero, supply chain features, and Browse CTA buttons.
//   2. Visit /Home/RoleChoice -> test selecting Brand, Factory, or Supplier with Login/Register toggle.
//   3. Visit /Home/Suppliers & /Home/Factories -> test search by keyword, city filter, and pagination.
//   4. Verify only Verified organizations appear in directory results.
// Notes:       Follows wireframes.md and ui-guidelines.md; thin controller delegating queries to IOrganizationService.
// ═══════════════════════════════════════════════════════

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SILA.Web.Models;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.Home;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

public class HomeController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IOrganizationService organizationService, ILogger<HomeController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// SILA Public Landing Page.
    /// Wireframe: Eyebrow, Hero, "Browse Suppliers" and "Browse Factories" CTA, and 3-step supply chain features.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Role-choice entry screen.
    /// Wireframe: "Sign in to SILA / Choose your organization type" or "Join SILA".
    /// </summary>
    [HttpGet]
    public IActionResult RoleChoice(string? actionType = "Register", OrganizationType? role = null)
    {
        var model = new RoleChoiceViewModel
        {
            ActionType = string.Equals(actionType, "Login", StringComparison.OrdinalIgnoreCase) ? "Login" : "Register",
            SelectedRole = role
        };

        return View(model);
    }

    /// <summary>
    /// Public directory of verified Suppliers.
    /// Allows searching by name/description, filtering by city, and paging.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Suppliers(string? search = null, string? city = null, int page = 1)
    {
        const int pageSize = 9;
        var currentPage = Math.Max(1, page);

        var (items, totalCount) = await _organizationService.GetSuppliersAsync(search, city, currentPage, pageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

        // Prepare route values for the _Pagination partial
        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;
        if (!string.IsNullOrWhiteSpace(city)) routeValues["city"] = city;

        ViewData["CurrentPage"] = currentPage;
        ViewData["TotalPages"] = totalPages;
        ViewData["PageAction"] = nameof(Suppliers);
        ViewData["PageController"] = "Home";
        ViewData["PageRouteValues"] = routeValues;

        var model = new BrowseOrganizationsViewModel
        {
            OrganizationType = OrganizationType.Supplier,
            Organizations = items,
            SearchTerm = search,
            SelectedCity = city,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };

        return View(model);
    }

    /// <summary>
    /// Public directory of verified Factories.
    /// Allows searching by name/description, filtering by city, and paging.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Factories(string? search = null, string? city = null, int page = 1)
    {
        const int pageSize = 9;
        var currentPage = Math.Max(1, page);

        var (items, totalCount) = await _organizationService.GetFactoriesAsync(search, city, currentPage, pageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

        // Prepare route values for the _Pagination partial
        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;
        if (!string.IsNullOrWhiteSpace(city)) routeValues["city"] = city;

        ViewData["CurrentPage"] = currentPage;
        ViewData["TotalPages"] = totalPages;
        ViewData["PageAction"] = nameof(Factories);
        ViewData["PageController"] = "Home";
        ViewData["PageRouteValues"] = routeValues;

        var model = new BrowseOrganizationsViewModel
        {
            OrganizationType = OrganizationType.Factory,
            Organizations = items,
            SearchTerm = search,
            SelectedCity = city,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
