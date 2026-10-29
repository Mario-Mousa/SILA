// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: Notification entity for in-app alerts on state-changing events
// What is left: // TODO(T2.4): Implement INotificationService (add, list, mark read, unread count)
// Depends on:  ApplicationUser
// Used by:     T2.4, T3.2, T3.3, T3.4
// How to test: Database migration creates Notifications table with FK to AspNetUsers
// Notes:       BR-10: In-app notifications only (email is strictly for account verification per D-04)
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Models.Entities;

public class Notification
{
    public int Id { get; set; }

    public string RecipientUserId { get; set; } = string.Empty;
    public ApplicationUser RecipientUser { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
