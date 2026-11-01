// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for in-app user notifications (BR-10)
// What is left: // TODO(T2.4): Implement NotificationService for real-time in-app alerts and count badge
// Depends on:  T1.1 (Notification entity, ApplicationUser)
// Used by:     T2.2, T2.4, T3.2, T3.3, T3.4
// How to test: Compile project; ensure DI registration compiles
// Notes:       Email is only for account verification; all business events trigger in-app notifications
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for managing in-app notifications and unread alert counts.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new in-app notification for a designated user.
    /// </summary>
    Task<Notification> CreateNotificationAsync(string recipientUserId, string message, string? linkUrl = null);

    /// <summary>
    /// Retrieves a paginated list of notifications for a given user.
    /// </summary>
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string recipientUserId, bool unreadOnly = false, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets the count of unread notifications for a user to display on the top nav bell icon.
    /// </summary>
    Task<int> GetUnreadCountAsync(string recipientUserId);

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId, string recipientUserId);

    /// <summary>
    /// Marks all unread notifications for a user as read.
    /// </summary>
    Task<bool> MarkAllAsReadAsync(string recipientUserId);
}

