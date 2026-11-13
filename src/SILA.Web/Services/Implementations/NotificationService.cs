// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.4 | Owner: P4 | Status: DONE
// What exists: Implemented NotificationService for persistent in-app notifications and unread alert counts
// What is left: // TODO(T3.2): Dispatch notifications on MaterialOrder placement, confirmation, rejection, dispatch, delivery
//               // TODO(T3.3): Dispatch notifications on ManufacturingRequest creation, offer, counter-offer, revision, acceptance
// Depends on:  T1.1 (Notification entity, ApplicationUser), T1.2 (INotificationService)
// Used by:     T2.2, T2.4, T3.2, T3.3, T3.4, NotificationsController
// How to test: Create notification, query user notifications, verify unread count decrements on mark as read
// Notes:       BR-10: In-app notifications only (email is strictly for account verification per D-04)
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

/// <summary>
/// Service implementation for managing in-app notifications and unread alert counts (BR-10).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Notification> CreateNotificationAsync(string recipientUserId, string message, string? linkUrl = null)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
        {
            throw new ArgumentException("Recipient user ID cannot be null or empty.", nameof(recipientUserId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Notification message cannot be null or empty.", nameof(message));
        }

        // Bound to entity constraints (500 chars message, 300 chars linkUrl)
        var sanitizedMessage = message.Length > 500 ? message[..500] : message;
        var sanitizedLink = linkUrl != null && linkUrl.Length > 300 ? linkUrl[..300] : linkUrl;

        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Message = sanitizedMessage,
            LinkUrl = sanitizedLink,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(
        string recipientUserId,
        bool unreadOnly = false,
        int page = 1,
        int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
        {
            return Enumerable.Empty<Notification>();
        }

        var validPage = page > 0 ? page : 1;
        var validPageSize = pageSize > 0 && pageSize <= 100 ? pageSize : 20;

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((validPage - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync();

        return notifications;
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(string recipientUserId)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
        {
            return 0;
        }

        var count = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == recipientUserId && !n.IsRead);

        return count;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(int notificationId, string recipientUserId)
    {
        if (notificationId <= 0 || string.IsNullOrWhiteSpace(recipientUserId))
        {
            return false;
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == recipientUserId);

        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAllAsReadAsync(string recipientUserId)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
        {
            return false;
        }

        var unreadNotifications = await _context.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            return true;
        }

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
