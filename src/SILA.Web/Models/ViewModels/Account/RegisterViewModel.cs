// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.1 | Owner: P1 | Status: DONE
// What exists: Registration ViewModel capturing user credentials and organization profile for the 3 business roles
// What is left: // TODO(T2.2): connect verification-document upload after registration
// Depends on:  T1.1 (OrganizationType enum)
// Used by:     AccountController, Views/Account/Register.cshtml
// How to test: Model validation in AccountController.Register POST
// Notes:       Admin role is strictly excluded; only Brand, Factory, and Supplier can register
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact phone number is required.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your business role.")]
    [Display(Name = "I am a")]
    public OrganizationType Role { get; set; }

    [Required(ErrorMessage = "Organization / Company name is required.")]
    [StringLength(150, ErrorMessage = "Organization name cannot exceed 150 characters.")]
    [Display(Name = "Organization Name")]
    public string OrganizationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "City / Governorate is required.")]
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

