// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for organization verification and Admin review
// What is left: // TODO(T2.2): Implement VerificationService with file upload validation and Admin review flow
// Depends on:  T1.1 (Organization, VerificationDocument, VerificationStatus)
// Used by:     T2.2, AdminController, ProfileController
// How to test: Build project; stub throws NotImplementedException until implemented in T2.2
// Notes:       P2 owns implementation in T2.2
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class VerificationService : IVerificationService
{
    private readonly ApplicationDbContext _context;

    public VerificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> SubmitVerificationDocumentAsync(int organizationId, string documentType, string filePath)
    {
        // TODO(T2.2): Save verification document and update organization status to PendingReview
        throw new NotImplementedException("TODO(T2.2): Implement VerificationService.SubmitVerificationDocumentAsync");
    }

    public Task<IEnumerable<VerificationDocument>> GetDocumentsByOrganizationIdAsync(int organizationId)
    {
        // TODO(T2.2): Return uploaded documents for the specified organization
        throw new NotImplementedException("TODO(T2.2): Implement VerificationService.GetDocumentsByOrganizationIdAsync");
    }

    public Task<IEnumerable<Organization>> GetPendingOrganizationsAsync()
    {
        // TODO(T2.2): Retrieve all organizations with VerificationStatus.PendingReview for Admin
        throw new NotImplementedException("TODO(T2.2): Implement VerificationService.GetPendingOrganizationsAsync");
    }

    public Task<bool> ApproveOrganizationAsync(int organizationId, string? note = null)
    {
        // TODO(T2.2): Update organization status to Verified, record note, trigger notification
        throw new NotImplementedException("TODO(T2.2): Implement VerificationService.ApproveOrganizationAsync");
    }

    public Task<bool> RejectOrganizationAsync(int organizationId, string note)
    {
        // TODO(T2.2): Update organization status to Rejected, record mandatory note, trigger notification
        throw new NotImplementedException("TODO(T2.2): Implement VerificationService.RejectOrganizationAsync");
    }
}

