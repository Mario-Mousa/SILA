// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.4 | Owner: P4 | Status: DONE
// What exists: HistoryController providing chronological status audit logging and timeline views
// What is left: none
// Depends on:  T1.1 (StatusHistory, StatusRecordType), T2.4 (IStatusHistoryService, IOwnershipService)
// Used by:     Brand, Factory, and Supplier organizations for audit and compliance (BR-07, BR-08)
// How to test: Visit /History; verify historical status transitions are listed and filtered by user's organization
// Notes:       BR-07: Strictly filters history rows so organizations only view audits for their own requests and orders
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.History;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

[Authorize]
public class HistoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrganizationService _organizationService;
    private readonly IOwnershipService _ownershipService;
    private readonly IStatusHistoryService _statusHistoryService;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IOrganizationService organizationService,
        IOwnershipService ownershipService,
        IStatusHistoryService statusHistoryService,
        ILogger<HistoryController> logger)
    {
        _context = context;
        _userManager = userManager;
        _organizationService = organizationService;
        _ownershipService = ownershipService;
        _statusHistoryService = statusHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Displays a comprehensive, chronological status audit log for all requests and orders accessible
    /// to the authenticated user's organization (enforcing BR-07 and BR-08).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string filter = "All", int page = 1)
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
        const int pageSize = 25;
        var currentPage = page > 0 ? page : 1;

        // BR-07: Extract IDs of requests and orders that this organization owns or is addressee of
        var accessibleRequestIds = await _context.ManufacturingRequests
            .AsNoTracking()
            .Where(r => r.BrandOrganizationId == orgId || r.FactoryOrganizationId == orgId)
            .Select(r => r.Id)
            .ToListAsync();

        var accessibleOrderIds = await _context.MaterialOrders
            .AsNoTracking()
            .Where(o => o.BuyerOrganizationId == orgId || o.SupplierOrganizationId == orgId)
            .Select(o => o.Id)
            .ToListAsync();

        var query = _context.StatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h =>
                (h.RecordType == StatusRecordType.Request && accessibleRequestIds.Contains(h.RecordId)) ||
                (h.RecordType == StatusRecordType.Order && accessibleOrderIds.Contains(h.RecordId)));

        if (string.Equals(filter, "Request", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(h => h.RecordType == StatusRecordType.Request);
        }
        else if (string.Equals(filter, "Order", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(h => h.RecordType == StatusRecordType.Order);
        }

        var totalCount = await query.CountAsync();

        var rawEntries = await query
            .OrderByDescending(h => h.ChangedAt)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Hydrate titles and counterparties
        var requestDict = await _context.ManufacturingRequests
            .AsNoTracking()
            .Include(r => r.BrandOrganization)
            .Include(r => r.FactoryOrganization)
            .Where(r => accessibleRequestIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => new
            {
                Title = r.ProductType,
                Counterparty = r.BrandOrganizationId == orgId ? r.FactoryOrganization.Name : r.BrandOrganization.Name,
                Url = $"/ManufacturingRequests/Details/{r.Id}"
            });

        var orderDict = await _context.MaterialOrders
            .AsNoTracking()
            .Include(o => o.BuyerOrganization)
            .Include(o => o.SupplierOrganization)
            .Where(o => accessibleOrderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => new
            {
                Title = $"Material Order #{o.Id}",
                Counterparty = o.BuyerOrganizationId == orgId ? o.SupplierOrganization.Name : o.BuyerOrganization.Name,
                Url = $"/Orders/Details/{o.Id}"
            });

        var viewItems = rawEntries.Select(h =>
        {
            var item = new StatusHistoryItemViewModel
            {
                Id = h.Id,
                RecordType = h.RecordType,
                RecordId = h.RecordId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                ChangedByUserName = h.ChangedByUser?.FullName ?? "System",
                ChangedAt = h.ChangedAt,
                Note = h.Note
            };

            if (h.RecordType == StatusRecordType.Request && requestDict.TryGetValue(h.RecordId, out var reqData))
            {
                item.RecordTitle = reqData.Title;
                item.CounterpartyName = reqData.Counterparty;
                item.DetailsUrl = reqData.Url;
            }
            else if (h.RecordType == StatusRecordType.Order && orderDict.TryGetValue(h.RecordId, out var ordData))
            {
                item.RecordTitle = ordData.Title;
                item.CounterpartyName = ordData.Counterparty;
                item.DetailsUrl = ordData.Url;
            }
            else
            {
                item.RecordTitle = $"{h.RecordType} #{h.RecordId}";
                item.CounterpartyName = "Direct counterparty";
                item.DetailsUrl = h.RecordType == StatusRecordType.Request
                    ? $"/ManufacturingRequests/Details/{h.RecordId}"
                    : $"/Orders/Details/{h.RecordId}";
            }

            return item;
        }).ToList();

        var viewModel = new StatusHistoryIndexViewModel
        {
            Entries = viewItems,
            Filter = filter,
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };

        return View(viewModel);
    }

    /// <summary>
    /// Displays detailed chronological history for a specific request or order after verifying ownership (BR-07).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string recordType, int recordId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var isRequest = string.Equals(recordType, "Request", StringComparison.OrdinalIgnoreCase);

        // BR-07: Verify record-level authorization
        if (isRequest)
        {
            var hasAccess = await _ownershipService.CanAccessManufacturingRequestAsync(user.Id, recordId);
            if (!hasAccess)
            {
                return Forbid();
            }

            return Redirect($"/ManufacturingRequests/Details/{recordId}");
        }
        else
        {
            var hasAccess = await _ownershipService.CanAccessMaterialOrderAsync(user.Id, recordId);
            if (!hasAccess)
            {
                return Forbid();
            }

            return Redirect($"/Orders/Details/{recordId}");
        }
    }
}

