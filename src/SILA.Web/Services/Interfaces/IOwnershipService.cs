// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for central record-level ownership and authorization checks (BR-07)
// What is left: // TODO(T2.4): Implement OwnershipService verifying user's organization against record owners/addressees
// Depends on:  T1.1 (ApplicationUser, Organization, ManufacturingRequest, MaterialOrder, MaterialListing)
// Used by:     All controllers and workflow services for record-level security
// How to test: Compile project; ensure DI registration compiles
// Notes:       Enforces BR-07: users can only view or modify records belonging to their own organization
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for enforcing record-level authorization and ownership validation (BR-07).
/// </summary>
public interface IOwnershipService
{
    /// <summary>
    /// Retrieves the organization ID linked to the specified ApplicationUser ID.
    /// </summary>
    Task<int?> GetUserOrganizationIdAsync(string userId);

    /// <summary>
    /// Validates whether the user's organization is either the Brand (sender) or Factory (recipient) of a ManufacturingRequest.
    /// </summary>
    Task<bool> CanAccessManufacturingRequestAsync(string userId, int requestId);

    /// <summary>
    /// Validates whether the user's organization is either the Buyer or Supplier of a MaterialOrder.
    /// </summary>
    Task<bool> CanAccessMaterialOrderAsync(string userId, int orderId);

    /// <summary>
    /// Validates whether the user's organization owns and can modify or deactivate a MaterialListing.
    /// </summary>
    Task<bool> CanModifyMaterialListingAsync(string userId, int listingId);

    /// <summary>
    /// Validates whether the user owns the specified organization profile.
    /// </summary>
    Task<bool> CanAccessOrganizationAsync(string userId, int organizationId);
}

