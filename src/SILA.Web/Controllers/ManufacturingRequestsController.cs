// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.3 | Owner: P2 | Status: DONE
// What exists: Brand request form, protected details view, and Factory/Brand negotiation decision endpoints.
// What is left: // TODO(T3.4): Link sent/received lists and dashboards to this controller.
// Depends on:  T1.2 (Identity roles), T2.2 (verified factories), T2.4 (ownership checks), T3.3 service.
// Used by:     Manufacturing Request views and role navigation.
// How to test: Build the web project; exercise each endpoint as Brand and Factory users.
// Notes:       Record-level access is checked with IOwnershipService and state/actor rules are enforced in the service.
// ═══════════════════════════════════════════════════════

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.ManufacturingRequests;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

[Authorize]
public sealed class ManufacturingRequestsController : Controller
{
    private readonly IManufacturingRequestService _requests;
    private readonly IOrganizationService _organizations;
    private readonly IOwnershipService _ownership;

    public ManufacturingRequestsController(
        IManufacturingRequestService requests,
        IOrganizationService organizations,
        IOwnershipService ownership)
    {
        _requests = requests;
        _organizations = organizations;
        _ownership = ownership;
    }

    [HttpGet]
    [Authorize(Roles = "Brand")]
    public async Task<IActionResult> Create()
    {
        var userId = CurrentUserId();
        var organizationId = await _ownership.GetUserOrganizationIdAsync(userId);
        if (organizationId is null) return Forbid();

        var model = new CreateManufacturingRequestViewModel();
        await PopulateFactoriesAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Brand")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateManufacturingRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFactoriesAsync(model);
            return View(model);
        }

        var userId = CurrentUserId();
        var organizationId = await _ownership.GetUserOrganizationIdAsync(userId);
        if (organizationId is null) return Forbid();

        try
        {
            var request = await _requests.CreateRequestAsync(
                organizationId.Value,
                model.FactoryOrganizationId,
                model.ProductType,
                model.DesignSpecification ?? string.Empty,
                model.MaterialDescription,
                model.Quantity,
                model.InitialOfferPrice,
                model.AttachmentPath,
                model.Notes,
                userId);
            TempData["SuccessMessage"] = "Your Manufacturing Request was sent to the factory.";
            return RedirectToAction(nameof(Details), new { id = request.Id });
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        await PopulateFactoriesAsync(model);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Brand,Factory")]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0) return NotFound();
        var userId = CurrentUserId();
        if (!await _ownership.CanAccessManufacturingRequestAsync(userId, id)) return Forbid();

        var request = await _requests.GetByIdAsync(id);
        if (request is null) return NotFound();
        var history = await _requests.GetOfferHistoryAsync(id);
        var isBrand = User.IsInRole("Brand");
        var isFactory = User.IsInRole("Factory");

        var model = new ManufacturingRequestDetailsViewModel
        {
            Id = request.Id,
            BrandName = request.BrandOrganization.Name,
            FactoryName = request.FactoryOrganization.Name,
            ProductType = request.ProductType,
            DesignSpecification = request.DesignSpecification,
            MaterialDescription = request.MaterialDescription,
            Quantity = request.Quantity,
            CurrentOfferPrice = request.CurrentOfferPrice,
            AttachmentPath = request.AttachmentPath,
            Notes = request.Notes,
            DecisionNote = request.DecisionNote,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            Offers = history.Select(offer => new OfferTimelineViewModel
            {
                RoundNumber = offer.RoundNumber,
                ActorName = offer.OfferedByOrganization.Name,
                UnitPrice = offer.UnitPrice,
                Note = offer.Note,
                CreatedAt = offer.CreatedAt
            }).ToList(),
            CanFactoryRespond = isFactory && request.Status == ManufacturingRequestStatus.PendingApproval,
            CanBrandRespondToCounterOffer = isBrand && request.Status == ManufacturingRequestStatus.CounterOffered,
            CanBrandCancel = isBrand && request.Status is ManufacturingRequestStatus.PendingApproval or ManufacturingRequestStatus.CounterOffered
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Factory")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Accept(RequestDecisionViewModel model) =>
        RunDecisionAsync(model, (requestId, organizationId, userId) =>
            _requests.AcceptRequestAsync(requestId, organizationId, userId, model.Note));

    [HttpPost]
    [Authorize(Roles = "Factory")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(RequestDecisionViewModel model) =>
        RunDecisionAsync(model, (requestId, organizationId, userId) =>
            _requests.RejectRequestAsync(requestId, organizationId, userId, model.Note));

    [HttpPost]
    [Authorize(Roles = "Factory")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CounterOffer(OfferActionViewModel model) =>
        RunOfferActionAsync(model, (requestId, organizationId, userId) =>
            _requests.CounterOfferAsync(requestId, organizationId, userId, model.OfferPrice, model.Note));

    [HttpPost]
    [Authorize(Roles = "Brand")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AcceptCounterOffer(RequestDecisionViewModel model) =>
        RunDecisionAsync(model, (requestId, organizationId, userId) =>
            _requests.AcceptRequestAsync(requestId, organizationId, userId, model.Note));

    [HttpPost]
    [Authorize(Roles = "Brand")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RejectCounterOffer(RequestDecisionViewModel model) =>
        RunDecisionAsync(model, (requestId, organizationId, userId) =>
            _requests.RejectRequestAsync(requestId, organizationId, userId, model.Note));

    [HttpPost]
    [Authorize(Roles = "Brand")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ReviseOffer(OfferActionViewModel model) =>
        RunOfferActionAsync(model, (requestId, organizationId, userId) =>
            _requests.ReviseOfferAsync(requestId, organizationId, userId, model.OfferPrice, model.Note));

    [HttpPost]
    [Authorize(Roles = "Brand")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(RequestDecisionViewModel model) =>
        RunDecisionAsync(model, (requestId, organizationId, userId) =>
            _requests.CancelRequestAsync(requestId, organizationId, userId));

    private async Task<IActionResult> RunDecisionAsync(
        RequestDecisionViewModel model,
        Func<int, int, string, Task<bool>> action)
    {
        if (model.RequestId <= 0) return BadRequest();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "The decision note is invalid. Please review it and try again.";
            return RedirectToAction(nameof(Details), new { id = model.RequestId });
        }
        return await ExecuteActionAsync(model.RequestId, action);
    }

    private async Task<IActionResult> RunOfferActionAsync(
        OfferActionViewModel model,
        Func<int, int, string, Task<bool>> action)
    {
        if (model.RequestId <= 0) return BadRequest();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Enter a valid offer price greater than zero and a note of up to 500 characters.";
            return RedirectToAction(nameof(Details), new { id = model.RequestId });
        }
        return await ExecuteActionAsync(model.RequestId, action);
    }

    private async Task<IActionResult> ExecuteActionAsync(int requestId, Func<int, int, string, Task<bool>> action)
    {
        var userId = CurrentUserId();
        if (!await _ownership.CanAccessManufacturingRequestAsync(userId, requestId)) return Forbid();
        var organizationId = await _ownership.GetUserOrganizationIdAsync(userId);
        if (organizationId is null) return Forbid();

        var succeeded = await action(requestId, organizationId.Value, userId);
        TempData[succeeded ? "SuccessMessage" : "ErrorMessage"] = succeeded
            ? "The Manufacturing Request was updated."
            : "That action is not allowed for the current request status or organization.";
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    private async Task PopulateFactoriesAsync(CreateManufacturingRequestViewModel model)
    {
        var (factories, _) = await _organizations.GetFactoriesAsync(page: 1, pageSize: 100);
        model.VerifiedFactories = factories
            .Select(factory => new SelectListItem(factory.Name, factory.Id.ToString(), factory.Id == model.FactoryOrganizationId))
            .ToList();
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("A signed-in user is required.");
}
