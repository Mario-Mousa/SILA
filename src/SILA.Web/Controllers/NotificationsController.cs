// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.4 | Owner: P4 | Status: DONE
// What exists: NotificationsController exposing notification listing, mark-read actions, and unread count endpoint
// What is left: // TODO(T3.4): Integrate latest notification alerts into role-specific dashboard views
// Depends on:  T1.1 (Notification), T1.2 (INotificationService), T2.4 (NotificationService)
// Used by:     Views/Notifications/*, _NotificationBell partial, site navigation
// How to test: Log in as any user; visit /Notifications; verify unread badge updates and marking as read works
// Notes:       Part 5.2: Thin controller delegating all business logic to INotificationService
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SILA.Web.Models.Entities;
using SILA.Web.Models.ViewModels.Notifications;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    /// <summary>
    /// Displays the paginated notifications center for the current authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        const int pageSize = 20;
        var currentPage = page > 0 ? page : 1;

        var notifications = await _notificationService.GetUserNotificationsAsync(
            user.Id, unreadOnly: false, page: currentPage, pageSize: pageSize);

        var unreadCount = await _notificationService.GetUnreadCountAsync(user.Id);

        var viewModel = new NotificationListViewModel
        {
            Notifications = notifications.Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList(),
            UnreadCount = unreadCount,
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalCount = unreadCount + notifications.Count() // Estimated minimum bound for pagination
        };

        return View(viewModel);
    }

    /// <summary>
    /// Lightweight JSON endpoint returning the current unread notifications count for the top nav bell icon.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { count = 0 });
        }

        var count = await _notificationService.GetUnreadCountAsync(user.Id);
        return Json(new { count });
    }

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var success = await _notificationService.MarkAsReadAsync(id, user.Id);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success });
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Marks all unread notifications for the current user as read.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var success = await _notificationService.MarkAllAsReadAsync(user.Id);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success });
        }

        return RedirectToAction(nameof(Index));
    }
}

