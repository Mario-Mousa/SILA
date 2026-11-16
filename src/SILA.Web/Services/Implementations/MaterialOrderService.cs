// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.2 | Owner: P1 | Status: DONE
// What exists: Implemented MaterialOrderService with single-supplier validation, line snapshots, atomic stock deduction, and strict state transitions
// What is left: // TODO(T3.4): Wire order listings into unified Sent/Received tabs and performance dashboards
// Depends on:  T1.1 (MaterialOrder, MaterialOrderLine, StockMovement, MaterialListing), T1.2 (IMaterialOrderService), T2.4 (IStatusHistoryService, INotificationService)
// Used by:     OrdersController, Views/Orders/*
// How to test: Place order with positive quantities -> verify snapshots and pending status;
//              Confirm order -> verify atomic stock deduction, StockMovement rows, and RowVersion concurrency;
//              Verify 15 + 15 against 20 stock test case (exactly one succeeds);
//              Reject with reason -> verify status history and notification.
// Notes:       BR-01 (positive qty/stock >= 0), BR-02 (single supplier partition), BR-03 (atomic stock deduction in transaction),
//              BR-04 (immutable line snapshots), BR-08 (immutable audit history), BR-09 (stock movements), BR-10 (notifications), BR-11 (rollback on failure)
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

/// <summary>
/// Service implementation managing the Material Order lifecycle, atomic inventory deductions,
/// immutable price/item snapshots, and state machine audit history.
/// </summary>
public class MaterialOrderService : IMaterialOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IStatusHistoryService _statusHistoryService;
    private readonly INotificationService _notificationService;

    public MaterialOrderService(
        ApplicationDbContext context,
        IStatusHistoryService statusHistoryService,
        INotificationService notificationService)
    {
        _context = context;
        _statusHistoryService = statusHistoryService;
        _notificationService = notificationService;
    }

    /// <inheritdoc />
    public async Task<MaterialOrder> CreateOrderAsync(
        int buyerOrgId,
        int supplierOrgId,
        IEnumerable<(int ListingId, int Quantity)> items)
    {
        if (buyerOrgId <= 0)
        {
            throw new ArgumentException("Buyer organization ID must be a positive integer.", nameof(buyerOrgId));
        }

        if (supplierOrgId <= 0)
        {
            throw new ArgumentException("Supplier organization ID must be a positive integer.", nameof(supplierOrgId));
        }

        if (buyerOrgId == supplierOrgId)
        {
            throw new InvalidOperationException("An organization cannot place a material order with itself.");
        }

        if (items == null)
        {
            throw new ArgumentNullException(nameof(items), "Order items list cannot be null.");
        }

        // Aggregate quantities for duplicate listings if any
        var itemList = items
            .GroupBy(i => i.ListingId)
            .Select(g => (ListingId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .ToList();

        if (itemList.Count == 0)
        {
            throw new ArgumentException("An order must contain at least one material listing.", nameof(items));
        }

        // BR-01: Quantities are positive whole numbers
        if (itemList.Any(i => i.Quantity <= 0))
        {
            throw new ArgumentException("All item quantities must be positive integers greater than zero (BR-01).", nameof(items));
        }

        var listingIds = itemList.Select(i => i.ListingId).ToList();
        var listings = await _context.MaterialListings
            .Include(l => l.SupplierOrganization)
            .Where(l => listingIds.Contains(l.Id))
            .ToListAsync();

        if (listings.Count != listingIds.Count)
        {
            throw new InvalidOperationException("One or more selected material listings could not be found.");
        }

        // BR-02: An order contains lines from one Supplier only
        if (listings.Any(l => l.SupplierOrganizationId != supplierOrgId))
        {
            throw new InvalidOperationException("Orders may contain materials from one supplier only (BR-02).");
        }

        // BR-05: Deactivated listings are blocked for new orders
        if (listings.Any(l => !l.IsActive))
        {
            throw new InvalidOperationException("One or more selected listings are deactivated and cannot be ordered (BR-05).");
        }

        // Validate stock availability and unit price
        foreach (var item in itemList)
        {
            var listing = listings.First(l => l.Id == item.ListingId);

            // BR-06: Out of stock indicator at 0
            if (listing.AvailableQuantity <= 0)
            {
                throw new InvalidOperationException($"Material '{listing.Name}' is currently out of stock and cannot be ordered (BR-06).");
            }

            if (item.Quantity > listing.AvailableQuantity)
            {
                throw new InvalidOperationException($"Requested quantity ({item.Quantity}) for '{listing.Name}' exceeds available stock ({listing.AvailableQuantity} {listing.Unit}).");
            }

            // BR-01: Unit price > 0
            if (listing.UnitPrice <= 0)
            {
                throw new InvalidOperationException($"Material listing '{listing.Name}' has an invalid unit price (BR-01).");
            }
        }

        // BR-04: Each order line stores an immutable snapshot of name, unit, and unit price
        var orderLines = new List<MaterialOrderLine>();
        decimal totalAmount = 0m;

        foreach (var item in itemList)
        {
            var listing = listings.First(l => l.Id == item.ListingId);
            var lineTotal = item.Quantity * listing.UnitPrice;
            totalAmount += lineTotal;

            orderLines.Add(new MaterialOrderLine
            {
                MaterialListingId = listing.Id,
                MaterialNameSnapshot = listing.Name,
                UnitSnapshot = listing.Unit,
                UnitPriceSnapshot = listing.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = lineTotal,
                CreatedAt = DateTime.UtcNow
            });
        }

        var order = new MaterialOrder
        {
            BuyerOrganizationId = buyerOrgId,
            SupplierOrganizationId = supplierOrgId,
            Status = MaterialOrderStatus.PendingReview,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow,
            Lines = orderLines
        };

        _context.MaterialOrders.Add(order);
        await _context.SaveChangesAsync();

        // Audit Trail: Log initial status transition (BR-08)
        var buyerOrg = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == buyerOrgId);
        if (buyerOrg != null && !string.IsNullOrWhiteSpace(buyerOrg.OwnerUserId))
        {
            await _statusHistoryService.LogHistoryAsync(
                StatusRecordType.Order,
                order.Id,
                "New",
                nameof(MaterialOrderStatus.PendingReview),
                buyerOrg.OwnerUserId,
                "Order created and submitted for supplier review.");
        }

        // In-App Notification: Notify Supplier of incoming order (BR-10)
        var supplierOrg = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == supplierOrgId);
        if (supplierOrg != null && !string.IsNullOrWhiteSpace(supplierOrg.OwnerUserId))
        {
            var buyerName = buyerOrg?.Name ?? "A buyer";
            await _notificationService.CreateNotificationAsync(
                supplierOrg.OwnerUserId,
                $"New Material Order #{order.Id} submitted by {buyerName} for {order.TotalAmount:N2} EGP.",
                $"/Orders/Details/{order.Id}");
        }

        return order;
    }

    /// <inheritdoc />
    public async Task<MaterialOrder?> GetByIdAsync(int orderId)
    {
        if (orderId <= 0)
        {
            return null;
        }

        return await _context.MaterialOrders
            .AsNoTracking()
            .Include(o => o.BuyerOrganization)
            .Include(o => o.SupplierOrganization)
            .Include(o => o.Lines)
                .ThenInclude(l => l.MaterialListing)
            .Include(o => o.StockMovements)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MaterialOrder>> GetOrdersByBuyerAsync(int buyerOrgId, MaterialOrderStatus? status = null)
    {
        if (buyerOrgId <= 0)
        {
            return Enumerable.Empty<MaterialOrder>();
        }

        var query = _context.MaterialOrders
            .AsNoTracking()
            .Include(o => o.SupplierOrganization)
            .Include(o => o.Lines)
            .Where(o => o.BuyerOrganizationId == buyerOrgId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MaterialOrder>> GetOrdersBySupplierAsync(int supplierOrgId, MaterialOrderStatus? status = null)
    {
        if (supplierOrgId <= 0)
        {
            return Enumerable.Empty<MaterialOrder>();
        }

        var query = _context.MaterialOrders
            .AsNoTracking()
            .Include(o => o.BuyerOrganization)
            .Include(o => o.Lines)
            .Where(o => o.SupplierOrganizationId == supplierOrgId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmOrderAsync(int orderId, int supplierOrgId, string actorUserId)
    {
        if (orderId <= 0 || supplierOrgId <= 0 || string.IsNullOrWhiteSpace(actorUserId))
        {
            return false;
        }

        // BR-03: Stock check + stock deduction + status change on confirmation happen in ONE database transaction.
        // Handled via ExecutionStrategy to support resilient retry policies.
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.MaterialOrders
                    .Include(o => o.Lines)
                    .Include(o => o.BuyerOrganization)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null || order.SupplierOrganizationId != supplierOrgId)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // BR-11: Strict state machine transition: must be PendingReview
                if (order.Status != MaterialOrderStatus.PendingReview)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                var listingIds = order.Lines.Select(l => l.MaterialListingId).Distinct().ToList();

                // Fetch listings with tracking to check AvailableQuantity and test RowVersion concurrency token
                var listings = await _context.MaterialListings
                    .Where(l => listingIds.Contains(l.Id))
                    .ToDictionaryAsync(l => l.Id);

                // Step 1: Re-verify stock for EVERY line at the exact moment of confirmation (BR-03)
                foreach (var line in order.Lines)
                {
                    if (!listings.TryGetValue(line.MaterialListingId, out var listing))
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    if (listing.AvailableQuantity < line.Quantity)
                    {
                        // Insufficient stock: fail atomically and rollback (TC-CONC-01)
                        await transaction.RollbackAsync();
                        return false;
                    }
                }

                // Step 2: Deduct quantity and record StockMovement rows (BR-01, BR-09)
                foreach (var line in order.Lines)
                {
                    var listing = listings[line.MaterialListingId];
                    listing.AvailableQuantity -= line.Quantity;
                    listing.UpdatedAt = DateTime.UtcNow;

                    if (listing.AvailableQuantity < 0)
                    {
                        // Stock must NEVER drop below 0
                        await transaction.RollbackAsync();
                        return false;
                    }

                    var stockMovement = new StockMovement
                    {
                        MaterialListingId = listing.Id,
                        MaterialOrderId = order.Id,
                        QuantityChange = -line.Quantity,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StockMovements.Add(stockMovement);
                }

                // Step 3: Transition status to Confirmed
                var previousStatus = order.Status.ToString();
                order.Status = MaterialOrderStatus.Confirmed;
                order.UpdatedAt = DateTime.UtcNow;

                // Save changes: EF Core validates RowVersion concurrency token on MaterialListings
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Step 4: Post-commit audit trail & notification (BR-08, BR-10)
                await _statusHistoryService.LogHistoryAsync(
                    StatusRecordType.Order,
                    order.Id,
                    previousStatus,
                    nameof(MaterialOrderStatus.Confirmed),
                    actorUserId,
                    "Order confirmed by supplier. Inventory deducted atomically.");

                var buyerUserId = order.BuyerOrganization?.OwnerUserId
                    ?? await _context.Organizations.Where(o => o.Id == order.BuyerOrganizationId).Select(o => o.OwnerUserId).FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(buyerUserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        buyerUserId,
                        $"Your Material Order #{order.Id} has been confirmed by the supplier.",
                        $"/Orders/Details/{order.Id}");
                }

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency conflict occurred on RowVersion: rollback and report failure (TC-CONC-01)
                await transaction.RollbackAsync();
                return false;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    /// <inheritdoc />
    public async Task<bool> RejectOrderAsync(int orderId, int supplierOrgId, string actorUserId, string reason)
    {
        if (orderId <= 0 || supplierOrgId <= 0 || string.IsNullOrWhiteSpace(actorUserId))
        {
            return false;
        }

        // Part 4.4 & Requirements: Mandatory rejection reason note
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        var order = await _context.MaterialOrders
            .Include(o => o.BuyerOrganization)
            .Include(o => o.SupplierOrganization)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.SupplierOrganizationId != supplierOrgId)
        {
            return false;
        }

        // Part 4.4: Only PendingReview can transition to Rejected
        if (order.Status != MaterialOrderStatus.PendingReview)
        {
            return false;
        }

        var previousStatus = order.Status.ToString();
        order.Status = MaterialOrderStatus.Rejected;
        order.RejectionReason = reason.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // BR-08: Log StatusHistory
        await _statusHistoryService.LogHistoryAsync(
            StatusRecordType.Order,
            order.Id,
            previousStatus,
            nameof(MaterialOrderStatus.Rejected),
            actorUserId,
            $"Rejection reason: {reason.Trim()}");

        // BR-10: Notify Buyer
        var buyerUserId = order.BuyerOrganization?.OwnerUserId
            ?? await _context.Organizations.Where(o => o.Id == order.BuyerOrganizationId).Select(o => o.OwnerUserId).FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(buyerUserId))
        {
            var supplierName = order.SupplierOrganization?.Name ?? "The supplier";
            await _notificationService.CreateNotificationAsync(
                buyerUserId,
                $"Your Material Order #{order.Id} was rejected by {supplierName}. Reason: {reason.Trim()}",
                $"/Orders/Details/{order.Id}");
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DispatchOrderAsync(int orderId, int supplierOrgId, string actorUserId)
    {
        if (orderId <= 0 || supplierOrgId <= 0 || string.IsNullOrWhiteSpace(actorUserId))
        {
            return false;
        }

        var order = await _context.MaterialOrders
            .Include(o => o.BuyerOrganization)
            .Include(o => o.SupplierOrganization)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.SupplierOrganizationId != supplierOrgId)
        {
            return false;
        }

        // Part 4.4: Only Confirmed can transition to Dispatched
        if (order.Status != MaterialOrderStatus.Confirmed)
        {
            return false;
        }

        var previousStatus = order.Status.ToString();
        order.Status = MaterialOrderStatus.Dispatched;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // BR-08: Log StatusHistory
        await _statusHistoryService.LogHistoryAsync(
            StatusRecordType.Order,
            order.Id,
            previousStatus,
            nameof(MaterialOrderStatus.Dispatched),
            actorUserId,
            "Order marked as dispatched by supplier.");

        // BR-10: Notify Buyer
        var buyerUserId = order.BuyerOrganization?.OwnerUserId
            ?? await _context.Organizations.Where(o => o.Id == order.BuyerOrganizationId).Select(o => o.OwnerUserId).FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(buyerUserId))
        {
            var supplierName = order.SupplierOrganization?.Name ?? "The supplier";
            await _notificationService.CreateNotificationAsync(
                buyerUserId,
                $"Your Material Order #{order.Id} has been dispatched by {supplierName}.",
                $"/Orders/Details/{order.Id}");
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeliverOrderAsync(int orderId, int supplierOrgId, string actorUserId)
    {
        if (orderId <= 0 || supplierOrgId <= 0 || string.IsNullOrWhiteSpace(actorUserId))
        {
            return false;
        }

        var order = await _context.MaterialOrders
            .Include(o => o.BuyerOrganization)
            .Include(o => o.SupplierOrganization)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.SupplierOrganizationId != supplierOrgId)
        {
            return false;
        }

        // Part 4.4: Only Dispatched can transition to Delivered
        if (order.Status != MaterialOrderStatus.Dispatched)
        {
            return false;
        }

        var previousStatus = order.Status.ToString();
        order.Status = MaterialOrderStatus.Delivered;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // BR-08: Log StatusHistory
        await _statusHistoryService.LogHistoryAsync(
            StatusRecordType.Order,
            order.Id,
            previousStatus,
            nameof(MaterialOrderStatus.Delivered),
            actorUserId,
            "Order marked as delivered by supplier.");

        // BR-10: Notify Buyer
        var buyerUserId = order.BuyerOrganization?.OwnerUserId
            ?? await _context.Organizations.Where(o => o.Id == order.BuyerOrganizationId).Select(o => o.OwnerUserId).FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(buyerUserId))
        {
            var supplierName = order.SupplierOrganization?.Name ?? "The supplier";
            await _notificationService.CreateNotificationAsync(
                buyerUserId,
                $"Your Material Order #{order.Id} has been delivered by {supplierName}.",
                $"/Orders/Details/{order.Id}");
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CancelOrderAsync(int orderId, int buyerOrgId, string actorUserId)
    {
        if (orderId <= 0 || buyerOrgId <= 0 || string.IsNullOrWhiteSpace(actorUserId))
        {
            return false;
        }

        var order = await _context.MaterialOrders
            .Include(o => o.BuyerOrganization)
            .Include(o => o.SupplierOrganization)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.BuyerOrganizationId != buyerOrgId)
        {
            return false;
        }

        // Part 4.4: Buyer can cancel ONLY while PendingReview. Confirmed orders cannot be cancelled.
        if (order.Status != MaterialOrderStatus.PendingReview)
        {
            return false;
        }

        var previousStatus = order.Status.ToString();
        order.Status = MaterialOrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // BR-08: Log StatusHistory
        await _statusHistoryService.LogHistoryAsync(
            StatusRecordType.Order,
            order.Id,
            previousStatus,
            nameof(MaterialOrderStatus.Cancelled),
            actorUserId,
            "Order cancelled by buyer while pending review.");

        // BR-10: Notify Supplier
        var supplierUserId = order.SupplierOrganization?.OwnerUserId
            ?? await _context.Organizations.Where(o => o.Id == order.SupplierOrganizationId).Select(o => o.OwnerUserId).FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(supplierUserId))
        {
            var buyerName = order.BuyerOrganization?.Name ?? "The buyer";
            await _notificationService.CreateNotificationAsync(
                supplierUserId,
                $"Material Order #{order.Id} was cancelled by {buyerName}.",
                $"/Orders/Details/{order.Id}");
        }

        return true;
    }
}
