// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.2 | Owner: P1 | Status: DONE
// What exists: OrdersController handling checkout, order details, supplier review/decision, status progression, and cancellation
// What is left: // TODO(T3.4): Connect with unified Sent/Received requests and orders dashboard
// Depends on:  T1.1 (Entities), T1.2 (IMaterialOrderService, IOwnershipService, IStatusHistoryService), T2.4 (Shared services)
// Used by:     Views/Orders/*, Navigation in _Layout.cshtml
// How to test: Place order as Buyer -> verify Details; Log in as Supplier -> Confirm/Reject order; Check stock deduction (BR-03)
// Notes:       Enforces BR-07 record-level authorization on every action; BR-02 single supplier; BR-03 atomic confirmation
// ═══════════════════════════════════════════════════════

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.Orders;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

/// <summary>
/// Controller managing buyer checkout, order details, supplier confirmation/rejection,
/// and order fulfillment lifecycle progression.
/// </summary>
[Authorize]
public class OrdersController : Controller
{
    private readonly IMaterialOrderService _orderService;
    private readonly IOwnershipService _ownershipService;
    private readonly IStatusHistoryService _statusHistoryService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        IMaterialOrderService orderService,
        IOwnershipService ownershipService,
        IStatusHistoryService statusHistoryService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _ownershipService = ownershipService;
        _statusHistoryService = statusHistoryService;
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Displays a list of orders for the authenticated user's organization.
    /// Suppliers view incoming customer orders; Brands and Factories view their placed purchases.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(MaterialOrderStatus? status = null)
    {
        var userId = GetUserId();
        var userOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);

        if (!userOrgId.HasValue)
        {
            TempData["ErrorMessage"] = "You must have an organization profile to view orders.";
            return RedirectToAction("Index", "Home");
        }

        bool isSupplier = User.IsInRole("Supplier");
        var allOrders = isSupplier
            ? await _orderService.GetOrdersBySupplierAsync(userOrgId.Value)
            : await _orderService.GetOrdersByBuyerAsync(userOrgId.Value);

        var orderList = allOrders.ToList();

        var viewModel = new OrderIndexViewModel
        {
            IsSupplier = isSupplier,
            CurrentStatus = status,
            TotalCount = orderList.Count,
            PendingCount = orderList.Count(o => o.Status == MaterialOrderStatus.PendingReview),
            ConfirmedCount = orderList.Count(o => o.Status == MaterialOrderStatus.Confirmed),
            DispatchedCount = orderList.Count(o => o.Status == MaterialOrderStatus.Dispatched),
            DeliveredCount = orderList.Count(o => o.Status == MaterialOrderStatus.Delivered),
            RejectedCount = orderList.Count(o => o.Status == MaterialOrderStatus.Rejected),
            CancelledCount = orderList.Count(o => o.Status == MaterialOrderStatus.Cancelled),
            Orders = status.HasValue ? orderList.Where(o => o.Status == status.Value).ToList() : orderList
        };

        return View(viewModel);
    }

    /// <summary>
    /// Displays checkout review page for purchasing raw materials.
    /// Accessible only to Buyer roles (Brand Owner or Factory Owner).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Brand,Factory")]
    public async Task<IActionResult> Checkout(int listingId, int quantity = 1)
    {
        if (listingId <= 0)
        {
            return BadRequest();
        }

        var userId = GetUserId();
        var buyerOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        if (!buyerOrgId.HasValue)
        {
            TempData["ErrorMessage"] = "Your account is not linked to an organization profile.";
            return RedirectToAction("Index", "Home");
        }

        var listing = await _context.MaterialListings
            .AsNoTracking()
            .Include(l => l.SupplierOrganization)
            .FirstOrDefaultAsync(l => l.Id == listingId);

        if (listing == null)
        {
            return NotFound();
        }

        if (listing.SupplierOrganizationId == buyerOrgId.Value)
        {
            TempData["ErrorMessage"] = "You cannot purchase raw materials from your own organization.";
            return RedirectToAction("Details", "Materials", new { id = listingId });
        }

        if (!listing.IsActive)
        {
            TempData["ErrorMessage"] = "This listing is deactivated and cannot be ordered (BR-05).";
            return RedirectToAction("Browse", "Materials");
        }

        // BR-06: Out of stock indicator at 0
        if (listing.AvailableQuantity <= 0)
        {
            TempData["ErrorMessage"] = "This material is currently out of stock and cannot be ordered (BR-06).";
            return RedirectToAction("Details", "Materials", new { id = listingId });
        }

        var initialQuantity = quantity > 0 && quantity <= listing.AvailableQuantity ? quantity : 1;

        var viewModel = new OrderCheckoutViewModel
        {
            ListingId = listing.Id,
            ListingName = listing.Name,
            Category = listing.Category,
            Unit = listing.Unit,
            UnitPrice = listing.UnitPrice,
            AvailableQuantity = listing.AvailableQuantity,
            Quantity = initialQuantity,
            SupplierOrgId = listing.SupplierOrganizationId,
            SupplierName = listing.SupplierOrganization.Name,
            SupplierCity = listing.SupplierOrganization.City,
            SupplierPhone = listing.SupplierOrganization.ContactPhone
        };

        return View(viewModel);
    }

    /// <summary>
    /// Submits a new material order from checkout.
    /// Captures price and name snapshots, assigns PendingReview status, and alerts the supplier.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Brand,Factory")]
    public async Task<IActionResult> SubmitCheckout(OrderCheckoutSubmitModel model)
    {
        var userId = GetUserId();
        var buyerOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        if (!buyerOrgId.HasValue)
        {
            TempData["ErrorMessage"] = "Your account is not linked to an organization profile.";
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return await ReloadCheckoutView(model.ListingId, model.Quantity);
        }

        try
        {
            var order = await _orderService.CreateOrderAsync(
                buyerOrgId.Value,
                model.SupplierOrgId,
                new[] { (model.ListingId, model.Quantity) });

            TempData["SuccessMessage"] = $"Material Order #{order.Id} submitted successfully and is pending supplier review.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadCheckoutView(model.ListingId, model.Quantity);
        }
    }

    /// <summary>
    /// Displays complete details of an order including line snapshots, financial totals,
    /// status badges, and chronological audit history (BR-07, BR-08).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var userId = GetUserId();

        // BR-07: Record-level authorization - user's organization must be Buyer or Supplier
        var hasAccess = await _ownershipService.CanAccessMaterialOrderAsync(userId, id);
        if (!hasAccess)
        {
            return Forbid();
        }

        var order = await _orderService.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        var userOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        var history = await _statusHistoryService.GetHistoryAsync(StatusRecordType.Order, id);

        var viewModel = new OrderDetailsViewModel
        {
            Order = order,
            History = history,
            IsBuyer = userOrgId.HasValue && userOrgId.Value == order.BuyerOrganizationId,
            IsSupplier = userOrgId.HasValue && userOrgId.Value == order.SupplierOrganizationId,
            RejectForm = new OrderRejectViewModel { OrderId = id }
        };

        return View(viewModel);
    }

    /// <summary>
    /// Confirms a pending order and atomically deducts inventory using RowVersion concurrency (BR-03).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Supplier")]
    public async Task<IActionResult> Confirm(int id)
    {
        var userId = GetUserId();
        var hasAccess = await _ownershipService.CanAccessMaterialOrderAsync(userId, id);
        if (!hasAccess)
        {
            return Forbid();
        }

        var userOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue)
        {
            return Forbid();
        }

        var success = await _orderService.ConfirmOrderAsync(id, userOrgId.Value, userId);
        if (success)
        {
            TempData["SuccessMessage"] = $"Order #{id} confirmed successfully! Inventory has been atomically deducted (BR-03).";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not confirm order. Insufficient available stock or concurrent modification detected (BR-03).";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Rejects a pending order with a mandatory explanation note.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Supplier")]
    public async Task<IActionResult> Reject(OrderRejectViewModel model)
    {
        var userId = GetUserId();
        var hasAccess = await _ownershipService.CanAccessMaterialOrderAsync(userId, model.OrderId);
        if (!hasAccess)
        {
            return Forbid();
        }

        var userOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            TempData["ErrorMessage"] = "A rejection reason is required (Part 4.4).";
            return RedirectToAction(nameof(Details), new { id = model.OrderId });
        }

        var success = await _orderService.RejectOrderAsync(model.OrderId, userOrgId.Value, userId, model.Reason);
        if (success)
        {
            TempData["SuccessMessage"] = $"Order #{model.OrderId} was rejected.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not reject order. It may have already transitioned to another status.";
        }

        return RedirectToAction(nameof(Details), new { id = model.OrderId });
    }

    /// <summary>
    /// Progresses a confirmed order to Dispatched status.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Supplier")]
    public async Task<IActionResult> Dispatch(int id)
    {
        var userId = GetUserId();
        var hasAccess = await _ownershipService.CanAccessMaterialOrderAsync(userId, id);
        if (!hasAccess)
        {
            return Forbid();
        }

        var userOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue)
        {
            return Forbid();
        }

        var success = await _orderService.DispatchOrderAsync(id, userOrgId.Value, userId);
        if (success)
        {
            TempData["SuccessMessage"] = $"Order #{id} has been marked as Dispatched.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not mark order as dispatched. It must be in Confirmed status.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Progresses a dispatched order to Delivered status.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Supplier")]
    public async Task<IActionResult> Deliver(int id)
    {
        var userId = GetUserId();
        var hasAccess = await _ownershipService.CanAccessMaterialOrderAsync(userId, id);
        if (!hasAccess)
        {
            return Forbid();
        }

        var userOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue)
        {
            return Forbid();
        }

        var success = await _orderService.DeliverOrderAsync(id, userOrgId.Value, userId);
        if (success)
        {
            TempData["SuccessMessage"] = $"Order #{id} has been marked as Delivered.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not mark order as delivered. It must be in Dispatched status.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Allows the buyer to cancel an order strictly while it is in PendingReview status (Part 4.4).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Brand,Factory")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetUserId();
        var hasAccess = await _ownershipService.CanAccessMaterialOrderAsync(userId, id);
        if (!hasAccess)
        {
            return Forbid();
        }

        var userOrgId = await _ownershipService.GetUserOrganizationIdAsync(userId);
        if (!userOrgId.HasValue)
        {
            return Forbid();
        }

        var success = await _orderService.CancelOrderAsync(id, userOrgId.Value, userId);
        if (success)
        {
            TempData["SuccessMessage"] = $"Order #{id} has been cancelled.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not cancel order. Only orders in PendingReview status can be cancelled.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private async Task<IActionResult> ReloadCheckoutView(int listingId, int quantity)
    {
        var listing = await _context.MaterialListings
            .AsNoTracking()
            .Include(l => l.SupplierOrganization)
            .FirstOrDefaultAsync(l => l.Id == listingId);

        if (listing == null)
        {
            return NotFound();
        }

        var viewModel = new OrderCheckoutViewModel
        {
            ListingId = listing.Id,
            ListingName = listing.Name,
            Category = listing.Category,
            Unit = listing.Unit,
            UnitPrice = listing.UnitPrice,
            AvailableQuantity = listing.AvailableQuantity,
            Quantity = quantity > 0 ? quantity : 1,
            SupplierOrgId = listing.SupplierOrganizationId,
            SupplierName = listing.SupplierOrganization.Name,
            SupplierCity = listing.SupplierOrganization.City,
            SupplierPhone = listing.SupplierOrganization.ContactPhone
        };

        return View("Checkout", viewModel);
    }
}
