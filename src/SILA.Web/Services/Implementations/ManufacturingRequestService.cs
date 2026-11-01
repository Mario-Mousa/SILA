// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for Manufacturing Request lifecycle and offer negotiation
// What is left: // TODO(T3.3): Implement ManufacturingRequestService with negotiation state machine and offer rounds
// Depends on:  T1.1 (ManufacturingRequest, RequestOffer, ManufacturingRequestStatus), T2.4 (StatusHistory, Notifications)
// Used by:     T3.3, ManufacturingRequestsController
// How to test: Build project; stub throws NotImplementedException until implemented in T3.3
// Notes:       P2 owns implementation in T3.3
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class ManufacturingRequestService : IManufacturingRequestService
{
    private readonly ApplicationDbContext _context;

    public ManufacturingRequestService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ManufacturingRequest> CreateRequestAsync(
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
        // TODO(T3.3): Validate target factory exists and is verified, create request and initial RequestOffer round
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.CreateRequestAsync");
    }

    public Task<ManufacturingRequest?> GetByIdAsync(int requestId)
    {
        // TODO(T3.3): Retrieve request with offer history
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.GetByIdAsync");
    }

    public Task<IEnumerable<ManufacturingRequest>> GetRequestsByBrandAsync(int brandOrgId, ManufacturingRequestStatus? status = null)
    {
        // TODO(T3.3): Retrieve requests submitted by brand
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.GetRequestsByBrandAsync");
    }

    public Task<IEnumerable<ManufacturingRequest>> GetRequestsByFactoryAsync(int factoryOrgId, ManufacturingRequestStatus? status = null)
    {
        // TODO(T3.3): Retrieve requests received by factory
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.GetRequestsByFactoryAsync");
    }

    public Task<bool> AcceptRequestAsync(int requestId, int organizationId, string actorUserId, string? note = null)
    {
        // TODO(T3.3): Factory accepts from PendingApproval, or Brand accepts from CounterOffered
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.AcceptRequestAsync");
    }

    public Task<bool> RejectRequestAsync(int requestId, int organizationId, string actorUserId, string? note = null)
    {
        // TODO(T3.3): Factory rejects from PendingApproval, or Brand rejects from CounterOffered
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.RejectRequestAsync");
    }

    public Task<bool> CounterOfferAsync(int requestId, int factoryOrgId, string actorUserId, decimal counterOfferPrice, string note)
    {
        // TODO(T3.3): Factory proposes counter-offer price, creates RequestOffer round, transitions to CounterOffered
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.CounterOfferAsync");
    }

    public Task<bool> ReviseOfferAsync(int requestId, int brandOrgId, string actorUserId, decimal revisedPrice, string note)
    {
        // TODO(T3.3): Brand revises offer, creates RequestOffer round, transitions to PendingApproval
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.ReviseOfferAsync");
    }

    public Task<bool> CancelRequestAsync(int requestId, int brandOrgId, string actorUserId)
    {
        // TODO(T3.3): Brand cancels pending or counter-offered request (D-06)
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.CancelRequestAsync");
    }

    public Task<IEnumerable<RequestOffer>> GetOfferHistoryAsync(int requestId)
    {
        // TODO(T3.3): Retrieve chronological negotiation rounds
        throw new NotImplementedException("TODO(T3.3): Implement ManufacturingRequestService.GetOfferHistoryAsync");
    }
}

