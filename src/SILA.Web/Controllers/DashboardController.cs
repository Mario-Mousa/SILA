// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.4 | Owner: P4 | Status: DONE
// What exists: Live role dashboards (Brand, Factory, Supplier) with real metrics, recent activity,
//               and the unified Sent/Received Hub combining workflows visually while keeping data strictly separate
// What is left: none
// Depends on:  T1.1 (Organization, ManufacturingRequest, MaterialOrder), T2.2 (IOrganizationService),
//              T2.4 (INotificationService, IOwnershipService), T3.1, T3.2, T3.3
// Used by:     All authenticated roles (Brand, Factory, Supplier)
// How to test: Sign in as Brand/Factory/Supplier; visit /Dashboard and /Requests; verify metrics, live widgets, and Sent/Received tabs
// Notes:       BR-07: Queries are filtered strictly by user's OrganizationId. Dual workflows are segregated in data model.
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.Dashboard;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrganizationService _organizationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IOrganizationService organizationService,
        INotificationService notificationService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _organizationService = organizationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the role-specific live dashboard (Brand, Factory, Supplier) with real-time metrics.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Admin");
        }

        var organization = await _organizationService.GetByUserIdAsync(user.Id);
        var unreadNotifications = await _notificationService.GetUnreadCountAsync(user.Id);

        if (organization == null)
        {
            return View("Index", null);
        }

        var orgId = organization.Id;

        if (User.IsInRole("Brand"))
        {
            var brandModel = await BuildBrandDashboardAsync(organization, user, unreadNotifications);
            return View("Brand", brandModel);
        }

        if (User.IsInRole("Factory"))
        {
            var factoryModel = await BuildFactoryDashboardAsync(organization, user, unreadNotifications);
            return View("Factory", factoryModel);
        }

        if (User.IsInRole("Supplier"))
        {
            var supplierModel = await BuildSupplierDashboardAsync(organization, user, unreadNotifications);
            return View("Supplier", supplierModel);
        }

        return View("Index", organization);
    }

    /// <summary>
    /// Unified Sent / Received Hub displaying the two separate workflows (Manufacturing Requests & Material Orders)
    /// separated into "Sent" and "Received" tabs according to the user's role.
    /// Accessible via /Requests and /Dashboard/Requests.
    /// </summary>
    [HttpGet]
    [Route("Requests")]
    [Route("Requests/Index")]
    [Route("Dashboard/Requests")]
    public async Task<IActionResult> Requests(string tab = "sent")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var organization = await _organizationService.GetByUserIdAsync(user.Id);
        if (organization == null)
        {
            return RedirectToAction("Index", "Profile");
        }

        var orgId = organization.Id;
        var activeTab = string.Equals(tab, "received", StringComparison.OrdinalIgnoreCase) ? "received" : "sent";

        var hubModel = new SentReceivedHubViewModel
        {
            ActiveTab = activeTab,
            Organization = organization,
            UserRole = User.IsInRole("Brand") ? "Brand" : (User.IsInRole("Factory") ? "Factory" : "Supplier")
        };

        if (User.IsInRole("Brand"))
        {
            // Brand: Sent = Requests sent to factories + Material orders placed with suppliers
            var sentRequests = await _context.ManufacturingRequests
                .AsNoTracking()
                .Include(r => r.FactoryOrganization)
                .Where(r => r.BrandOrganizationId == orgId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SentReceivedItemViewModel
                {
                    Id = r.Id,
                    WorkflowType = "ManufacturingRequest",
                    Title = r.ProductType,
                    CounterpartyRole = "Factory",
                    CounterpartyName = r.FactoryOrganization.Name,
                    CreatedAt = r.CreatedAt,
                    Status = r.Status.ToString(),
                    FormattedAmount = $"{r.CurrentOfferPrice:N2} EGP / unit ({r.Quantity} units)",
                    DetailsUrl = $"/ManufacturingRequests/Details/{r.Id}",
                    RequiresAction = r.Status == ManufacturingRequestStatus.CounterOffered,
                    ActionPrompt = r.Status == ManufacturingRequestStatus.CounterOffered ? "Counter-Offer Received: Review & Respond" : null
                })
                .ToListAsync();

            var sentOrders = await _context.MaterialOrders
                .AsNoTracking()
                .Include(o => o.SupplierOrganization)
                .Where(o => o.BuyerOrganizationId == orgId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SentReceivedItemViewModel
                {
                    Id = o.Id,
                    WorkflowType = "MaterialOrder",
                    Title = $"Material Order #{o.Id}",
                    CounterpartyRole = "Supplier",
                    CounterpartyName = o.SupplierOrganization.Name,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    FormattedAmount = $"{o.TotalAmount:N2} EGP",
                    DetailsUrl = $"/Orders/Details/{o.Id}",
                    RequiresAction = false
                })
                .ToListAsync();

            hubModel.SentItems = sentRequests.Concat(sentOrders)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            // Brand does not receive initial requests, but receives counter-offers which are highlighted in Sent
            hubModel.ReceivedItems = sentRequests.Where(r => r.RequiresAction).ToList();
        }
        else if (User.IsInRole("Factory"))
        {
            // Factory: Received = Manufacturing requests from brands; Sent = Material orders placed with suppliers
            var receivedRequests = await _context.ManufacturingRequests
                .AsNoTracking()
                .Include(r => r.BrandOrganization)
                .Where(r => r.FactoryOrganizationId == orgId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SentReceivedItemViewModel
                {
                    Id = r.Id,
                    WorkflowType = "ManufacturingRequest",
                    Title = r.ProductType,
                    CounterpartyRole = "Brand",
                    CounterpartyName = r.BrandOrganization.Name,
                    CreatedAt = r.CreatedAt,
                    Status = r.Status.ToString(),
                    FormattedAmount = $"{r.CurrentOfferPrice:N2} EGP / unit ({r.Quantity} units)",
                    DetailsUrl = $"/ManufacturingRequests/Details/{r.Id}",
                    RequiresAction = r.Status == ManufacturingRequestStatus.PendingApproval,
                    ActionPrompt = r.Status == ManufacturingRequestStatus.PendingApproval ? "New Request: Review & Propose Price" : null
                })
                .ToListAsync();

            var sentOrders = await _context.MaterialOrders
                .AsNoTracking()
                .Include(o => o.SupplierOrganization)
                .Where(o => o.BuyerOrganizationId == orgId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SentReceivedItemViewModel
                {
                    Id = o.Id,
                    WorkflowType = "MaterialOrder",
                    Title = $"Material Order #{o.Id}",
                    CounterpartyRole = "Supplier",
                    CounterpartyName = o.SupplierOrganization.Name,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    FormattedAmount = $"{o.TotalAmount:N2} EGP",
                    DetailsUrl = $"/Orders/Details/{o.Id}",
                    RequiresAction = false
                })
                .ToListAsync();

            hubModel.ReceivedItems = receivedRequests;
            hubModel.SentItems = sentOrders;
        }
        else if (User.IsInRole("Supplier"))
        {
            // Supplier: Received = Material orders from buyers
            var receivedOrders = await _context.MaterialOrders
                .AsNoTracking()
                .Include(o => o.BuyerOrganization)
                .Where(o => o.SupplierOrganizationId == orgId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SentReceivedItemViewModel
                {
                    Id = o.Id,
                    WorkflowType = "MaterialOrder",
                    Title = $"Material Order #{o.Id}",
                    CounterpartyRole = "Buyer",
                    CounterpartyName = o.BuyerOrganization.Name,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    FormattedAmount = $"{o.TotalAmount:N2} EGP",
                    DetailsUrl = $"/Orders/Details/{o.Id}",
                    RequiresAction = o.Status == MaterialOrderStatus.PendingReview,
                    ActionPrompt = o.Status == MaterialOrderStatus.PendingReview ? "Pending Stock Confirmation" : null
                })
                .ToListAsync();

            hubModel.ReceivedItems = receivedOrders;
            hubModel.SentItems = new List<SentReceivedItemViewModel>();
        }

        return View("Requests", hubModel);
    }

    #region Private Role Dashboard Builders

    private async Task<RoleDashboardViewModel> BuildBrandDashboardAsync(
        Organization org, ApplicationUser user, int unreadCount)
    {
        var orgId = org.Id;

        // Metric 1: Requests awaiting factory approval
        var pendingRequestsCount = await _context.ManufacturingRequests
            .AsNoTracking()
            .CountAsync(r => r.BrandOrganizationId == orgId && r.Status == ManufacturingRequestStatus.PendingApproval);

        // Metric 2: Counter-offers to review (Action required!)
        var counterOfferedCount = await _context.ManufacturingRequests
            .AsNoTracking()
            .CountAsync(r => r.BrandOrganizationId == orgId && r.Status == ManufacturingRequestStatus.CounterOffered);

        // Metric 3: Active material orders placed
        var activeOrdersCount = await _context.MaterialOrders
            .AsNoTracking()
            .CountAsync(o => o.BuyerOrganizationId == orgId &&
                             (o.Status == MaterialOrderStatus.PendingReview ||
                              o.Status == MaterialOrderStatus.Confirmed ||
                              o.Status == MaterialOrderStatus.Dispatched));

        // Recent 5 activity items (Requests + Orders)
        var recentRequests = await _context.ManufacturingRequests
            .AsNoTracking()
            .Include(r => r.FactoryOrganization)
            .Where(r => r.BrandOrganizationId == orgId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new SentReceivedItemViewModel
            {
                Id = r.Id,
                WorkflowType = "ManufacturingRequest",
                Title = r.ProductType,
                CounterpartyRole = "Factory",
                CounterpartyName = r.FactoryOrganization.Name,
                CreatedAt = r.CreatedAt,
                Status = r.Status.ToString(),
                FormattedAmount = $"{r.CurrentOfferPrice:N2} EGP",
                DetailsUrl = $"/ManufacturingRequests/Details/{r.Id}",
                RequiresAction = r.Status == ManufacturingRequestStatus.CounterOffered
            })
            .ToListAsync();

        var recentOrders = await _context.MaterialOrders
            .AsNoTracking()
            .Include(o => o.SupplierOrganization)
            .Where(o => o.BuyerOrganizationId == orgId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new SentReceivedItemViewModel
            {
                Id = o.Id,
                WorkflowType = "MaterialOrder",
                Title = $"Material Order #{o.Id}",
                CounterpartyRole = "Supplier",
                CounterpartyName = o.SupplierOrganization.Name,
                CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(),
                FormattedAmount = $"{o.TotalAmount:N2} EGP",
                DetailsUrl = $"/Orders/Details/{o.Id}",
                RequiresAction = false
            })
            .ToListAsync();

        var combinedActivity = recentRequests.Concat(recentOrders)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToList();

        return new RoleDashboardViewModel
        {
            Organization = org,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = "Brand",
            Metric1Value = pendingRequestsCount,
            Metric1Label = "Requests Awaiting Factory",
            Metric1Subtext = "Awaiting factory response",
            Metric1StatusBadge = "PendingApproval",
            Metric2Value = counterOfferedCount,
            Metric2Label = "Counter-Offers to Review",
            Metric2Subtext = "Requires your decision",
            Metric2StatusBadge = "CounterOffered",
            Metric3Value = activeOrdersCount,
            Metric3Label = "Active Material Orders",
            Metric3Subtext = "Fabric & trim purchases",
            Metric3StatusBadge = "Active",
            UnreadNotificationsCount = unreadCount,
            RecentActivity = combinedActivity
        };
    }

    private async Task<RoleDashboardViewModel> BuildFactoryDashboardAsync(
        Organization org, ApplicationUser user, int unreadCount)
    {
        var orgId = org.Id;

        // Metric 1: Incoming requests to review
        var pendingReviewCount = await _context.ManufacturingRequests
            .AsNoTracking()
            .CountAsync(r => r.FactoryOrganizationId == orgId && r.Status == ManufacturingRequestStatus.PendingApproval);

        // Metric 2: Open negotiations (counter-offered)
        var openNegotiationsCount = await _context.ManufacturingRequests
            .AsNoTracking()
            .CountAsync(r => r.FactoryOrganizationId == orgId && r.Status == ManufacturingRequestStatus.CounterOffered);

        // Metric 3: Material orders placed as buyer
        var ordersPlacedCount = await _context.MaterialOrders
            .AsNoTracking()
            .CountAsync(o => o.BuyerOrganizationId == orgId &&
                             (o.Status == MaterialOrderStatus.PendingReview ||
                              o.Status == MaterialOrderStatus.Confirmed ||
                              o.Status == MaterialOrderStatus.Dispatched));

        // Recent 5 activity items
        var recentRequests = await _context.ManufacturingRequests
            .AsNoTracking()
            .Include(r => r.BrandOrganization)
            .Where(r => r.FactoryOrganizationId == orgId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new SentReceivedItemViewModel
            {
                Id = r.Id,
                WorkflowType = "ManufacturingRequest",
                Title = r.ProductType,
                CounterpartyRole = "Brand",
                CounterpartyName = r.BrandOrganization.Name,
                CreatedAt = r.CreatedAt,
                Status = r.Status.ToString(),
                FormattedAmount = $"{r.CurrentOfferPrice:N2} EGP",
                DetailsUrl = $"/ManufacturingRequests/Details/{r.Id}",
                RequiresAction = r.Status == ManufacturingRequestStatus.PendingApproval
            })
            .ToListAsync();

        var recentOrders = await _context.MaterialOrders
            .AsNoTracking()
            .Include(o => o.SupplierOrganization)
            .Where(o => o.BuyerOrganizationId == orgId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new SentReceivedItemViewModel
            {
                Id = o.Id,
                WorkflowType = "MaterialOrder",
                Title = $"Material Order #{o.Id}",
                CounterpartyRole = "Supplier",
                CounterpartyName = o.SupplierOrganization.Name,
                CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(),
                FormattedAmount = $"{o.TotalAmount:N2} EGP",
                DetailsUrl = $"/Orders/Details/{o.Id}",
                RequiresAction = false
            })
            .ToListAsync();

        var combinedActivity = recentRequests.Concat(recentOrders)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToList();

        return new RoleDashboardViewModel
        {
            Organization = org,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = "Factory",
            Metric1Value = pendingReviewCount,
            Metric1Label = "Requests to Review",
            Metric1Subtext = "New production proposals",
            Metric1StatusBadge = "PendingApproval",
            Metric2Value = openNegotiationsCount,
            Metric2Label = "Open Negotiations",
            Metric2Subtext = "Counter-offers in discussion",
            Metric2StatusBadge = "CounterOffered",
            Metric3Value = ordersPlacedCount,
            Metric3Label = "Material Orders Placed",
            Metric3Subtext = "Fabric sourcing orders",
            Metric3StatusBadge = "Active",
            UnreadNotificationsCount = unreadCount,
            RecentActivity = combinedActivity
        };
    }

    private async Task<RoleDashboardViewModel> BuildSupplierDashboardAsync(
        Organization org, ApplicationUser user, int unreadCount)
    {
        var orgId = org.Id;

        // Metric 1: Active listings
        var activeListingsCount = await _context.MaterialListings
            .AsNoTracking()
            .CountAsync(l => l.SupplierOrganizationId == orgId && l.IsActive);

        // Metric 2: Orders to review (PendingReview - Action required!)
        var pendingOrdersCount = await _context.MaterialOrders
            .AsNoTracking()
            .CountAsync(o => o.SupplierOrganizationId == orgId && o.Status == MaterialOrderStatus.PendingReview);

        // Metric 3: Confirmed / Dispatched orders
        var confirmedOrdersCount = await _context.MaterialOrders
            .AsNoTracking()
            .CountAsync(o => o.SupplierOrganizationId == orgId &&
                             (o.Status == MaterialOrderStatus.Confirmed ||
                              o.Status == MaterialOrderStatus.Dispatched));

        // Recent 5 activity items (Received Material Orders)
        var recentOrders = await _context.MaterialOrders
            .AsNoTracking()
            .Include(o => o.BuyerOrganization)
            .Where(o => o.SupplierOrganizationId == orgId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new SentReceivedItemViewModel
            {
                Id = o.Id,
                WorkflowType = "MaterialOrder",
                Title = $"Material Order #{o.Id}",
                CounterpartyRole = "Buyer",
                CounterpartyName = o.BuyerOrganization.Name,
                CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(),
                FormattedAmount = $"{o.TotalAmount:N2} EGP",
                DetailsUrl = $"/Orders/Details/{o.Id}",
                RequiresAction = o.Status == MaterialOrderStatus.PendingReview
            })
            .ToListAsync();

        return new RoleDashboardViewModel
        {
            Organization = org,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = "Supplier",
            Metric1Value = activeListingsCount,
            Metric1Label = "Active Material Listings",
            Metric1Subtext = "Published in catalog",
            Metric1StatusBadge = "Catalog",
            Metric2Value = pendingOrdersCount,
            Metric2Label = "Orders to Review",
            Metric2Subtext = "Awaiting stock confirmation",
            Metric2StatusBadge = "PendingReview",
            Metric3Value = confirmedOrdersCount,
            Metric3Label = "Active Fulfillments",
            Metric3Subtext = "Confirmed & in transit",
            Metric3StatusBadge = "Active",
            UnreadNotificationsCount = unreadCount,
            RecentActivity = recentOrders
        };
    }

    #endregion
}
