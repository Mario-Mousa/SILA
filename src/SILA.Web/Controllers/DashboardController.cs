// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.3 | Owner: P3 | Status: DONE
// What exists: DashboardController routing authenticated users to their role-specific dashboard shell (Brand, Factory, Supplier)
// What is left: // TODO(T3.4): P4 to bind live metric cards, incoming/outgoing requests, material orders, and notifications
// Depends on:  T1.1 (ApplicationUser, Organization, VerificationStatus), T2.2 (IOrganizationService)
// Used by:     Brand, Factory, and Supplier authenticated users
// How to test: Sign in as Brand, Factory, or Supplier; verify redirection to /Dashboard and rendering of the role-tailored shell
// Notes:       P3 builds the responsive dashboard shell in T2.3; P4 wires live database data in T3.4
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SILA.Web.Models.Entities;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        IOrganizationService organizationService,
        ILogger<DashboardController> logger)
    {
        _userManager = userManager;
        _organizationService = organizationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var organization = await _organizationService.GetByUserIdAsync(user.Id);

        ViewBag.FullName = user.FullName;
        ViewBag.Email = user.Email;
        ViewBag.Organization = organization;

        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Admin");
        }

        if (User.IsInRole("Brand"))
        {
            return View("Brand", organization);
        }

        if (User.IsInRole("Factory"))
        {
            return View("Factory", organization);
        }

        if (User.IsInRole("Supplier"))
        {
            return View("Supplier", organization);
        }

        return View(organization);
    }
}

