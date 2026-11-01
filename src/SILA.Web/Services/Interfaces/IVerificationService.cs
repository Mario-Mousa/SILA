// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for organization verification and admin approvals
// What is left: // TODO(T2.2): Implement VerificationService with file upload validations and Admin decisions
// Depends on:  T1.1 (Organization, VerificationDocument, VerificationStatus)
// Used by:     T2.2, AdminController, ProfileController
// How to test: Compile project; ensure DI registration compiles
// Notes:       Admin approval/rejection updates Organization.VerificationStatus
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for handling organization verification documents and Admin review operations.
/// </summary>
public interface IVerificationService
{
    /// <summary>
    /// Submits a verification document for an organization and transitions status to PendingReview.
    /// </summary>
    Task<bool> SubmitVerificationDocumentAsync(int organizationId, string documentType, string filePath);

    /// <summary>
    /// Gets all verification documents uploaded by a specific organization.
    /// </summary>
    Task<IEnumerable<VerificationDocument>> GetDocumentsByOrganizationIdAsync(int organizationId);

    /// <summary>
    /// Retrieves all organizations currently awaiting Admin review (VerificationStatus == PendingReview).
    /// </summary>
    Task<IEnumerable<Organization>> GetPendingOrganizationsAsync();

    /// <summary>
    /// Approves an organization's verification request (sets VerificationStatus to Verified).
    /// </summary>
    Task<bool> ApproveOrganizationAsync(int organizationId, string? note = null);

    /// <summary>
    /// Rejects an organization's verification request with a mandatory reason note.
    /// </summary>
    Task<bool> RejectOrganizationAsync(int organizationId, string note);
}

