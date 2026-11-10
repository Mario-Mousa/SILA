// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.2 | Owner: P2 | Status: DONE
// What exists: Full implementation of IVerificationService: verification document persistence, transition to PendingReview, pending queue retrieval for Admin, and Approve/Reject with mandatory reason
// What is left: // TODO(T2.4): NotificationService implementation will persist in-app notification records for admin decisions
// Depends on:  T1.1 (Organization, VerificationDocument, VerificationStatus, ApplicationDbContext), T1.2 (IVerificationService, INotificationService)
// Used by:     ProfileController, AdminController
// How to test: Upload document -> verify status transitions to PendingReview -> log in as Admin -> approve or reject with note -> verify status updates
// Notes:       Rejection requires a mandatory note (BR-11, D-02). Documents uploaded are linked to Organization.
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class VerificationService : IVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<VerificationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<bool> SubmitVerificationDocumentAsync(int organizationId, string documentType, string filePath)
    {
        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId);
        if (org == null)
        {
            return false;
        }

        var document = new VerificationDocument
        {
            OrganizationId = organizationId,
            DocumentType = documentType.Trim(),
            FilePath = filePath.Trim(),
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.VerificationDocuments.Add(document);

        // Transition organization status to PendingReview when documents are submitted (Part 4.1)
        org.VerificationStatus = VerificationStatus.PendingReview;
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Verification document {DocType} submitted for Organization {OrgId}. Status set to PendingReview.",
            documentType, organizationId);

        return true;
    }

    public async Task<IEnumerable<VerificationDocument>> GetDocumentsByOrganizationIdAsync(int organizationId)
    {
        return await _context.VerificationDocuments
            .Where(d => d.OrganizationId == organizationId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Organization>> GetPendingOrganizationsAsync()
    {
        return await _context.Organizations
            .Include(o => o.OwnerUser)
            .Include(o => o.VerificationDocuments)
            .Where(o => o.VerificationStatus == VerificationStatus.PendingReview)
            .OrderBy(o => o.UpdatedAt ?? o.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveOrganizationAsync(int organizationId, string? note = null)
    {
        var org = await _context.Organizations
            .Include(o => o.OwnerUser)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
        {
            return false;
        }

        org.VerificationStatus = VerificationStatus.Verified;
        org.VerificationNote = note?.Trim();
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Organization {OrgId} ({OrgName}) approved by Admin.", org.Id, org.Name);

        // Trigger in-app notification for the organization owner (BR-10)
        try
        {
            await _notificationService.CreateNotificationAsync(
                org.OwnerUserId,
                $"Congratulations! Your organization '{org.Name}' has been verified by the administrator.",
                "/Profile");
        }
        catch (NotImplementedException)
        {
            // TODO(T2.4): NotificationService will be fully active once P4 completes T2.4
            _logger.LogDebug("NotificationService not yet implemented; skipped notification for org {OrgId}.", org.Id);
        }

        return true;
    }

    public async Task<bool> RejectOrganizationAsync(int organizationId, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("A rejection reason note is mandatory.", nameof(note));
        }

        var org = await _context.Organizations
            .Include(o => o.OwnerUser)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
        {
            return false;
        }

        org.VerificationStatus = VerificationStatus.Rejected;
        org.VerificationNote = note.Trim();
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Organization {OrgId} ({OrgName}) rejected by Admin with note: {Note}", org.Id, org.Name, note);

        // Trigger in-app notification for the organization owner (BR-10)
        try
        {
            await _notificationService.CreateNotificationAsync(
                org.OwnerUserId,
                $"Your organization verification was rejected: {note}",
                "/Profile");
        }
        catch (NotImplementedException)
        {
            // TODO(T2.4): NotificationService will be fully active once P4 completes T2.4
            _logger.LogDebug("NotificationService not yet implemented; skipped notification for org {OrgId}.", org.Id);
        }

        return true;
    }
}
