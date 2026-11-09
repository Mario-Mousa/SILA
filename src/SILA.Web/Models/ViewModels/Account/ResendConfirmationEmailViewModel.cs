// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.1 | Owner: P1 | Status: DONE
// What exists: ViewModel for requesting a new account verification email
// What is left: none
// Depends on:  none
// Used by:     AccountController, Views/Account/ResendConfirmationEmail.cshtml
// How to test: Submit email to request a new verification token link
// Notes:       Email is only for account verification (D-04)
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace SILA.Web.Models.ViewModels.Account;

public class ResendConfirmationEmailViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Registered Email Address")]
    public string Email { get; set; } = string.Empty;
}

