// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: ApplicationUser Identity entity with FullName and navigation properties
// What is left: // TODO(T1.2): Configure Identity options and roles (Brand, Factory, Supplier, Admin)
// Depends on:  none
// Used by:     T1.2, T2.1, T2.2, T2.4
// How to test: Build project and inspect schema/migrations
// Notes:       1-1 with Organization via OwnerUserId, 1-* with Notifications
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Identity;

namespace SILA.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public Organization? Organization { get; set; }

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
