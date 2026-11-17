// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.3 | Owner: P2 | Status: DONE
// What exists: Manufacturing Request creation, offer rounds, strict lifecycle transitions, audit and notification calls.
// What is left: // TODO(T4.1): Add transition, ownership, and round-history service tests.
// Depends on:  T1.1 (entities/enums), T2.4 (ownership, status history, notifications).
// Used by:     ManufacturingRequestsController, T3.4 dashboards and request views.
// How to test: Build the web project; exercise each allowed/denied transition against a migrated database.
// Notes:       Manufacturing Requests never read or change Material Order stock.
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

/// <summary>Implements the Brand Owner to Factory Owner request and negotiation workflow.</summary>
public sealed class ManufacturingRequestService : IManufacturingRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IStatusHistoryService _statusHistory;
    private readonly INotificationService _notifications;

    public ManufacturingRequestService(
        ApplicationDbContext context,
        IStatusHistoryService statusHistory,
        INotificationService notifications)
    {
        _context = context;
        _statusHistory = statusHistory;
        _notifications = notifications;
    }

    public async Task<ManufacturingRequest> CreateRequestAsync(
        int brandOrgId,
        int factoryOrgId,
        string productType,
        string designSpecification,
        string materialDescription,
        int quantity,
        decimal initialOfferPrice,
        string? attachmentPath = null,
        string? notes = null,
        string actorUserId = "")
    {
        ValidateOffer(initialOfferPrice, nameof(initialOfferPrice));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be a positive integer.");
        RequireText(productType, nameof(productType), 100);
        RequireText(materialDescription, nameof(materialDescription), 2000);
        RequireLength(designSpecification, nameof(designSpecification), 2000);
        RequireLength(attachmentPath, nameof(attachmentPath), 500);
        RequireLength(notes, nameof(notes), 1000);
        if (string.IsNullOrWhiteSpace(actorUserId)) throw new ArgumentException("An authenticated Brand Owner is required.", nameof(actorUserId));

        var brand = await _context.Organizations.SingleOrDefaultAsync(o => o.Id == brandOrgId);
        if (brand is null || brand.Type != OrganizationType.Brand || brand.OwnerUserId != actorUserId)
            throw new InvalidOperationException("The signed-in user does not own the submitting Brand organization.");
        if (brand.VerificationStatus != VerificationStatus.Verified)
            throw new InvalidOperationException("The Brand organization must be Verified before submitting a Manufacturing Request.");

        var factory = await _context.Organizations.SingleOrDefaultAsync(o => o.Id == factoryOrgId);
        if (factory is null || factory.Type != OrganizationType.Factory || factory.VerificationStatus != VerificationStatus.Verified)
            throw new InvalidOperationException("Choose an existing Verified factory.");

        var request = new ManufacturingRequest
        {
            BrandOrganizationId = brand.Id,
            FactoryOrganizationId = factory.Id,
            ProductType = productType.Trim(),
            DesignSpecification = designSpecification?.Trim() ?? string.Empty,
            MaterialDescription = materialDescription.Trim(),
            Quantity = quantity,
            CurrentOfferPrice = initialOfferPrice,
            AttachmentPath = NullIfWhiteSpace(attachmentPath),
            Notes = NullIfWhiteSpace(notes),
            Status = ManufacturingRequestStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow,
            Offers =
            {
                new RequestOffer
                {
                    OfferedByOrganizationId = brand.Id,
                    UnitPrice = initialOfferPrice,
                    Note = NullIfWhiteSpace(notes),
                    RoundNumber = 1,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.ManufacturingRequests.Add(request);
        await _context.SaveChangesAsync();
        await _statusHistory.LogHistoryAsync(
            StatusRecordType.Request,
            request.Id,
            string.Empty,
            request.Status.ToString(),
            actorUserId,
            "Manufacturing Request submitted.");
        await _notifications.CreateNotificationAsync(
            factory.OwnerUserId,
            $"A Brand Owner sent you Manufacturing Request #{request.Id}.",
            $"/ManufacturingRequests/Details/{request.Id}");
        await transaction.CommitAsync();
        return request;
    }

    public Task<ManufacturingRequest?> GetByIdAsync(int requestId) =>
        _context.ManufacturingRequests
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.BrandOrganization)
            .Include(r => r.FactoryOrganization)
            .Include(r => r.Offers.OrderBy(o => o.RoundNumber))
                .ThenInclude(o => o.OfferedByOrganization)
            .SingleOrDefaultAsync(r => r.Id == requestId);

    public async Task<IEnumerable<ManufacturingRequest>> GetRequestsByBrandAsync(int brandOrgId, ManufacturingRequestStatus? status = null)
    {
        var query = _context.ManufacturingRequests
            .AsNoTracking()
            .Include(r => r.FactoryOrganization)
            .Where(r => r.BrandOrganizationId == brandOrgId);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<ManufacturingRequest>> GetRequestsByFactoryAsync(int factoryOrgId, ManufacturingRequestStatus? status = null)
    {
        var query = _context.ManufacturingRequests
            .AsNoTracking()
            .Include(r => r.BrandOrganization)
            .Where(r => r.FactoryOrganizationId == factoryOrgId);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<bool> AcceptRequestAsync(int requestId, int organizationId, string actorUserId, string? note = null)
    {
        var request = await LoadForTransitionAsync(requestId);
        if (request is null || !IsAllowedDecisionActor(request, organizationId, actorUserId)) return false;

        var isFactoryAccepting = request.Status == ManufacturingRequestStatus.PendingApproval && organizationId == request.FactoryOrganizationId;
        var isBrandAccepting = request.Status == ManufacturingRequestStatus.CounterOffered && organizationId == request.BrandOrganizationId;
        if (!isFactoryAccepting && !isBrandAccepting) return false;

        var recipient = isFactoryAccepting ? request.BrandOrganization.OwnerUserId : request.FactoryOrganization.OwnerUserId;
        return await CompleteTransitionAsync(request, ManufacturingRequestStatus.Accepted, actorUserId, recipient, note);
    }

    public async Task<bool> RejectRequestAsync(int requestId, int organizationId, string actorUserId, string? note = null)
    {
        var request = await LoadForTransitionAsync(requestId);
        if (request is null || !IsAllowedDecisionActor(request, organizationId, actorUserId)) return false;

        var isFactoryRejecting = request.Status == ManufacturingRequestStatus.PendingApproval && organizationId == request.FactoryOrganizationId;
        var isBrandRejecting = request.Status == ManufacturingRequestStatus.CounterOffered && organizationId == request.BrandOrganizationId;
        if (!isFactoryRejecting && !isBrandRejecting) return false;

        var recipient = isFactoryRejecting ? request.BrandOrganization.OwnerUserId : request.FactoryOrganization.OwnerUserId;
        return await CompleteTransitionAsync(request, ManufacturingRequestStatus.Rejected, actorUserId, recipient, note);
    }

    public async Task<bool> CounterOfferAsync(int requestId, int factoryOrgId, string actorUserId, decimal counterOfferPrice, string note)
    {
        ValidateOffer(counterOfferPrice, nameof(counterOfferPrice));
        RequireText(note, nameof(note), 500);
        var request = await LoadForTransitionAsync(requestId);
        if (request is null || request.Status != ManufacturingRequestStatus.PendingApproval ||
            factoryOrgId != request.FactoryOrganizationId || !IsAllowedDecisionActor(request, factoryOrgId, actorUserId)) return false;

        var round = request.Offers.Count == 0 ? 1 : request.Offers.Max(o => o.RoundNumber) + 1;
        request.Offers.Add(new RequestOffer
        {
            OfferedByOrganizationId = factoryOrgId,
            UnitPrice = counterOfferPrice,
            Note = note.Trim(),
            RoundNumber = round,
            CreatedAt = DateTime.UtcNow
        });
        request.CurrentOfferPrice = counterOfferPrice;
        return await CompleteTransitionAsync(request, ManufacturingRequestStatus.CounterOffered, actorUserId,
            request.BrandOrganization.OwnerUserId, note);
    }

    public async Task<bool> ReviseOfferAsync(int requestId, int brandOrgId, string actorUserId, decimal revisedPrice, string note)
    {
        ValidateOffer(revisedPrice, nameof(revisedPrice));
        RequireText(note, nameof(note), 500);
        var request = await LoadForTransitionAsync(requestId);
        if (request is null || request.Status != ManufacturingRequestStatus.CounterOffered ||
            brandOrgId != request.BrandOrganizationId || !IsAllowedDecisionActor(request, brandOrgId, actorUserId)) return false;

        var round = request.Offers.Count == 0 ? 1 : request.Offers.Max(o => o.RoundNumber) + 1;
        request.Offers.Add(new RequestOffer
        {
            OfferedByOrganizationId = brandOrgId,
            UnitPrice = revisedPrice,
            Note = note.Trim(),
            RoundNumber = round,
            CreatedAt = DateTime.UtcNow
        });
        request.CurrentOfferPrice = revisedPrice;
        return await CompleteTransitionAsync(request, ManufacturingRequestStatus.PendingApproval, actorUserId,
            request.FactoryOrganization.OwnerUserId, note);
    }

    public async Task<bool> CancelRequestAsync(int requestId, int brandOrgId, string actorUserId)
    {
        var request = await LoadForTransitionAsync(requestId);
        if (request is null || (request.Status != ManufacturingRequestStatus.PendingApproval && request.Status != ManufacturingRequestStatus.CounterOffered) ||
            brandOrgId != request.BrandOrganizationId || !IsAllowedDecisionActor(request, brandOrgId, actorUserId)) return false;

        return await CompleteTransitionAsync(request, ManufacturingRequestStatus.Cancelled, actorUserId,
            request.FactoryOrganization.OwnerUserId, "Manufacturing Request cancelled by its Brand Owner.");
    }

    public async Task<IEnumerable<RequestOffer>> GetOfferHistoryAsync(int requestId) =>
        await _context.RequestOffers
            .AsNoTracking()
            .Include(o => o.OfferedByOrganization)
            .Where(o => o.ManufacturingRequestId == requestId)
            .OrderBy(o => o.RoundNumber)
            .ThenBy(o => o.CreatedAt)
            .ToListAsync();

    private Task<ManufacturingRequest?> LoadForTransitionAsync(int requestId) =>
        _context.ManufacturingRequests
            .AsSplitQuery()
            .Include(r => r.BrandOrganization)
            .Include(r => r.FactoryOrganization)
            .Include(r => r.Offers)
            .SingleOrDefaultAsync(r => r.Id == requestId);

    private static bool IsAllowedDecisionActor(ManufacturingRequest request, int organizationId, string actorUserId) =>
        !string.IsNullOrWhiteSpace(actorUserId) &&
        ((organizationId == request.BrandOrganizationId && request.BrandOrganization.OwnerUserId == actorUserId && request.BrandOrganization.Type == OrganizationType.Brand) ||
         (organizationId == request.FactoryOrganizationId && request.FactoryOrganization.OwnerUserId == actorUserId && request.FactoryOrganization.Type == OrganizationType.Factory));

    private async Task<bool> CompleteTransitionAsync(
        ManufacturingRequest request,
        ManufacturingRequestStatus targetStatus,
        string actorUserId,
        string recipientUserId,
        string? note)
    {
        var previousStatus = request.Status;
        request.Status = targetStatus;
        request.DecisionNote = NullIfWhiteSpace(note);
        request.UpdatedAt = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.SaveChangesAsync();
        await _statusHistory.LogHistoryAsync(StatusRecordType.Request, request.Id,
            previousStatus.ToString(), targetStatus.ToString(), actorUserId, NullIfWhiteSpace(note));
        await _notifications.CreateNotificationAsync(recipientUserId,
            $"Manufacturing Request #{request.Id} was updated to {targetStatus}.",
            $"/ManufacturingRequests/Details/{request.Id}");
        await transaction.CommitAsync();
        return true;
    }

    private static void ValidateOffer(decimal price, string parameterName)
    {
        if (price <= 0) throw new ArgumentOutOfRangeException(parameterName, "Offer price per unit must be greater than zero.");
    }

    private static void RequireText(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A value is required.", parameterName);
        RequireLength(value, parameterName, maxLength);
    }

    private static void RequireLength(string? value, string parameterName, int maxLength)
    {
        if (value?.Length > maxLength) throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
