// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for Material Order workflow and atomic stock management
// What is left: // TODO(T3.2): Implement MaterialOrderService with single-supplier check, snapshots, and atomic confirmation
// Depends on:  T1.1 (MaterialOrder, MaterialOrderLine, MaterialOrderStatus, StockMovement), T2.4 (StatusHistory, Notifications)
// Used by:     T3.2, OrdersController
// How to test: Compile project; ensure DI registration compiles
// Notes:       BR-02: Single supplier per order. BR-03: Atomic stock check/deduction on confirmation. BR-04: Line snapshots
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for handling Material Order lifecycles and atomic inventory movements.
/// </summary>
public interface IMaterialOrderService
{
    /// <summary>
    /// Creates a new Material Order with snapshots of listing name, unit, and price (BR-02, BR-04).
    /// </summary>
    Task<MaterialOrder> CreateOrderAsync(
        int buyerOrgId,
        int supplierOrgId,
        IEnumerable<(int ListingId, int Quantity)> items);

    /// <summary>
    /// Gets a Material Order with all child order lines and status audit history.
    /// </summary>
    Task<MaterialOrder?> GetByIdAsync(int orderId);

    /// <summary>
    /// Retrieves orders submitted by a buyer organization, optionally filtered by status.
    /// </summary>
    Task<IEnumerable<MaterialOrder>> GetOrdersByBuyerAsync(int buyerOrgId, MaterialOrderStatus? status = null);

    /// <summary>
    /// Retrieves orders received by a supplier organization, optionally filtered by status.
    /// </summary>
    Task<IEnumerable<MaterialOrder>> GetOrdersBySupplierAsync(int supplierOrgId, MaterialOrderStatus? status = null);

    /// <summary>
    /// Atomically confirms the order, checks stock availability, deducts inventory, records StockMovement, and logs status change (BR-03).
    /// </summary>
    Task<bool> ConfirmOrderAsync(int orderId, int supplierOrgId, string actorUserId);

    /// <summary>
    /// Rejects an order with a mandatory reason note.
    /// </summary>
    Task<bool> RejectOrderAsync(int orderId, int supplierOrgId, string actorUserId, string reason);

    /// <summary>
    /// Marks a confirmed order as dispatched by the supplier.
    /// </summary>
    Task<bool> DispatchOrderAsync(int orderId, int supplierOrgId, string actorUserId);

    /// <summary>
    /// Marks a dispatched order as delivered.
    /// </summary>
    Task<bool> DeliverOrderAsync(int orderId, int supplierOrgId, string actorUserId);

    /// <summary>
    /// Cancels a pending order by the buyer organization (D-06).
    /// </summary>
    Task<bool> CancelOrderAsync(int orderId, int buyerOrgId, string actorUserId);
}

