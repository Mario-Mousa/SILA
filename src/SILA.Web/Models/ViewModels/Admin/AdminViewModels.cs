// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.2 | Owner: P2 | Status: DONE
// What exists: Admin ViewModels for reviewing pending verification requests, viewing documents, and approving/rejecting
// What is left: // TODO(T2.3): Wire client-side admin styling
// Depends on:  T1.1 (Organization, VerificationDocument, Enums)
// Used by:     AdminController, Views/Admin/*
// How to test: Sign in as Admin (admin@sila.com); navigate to /Admin; review pending organizations
// Notes:       Enforces mandatory rejection notes (D-02, BR-11)
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.Admin;

public class AdminPendingListViewModel
{
    public IEnumerable<Organization> PendingOrganizations { get; set; } = new List<Organization>();
    public int TotalPendingCount { get; set; }
}

public class AdminReviewViewModel
{
    public Organization Organization { get; set; } = null!;
    public IEnumerable<VerificationDocument> Documents { get; set; } = new List<VerificationDocument>();
    public AdminDecisionInputModel Decision { get; set; } = new();
}

public class AdminDecisionInputModel
{
    public int OrganizationId { get; set; }

    [Required(ErrorMessage = "Action decision is required.")]
    public string Action { get; set; } = string.Empty; // "Approve" or "Reject"

    [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
    [Display(Name = "Feedback Note (Mandatory if rejected)")]
    public string? Note { get; set; }
}

