// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.2 | Owner: P2 | Status: DONE
// What exists: AdminController for reviewing pending organizations, inspecting verification documents, and approving or rejecting accounts
// What is left: // TODO(T2.3): Wire client-side admin view styling
// Depends on:  T1.1 (Organization, VerificationDocument, Enums), T1.2 (Identity, Admin role, IVerificationService, IOrganizationService)
// Used by:     Views/Admin/*, Admin workspace
// How to test: Sign in as admin@sila.com / Admin@123456 -> visit /Admin -> review pending requests -> approve or reject with note
// Notes:       Restricted strictly to [Authorize(Roles = "Admin")] (D-02); rejection requires a mandatory note
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SILA.Web.Models.ViewModels.Admin;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly IVerificationService _verificationService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IOrganizationService organizationService,
        IVerificationService verificationService,
        ILogger<AdminController> logger)
    {
        _organizationService = organizationService;
        _verificationService = verificationService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // 1. PENDING VERIFICATIONS QUEUE
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pendingOrgs = (await _verificationService.GetPendingOrganizationsAsync()).ToList();

        var model = new AdminPendingListViewModel
        {
            PendingOrganizations = pendingOrgs,
            TotalPendingCount = pendingOrgs.Count
        };

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────
    // 2. REVIEW ORGANIZATION DETAILS & DOCUMENTS
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var org = await _organizationService.GetByIdAsync(id);
        if (org == null)
        {
            return NotFound("Organization not found.");
        }

        var documents = await _verificationService.GetDocumentsByOrganizationIdAsync(id);

        var model = new AdminReviewViewModel
        {
            Organization = org,
            Documents = documents,
            Decision = new AdminDecisionInputModel
            {
                OrganizationId = org.Id
            }
        };

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────
    // 3. APPROVE / REJECT DECISION
    // ─────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(AdminDecisionInputModel model)
    {
        if (model.OrganizationId <= 0)
        {
            return BadRequest("Invalid organization ID.");
        }

        var org = await _organizationService.GetByIdAsync(model.OrganizationId);
        if (org == null)
        {
            return NotFound("Organization not found.");
        }

        if (string.Equals(model.Action, "Approve", StringComparison.OrdinalIgnoreCase))
        {
            await _verificationService.ApproveOrganizationAsync(model.OrganizationId, model.Note);
            TempData["SuccessMessage"] = $"Organization '{org.Name}' has been successfully verified.";
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(model.Action, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(model.Note))
            {
                ModelState.AddModelError("Decision.Note", "A feedback note is required when rejecting an organization.");

                var documents = await _verificationService.GetDocumentsByOrganizationIdAsync(model.OrganizationId);
                var reviewModel = new AdminReviewViewModel
                {
                    Organization = org,
                    Documents = documents,
                    Decision = model
                };

                return View(nameof(Review), reviewModel);
            }

            await _verificationService.RejectOrganizationAsync(model.OrganizationId, model.Note);
            TempData["SuccessMessage"] = $"Organization '{org.Name}' has been marked as Rejected with the specified feedback note.";
            return RedirectToAction(nameof(Index));
        }

        return BadRequest("Unknown verification action.");
    }
}

