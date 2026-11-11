// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.3 | Owner: P3 | Status: DONE
// What exists: ViewModel for the role-choice entry screen (Choose organization type)
// What is left: none
// Depends on:  T1.1 (OrganizationType enum)
// Used by:     HomeController (RoleChoice action), Views/Home/RoleChoice.cshtml
// How to test: Navigate to /Home/RoleChoice; select a role and continue to sign in or register
// Notes:       Enforces explicit role choice preceding credentials (wireframes line 70-85)
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.Home;

public class RoleChoiceViewModel
{
    public OrganizationType? SelectedRole { get; set; }

    public string ActionType { get; set; } = "Register"; // "Register" or "Login"
}

