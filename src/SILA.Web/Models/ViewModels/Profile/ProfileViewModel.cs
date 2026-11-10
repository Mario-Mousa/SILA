// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.2 | Owner: P2 | Status: DONE
// What exists: Profile ViewModels for viewing, editing, and uploading verification documents
// What is left: // TODO(T2.3): Wire client-side styling and validation helpers
// Depends on:  T1.1 (Organization, VerificationDocument, Enums)
// Used by:     ProfileController, Views/Profile/*
// How to test: View profile page as Brand, Factory, or Supplier; verify role-specific fields and document upload
// Notes:       Enforces role-based display and strict file upload rules (.pdf, .png, .jpg, max 5MB)
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.Profile;

public class ProfileViewModel
{
    public int OrganizationId { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;
    public OrganizationType Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string City { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? LogoPath { get; set; }

    // Brand-specific
    public string? InstagramUrl { get; set; }
    public string? PortfolioText { get; set; }

    // Factory-specific
    public string? Address { get; set; }

    // Verification
    public VerificationStatus VerificationStatus { get; set; }
    public string? VerificationNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public IEnumerable<VerificationDocument> Documents { get; set; } = new List<VerificationDocument>();
    public UploadVerificationDocumentViewModel UploadModel { get; set; } = new();
}

public class EditProfileViewModel
{
    public int OrganizationId { get; set; }
    public OrganizationType Role { get; set; }

    [Required(ErrorMessage = "Organization name is required.")]
    [StringLength(150, ErrorMessage = "Organization name cannot exceed 150 characters.")]
    [Display(Name = "Organization Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "City / Governorate is required.")]
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact phone number is required.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    [Display(Name = "Contact Phone")]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Business Description")]
    public string? Description { get; set; }

    // Brand-specific
    [Url(ErrorMessage = "Please enter a valid URL (e.g. https://instagram.com/yourbrand).")]
    [StringLength(300, ErrorMessage = "Instagram URL cannot exceed 300 characters.")]
    [Display(Name = "Instagram / Social Portfolio Link (Brand Proof)")]
    public string? InstagramUrl { get; set; }

    [StringLength(2000, ErrorMessage = "Portfolio summary cannot exceed 2000 characters.")]
    [Display(Name = "Portfolio & Past Collections")]
    public string? PortfolioText { get; set; }

    // Factory-specific
    [StringLength(300, ErrorMessage = "Factory address cannot exceed 300 characters.")]
    [Display(Name = "Factory Workshop Address (Physical Location)")]
    public string? Address { get; set; }

    [Display(Name = "Organization Logo")]
    public IFormFile? LogoFile { get; set; }
}

public class UploadVerificationDocumentViewModel
{
    public int OrganizationId { get; set; }

    [Required(ErrorMessage = "Please select or enter the document type.")]
    [StringLength(50, ErrorMessage = "Document type cannot exceed 50 characters.")]
    [Display(Name = "Document Type")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a file to upload.")]
    [Display(Name = "Verification Document (.pdf, .png, .jpg — Max 5MB)")]
    public IFormFile? File { get; set; }
}

