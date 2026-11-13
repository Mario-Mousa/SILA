// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.4 | Owner: P4 | Status: DONE
// What exists: ViewModels for notification listing and item presentation
// What is left: none
// Depends on:  T1.1 (Notification entity)
// Used by:     NotificationsController, Views/Notifications/Index.cshtml
// How to test: View notifications list in browser; verify unread badge count and pagination
// Notes:       Part 7.1: ViewModels used exclusively for views, never entities
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Models.ViewModels.Notifications;

public class NotificationItemViewModel
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.UtcNow - CreatedAt;
            if (elapsed.TotalMinutes < 1) return "Just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
            return CreatedAt.ToString("dd MMM yyyy");
        }
    }
}

public class NotificationListViewModel
{
    public List<NotificationItemViewModel> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

