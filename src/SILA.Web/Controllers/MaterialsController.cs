// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.1 | Owner: P3 | Status: DONE
// What exists: MaterialsController handling public/buyer catalog browsing, search/filter, listing details, and supplier CRUD/deactivation
// What is left: // TODO(T3.2): P1 connects direct "Place Material Order" CTA from Details / Catalog cards
// Depends on:  IMaterialListingService, IOwnershipService, IOrganizationService, UserManager<ApplicationUser>
// Used by:     Views/Materials/*, Navigation in _Layout.cshtml
// How to test:
//   1. Browse /Materials as anonymous / buyer: verify category filtering, keyword search, and 12-item pagination.
//   2. As Supplier: visit /Materials/Manage, create a new listing, edit it, and deactivate it (verify BR-05 & BR-07).
//   3. Attempt to edit another supplier's listing -> verify 403 Forbidden is returned.
// Notes:       BR-01: UnitPrice > 0, AvailableQuantity >= 0. BR-05: Deactivate only (no hard deletes).
//              BR-06: Out of stock at 0. BR-07: Record-level ownership authorization.
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SILA.Web.Models.Entities;
using SILA.Web.Models.ViewModels.Materials;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

public class MaterialsController : Controller
{
    private readonly IMaterialListingService _listingService;
    private readonly IOwnershipService _ownershipService;
    private readonly IOrganizationService _organizationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<MaterialsController> _logger;

    public MaterialsController(
        IMaterialListingService listingService,
        IOwnershipService ownershipService,
        IOrganizationService organizationService,
        UserManager<ApplicationUser> userManager,
        ILogger<MaterialsController> logger)
    {
        _listingService = listingService;
        _ownershipService = ownershipService;
        _organizationService = organizationService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Displays the public & buyer raw materials catalog with keyword search, category filter, and pagination (D-08).
    /// Only active listings from verified supplier organizations are shown.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, string? category = null, int page = 1)
    {
        const int pageSize = 12;
        var currentPage = page > 0 ? page : 1;

        var (items, totalCount) = await _listingService.BrowseActiveListingsAsync(
            search: search,
            category: category,
            page: currentPage,
            pageSize: pageSize);

        var categories = await _listingService.GetCategoriesAsync();

        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (totalPages < 1) totalPages = 1;

        var viewModel = new MaterialCatalogViewModel
        {
            Listings = items,
            SearchTerm = search,
            SelectedCategory = category,
            Categories = categories,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };

        // Configure ViewData for the shared _Pagination partial
        ViewData["CurrentPage"] = viewModel.CurrentPage;
        ViewData["TotalPages"] = viewModel.TotalPages;
        ViewData["PageAction"] = "Index";
        ViewData["PageController"] = "Materials";
        ViewData["PageRouteValues"] = new Dictionary<string, string>
        {
            { "search", search ?? string.Empty },
            { "category", category ?? string.Empty }
        };

        return View(viewModel);
    }

    /// <summary>
    /// Alias for catalog browsing (matching requirements calling for Browse view).
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Browse(string? search = null, string? category = null, int page = 1)
    {
        return await Index(search, category, page);
    }

    /// <summary>
    /// Displays full details for a material listing, including supplier specs, price in EGP, and ordering readiness.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var listing = await _listingService.GetByIdAsync(id);
        if (listing == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        int? userOrgId = user != null ? await _ownershipService.GetUserOrganizationIdAsync(user.Id) : null;
        bool isOwnerSupplier = userOrgId.HasValue && userOrgId.Value == listing.SupplierOrganizationId;

        // Inactive listings are hidden from public/buyers; only the owner supplier can inspect them
        if (!listing.IsActive && !isOwnerSupplier)
        {
            return NotFound();
        }

        // BR-06: Can order only if user is Brand/Factory, listing is active, and stock > 0
        bool isBuyer = User.IsInRole("Brand") || User.IsInRole("Factory");
        bool canOrder = isBuyer && listing.IsActive && listing.AvailableQuantity > 0;

        var viewModel = new MaterialDetailsViewModel
        {
            Listing = listing,
            CanOrder = canOrder,
            IsOwnerSupplier = isOwnerSupplier
        };

        return View(viewModel);
    }

    /// <summary>
    /// Displays the supplier's management dashboard for their own material listings (active and deactivated).
    /// </summary>
    [Authorize(Roles = "Supplier")]
    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var org = await _organizationService.GetByUserIdAsync(user.Id);
        if (org == null)
        {
            _logger.LogWarning("Supplier user {UserId} has no associated organization profile", user.Id);
            return Forbid();
        }

        bool isVerified = await _organizationService.IsVerifiedAsync(org.Id);
        var listings = await _listingService.GetSupplierListingsAsync(org.Id);

        var viewModel = new SupplierManageListingsViewModel
        {
            Listings = listings,
            SupplierOrganization = org,
            IsVerified = isVerified
        };

        return View(viewModel);
    }

    /// <summary>
    /// Displays the form to create a new material listing.
    /// Only verified suppliers may publish new listings.
    /// </summary>
    [Authorize(Roles = "Supplier")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var org = await _organizationService.GetByUserIdAsync(user.Id);
        if (org == null)
        {
            return Forbid();
        }

        bool isVerified = await _organizationService.IsVerifiedAsync(org.Id);
        if (!isVerified)
        {
            TempData["ErrorMessage"] = "Your organization must be verified by an Administrator before you can create material listings.";
            return RedirectToAction(nameof(Manage));
        }

        return View(new MaterialListingFormViewModel());
    }

    /// <summary>
    /// Handles submission of a new material listing.
    /// </summary>
    [Authorize(Roles = "Supplier")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaterialListingFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var org = await _organizationService.GetByUserIdAsync(user.Id);
        if (org == null)
        {
            return Forbid();
        }

        bool isVerified = await _organizationService.IsVerifiedAsync(org.Id);
        if (!isVerified)
        {
            ModelState.AddModelError(string.Empty, "Your organization must be verified by an Administrator before publishing listings.");
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var listing = new MaterialListing
        {
            SupplierOrganizationId = org.Id,
            Name = model.Name,
            Category = model.Category,
            Description = model.Description,
            Unit = model.Unit,
            UnitPrice = model.UnitPrice,
            AvailableQuantity = model.AvailableQuantity,
            IsActive = true
        };

        try
        {
            await _listingService.CreateListingAsync(listing);
            TempData["SuccessMessage"] = $"Material listing '{listing.Name}' was successfully created.";
            return RedirectToAction(nameof(Manage));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    /// <summary>
    /// Displays the form to edit an existing material listing.
    /// Server-side ownership check enforced via IOwnershipService (BR-07).
    /// </summary>
    [Authorize(Roles = "Supplier")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        // BR-07: Record-level authorization
        bool canModify = await _ownershipService.CanModifyMaterialListingAsync(user.Id, id);
        if (!canModify)
        {
            _logger.LogWarning("User {UserId} attempted to edit material listing {Id} without ownership (BR-07)", user.Id, id);
            return Forbid();
        }

        var listing = await _listingService.GetByIdAsync(id);
        if (listing == null)
        {
            return NotFound();
        }

        var viewModel = new MaterialListingFormViewModel
        {
            Id = listing.Id,
            Name = listing.Name,
            Category = listing.Category,
            Description = listing.Description,
            Unit = listing.Unit,
            UnitPrice = listing.UnitPrice,
            AvailableQuantity = listing.AvailableQuantity,
            RowVersion = listing.RowVersion
        };

        return View(viewModel);
    }

    /// <summary>
    /// Handles updates to an existing material listing with optimistic concurrency control.
    /// </summary>
    [Authorize(Roles = "Supplier")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MaterialListingFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        // BR-07: Record-level authorization
        bool canModify = await _ownershipService.CanModifyMaterialListingAsync(user.Id, id);
        if (!canModify)
        {
            _logger.LogWarning("User {UserId} attempted to save edits to material listing {Id} without ownership (BR-07)", user.Id, id);
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var listing = await _listingService.GetByIdAsync(id);
        if (listing == null)
        {
            return NotFound();
        }

        listing.Name = model.Name;
        listing.Category = model.Category;
        listing.Description = model.Description;
        listing.Unit = model.Unit;
        listing.UnitPrice = model.UnitPrice;
        listing.AvailableQuantity = model.AvailableQuantity;
        listing.RowVersion = model.RowVersion ?? [];

        bool updated = await _listingService.UpdateListingAsync(listing);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Another user has updated this listing in the meantime, or invalid values were provided. Please reload the page.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Material listing '{listing.Name}' was successfully updated.";
        return RedirectToAction(nameof(Manage));
    }

    /// <summary>
    /// Soft-deactivates a material listing (IsActive = false).
    /// Listings referenced by orders can never be hard-deleted (BR-05).
    /// </summary>
    [Authorize(Roles = "Supplier")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        // BR-07: Record-level authorization
        bool canModify = await _ownershipService.CanModifyMaterialListingAsync(user.Id, id);
        if (!canModify)
        {
            _logger.LogWarning("User {UserId} attempted to deactivate material listing {Id} without ownership (BR-07)", user.Id, id);
            return Forbid();
        }

        int? orgId = await _ownershipService.GetUserOrganizationIdAsync(user.Id);
        if (!orgId.HasValue)
        {
            return Forbid();
        }

        bool deactivated = await _listingService.DeactivateListingAsync(id, orgId.Value);
        if (!deactivated)
        {
            TempData["ErrorMessage"] = "Could not deactivate the material listing.";
            return RedirectToAction(nameof(Manage));
        }

        TempData["SuccessMessage"] = "The material listing was deactivated and hidden from the public catalog. Order history remains preserved (BR-05).";
        return RedirectToAction(nameof(Manage));
    }
}
