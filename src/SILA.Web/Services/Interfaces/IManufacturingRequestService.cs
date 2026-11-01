// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for Manufacturing Request workflow and negotiation rounds
// What is left: // TODO(T3.3): Implement ManufacturingRequestService with negotiation state machine and offer rounds
// Depends on:  T1.1 (ManufacturingRequest, RequestOffer, ManufacturingRequestStatus), T2.4 (StatusHistory, Notifications)
// Used by:     T3.3, ManufacturingRequestsController
// How to test: Compile project; ensure DI registration compiles
// Notes:       Part 4.3: Strict transition state machine; offers recorded in RequestOffer entity
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for handling Manufacturing Requests, counter-offers, and status transitions.
/// </summary>
public interface IManufacturingRequestService
{
    /// <summary>
    /// Submits a new manufacturing request targeted to a single verified factory (D-01).
    /// </summary>
    Task<ManufacturingRequest> CreateRequestAsync(
        int brandOrgId,
        int factoryOrgId,
        string productType,
        string designSpecification,
        string materialDescription,
        int quantity,
        decimal initialOfferPrice,
        string? attachmentPath = null,
        string? notes = null,
        string actorUserId = "");

    /// <summary>
    /// Gets a manufacturing request with all negotiation offer rounds and details.
    /// </summary>
    Task<ManufacturingRequest?> GetByIdAsync(int requestId);

    /// <summary>
    /// Retrieves manufacturing requests submitted by a brand organization.
    /// </summary>
    Task<IEnumerable<ManufacturingRequest>> GetRequestsByBrandAsync(int brandOrgId, ManufacturingRequestStatus? status = null);

    /// <summary>
    /// Retrieves manufacturing requests targeted to a factory organization.
    /// </summary>
    Task<IEnumerable<ManufacturingRequest>> GetRequestsByFactoryAsync(int factoryOrgId, ManufacturingRequestStatus? status = null);

    /// <summary>
    /// Accepts a request (by factory from PendingApproval, or by brand from CounterOffered).
    /// </summary>
    Task<bool> AcceptRequestAsync(int requestId, int organizationId, string actorUserId, string? note = null);

    /// <summary>
    /// Rejects a request (by factory from PendingApproval, or by brand from CounterOffered).
    /// </summary>
    Task<bool> RejectRequestAsync(int requestId, int organizationId, string actorUserId, string? note = null);

    /// <summary>
    /// Sends a counter-offer from the factory to the brand (transitions status to CounterOffered).
    /// </summary>
    Task<bool> CounterOfferAsync(int requestId, int factoryOrgId, string actorUserId, decimal counterOfferPrice, string note);

    /// <summary>
    /// Sends a revised offer from the brand back to the factory (transitions status back to PendingApproval).
    /// </summary>
    Task<bool> ReviseOfferAsync(int requestId, int brandOrgId, string actorUserId, decimal revisedPrice, string note);

    /// <summary>
    /// Cancels a pending or counter-offered request by the submitting brand (D-06).
    /// </summary>
    Task<bool> CancelRequestAsync(int requestId, int brandOrgId, string actorUserId);

    /// <summary>
    /// Retrieves chronological negotiation history (RequestOffer rounds) for a request.
    /// </summary>
    Task<IEnumerable<RequestOffer>> GetOfferHistoryAsync(int requestId);
}

