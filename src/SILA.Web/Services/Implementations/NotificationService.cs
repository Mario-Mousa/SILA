// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for in-app user notifications (BR-10)
// What is left: // TODO(T2.4): Implement NotificationService for persistent in-app notifications and unread badge
// Depends on:  T1.1 (Notification entity, ApplicationUser)
// Used by:     T2.4, T3.2, T3.3, T3.4
// How to test: Build project; stub throws NotImplementedException until implemented in T2.4
// Notes:       P4 owns implementation in T2.4
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Notification> CreateNotificationAsync(string recipientUserId, string message, string? linkUrl = null)
    {
        // TODO(T2.4): Create Notification entity and persist to database
        throw new NotImplementedException("TODO(T2.4): Implement NotificationService.CreateNotificationAsync");
    }

    public Task<IEnumerable<Notification>> GetUserNotificationsAsync(string recipientUserId, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        // TODO(T2.4): Retrieve paginated notifications ordered by CreatedAt descending
        throw new NotImplementedException("TODO(T2.4): Implement NotificationService.GetUserNotificationsAsync");
    }

    public Task<int> GetUnreadCountAsync(string recipientUserId)
    {
        // TODO(T2.4): Count notifications where IsRead == false
        throw new NotImplementedException("TODO(T2.4): Implement NotificationService.GetUnreadCountAsync");
    }

    public Task<bool> MarkAsReadAsync(int notificationId, string recipientUserId)
    {
        // TODO(T2.4): Mark single notification as read if recipient matches
        throw new NotImplementedException("TODO(T2.4): Implement NotificationService.MarkAsReadAsync");
    }

    public Task<bool> MarkAllAsReadAsync(string recipientUserId)
    {
        // TODO(T2.4): Mark all notifications for user as read
        throw new NotImplementedException("TODO(T2.4): Implement NotificationService.MarkAllAsReadAsync");
    }
}

