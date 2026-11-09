// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.1 | Owner: P1 | Status: DONE
// What exists: Login ViewModel for user sign-in with email, password, and persistent cookie options
// What is left: none
// Depends on:  none
// Used by:     AccountController, Views/Account/Login.cshtml
// How to test: Sign in with valid credentials; verify validation triggers on empty/invalid inputs
// Notes:       Used by Brand, Factory, Supplier, and seeded Admin
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace SILA.Web.Models.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me on this device")]
    public bool RememberMe { get; set; }
}

