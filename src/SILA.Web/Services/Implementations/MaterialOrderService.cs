// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for Material Order workflow and atomic stock updates
// What is left: // TODO(T3.2): Implement MaterialOrderService with single-supplier check, snapshots, and atomic confirmation
// Depends on:  T1.1 (MaterialOrder, MaterialOrderLine, MaterialOrderStatus, StockMovement), T2.4 (StatusHistory, Notifications)
// Used by:     T3.2, OrdersController
// How to test: Build project; stub throws NotImplementedException until implemented in T3.2
// Notes:       P1 owns implementation in T3.2
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class MaterialOrderService : IMaterialOrderService
{
    private readonly ApplicationDbContext _context;

    public MaterialOrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<MaterialOrder> CreateOrderAsync(
        int buyerOrgId,
        int supplierOrgId,
        IEnumerable<(int ListingId, int Quantity)> items)
    {
        // TODO(T3.2): Validate single supplier (BR-02), positive quantities (BR-01), capture snapshots (BR-04)
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.CreateOrderAsync");
    }

    public Task<MaterialOrder?> GetByIdAsync(int orderId)
    {
        // TODO(T3.2): Retrieve order with lines and status history
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.GetByIdAsync");
    }

    public Task<IEnumerable<MaterialOrder>> GetOrdersByBuyerAsync(int buyerOrgId, MaterialOrderStatus? status = null)
    {
        // TODO(T3.2): Retrieve orders placed by buyer
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.GetOrdersByBuyerAsync");
    }

    public Task<IEnumerable<MaterialOrder>> GetOrdersBySupplierAsync(int supplierOrgId, MaterialOrderStatus? status = null)
    {
        // TODO(T3.2): Retrieve orders received by supplier
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.GetOrdersBySupplierAsync");
    }

    public Task<bool> ConfirmOrderAsync(int orderId, int supplierOrgId, string actorUserId)
    {
        // TODO(T3.2): Execute atomic transaction: verify stock, deduct inventory, record StockMovement, update status, notify buyer (BR-03)
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.ConfirmOrderAsync");
    }

    public Task<bool> RejectOrderAsync(int orderId, int supplierOrgId, string actorUserId, string reason)
    {
        // TODO(T3.2): Transition status to Rejected with reason note, notify buyer
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.RejectOrderAsync");
    }

    public Task<bool> DispatchOrderAsync(int orderId, int supplierOrgId, string actorUserId)
    {
        // TODO(T3.2): Transition status to Dispatched, notify buyer
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.DispatchOrderAsync");
    }

    public Task<bool> DeliverOrderAsync(int orderId, int supplierOrgId, string actorUserId)
    {
        // TODO(T3.2): Transition status to Delivered, notify buyer
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.DeliverOrderAsync");
    }

    public Task<bool> CancelOrderAsync(int orderId, int buyerOrgId, string actorUserId)
    {
        // TODO(T3.2): Cancel pending order by buyer, notify supplier (D-06)
        throw new NotImplementedException("TODO(T3.2): Implement MaterialOrderService.CancelOrderAsync");
    }
}

