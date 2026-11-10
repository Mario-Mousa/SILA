// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.2 | Owner: P2 | Status: DONE
// What exists: ProfileController with record-level security, role-specific profile view/edit, and secure verification document uploads
// What is left: // TODO(T2.3): Wire client-side profile view enhancements
// Depends on:  T1.1 (Organization, VerificationDocument, Enums), T1.2 (IOrganizationService, IVerificationService), T2.1 (ApplicationUser)
// Used by:     Views/Profile/*, Application navigation
// How to test: Sign in as Brand/Factory/Supplier -> navigate to /Profile -> edit profile details -> upload .pdf or .png document -> verify status changes to PendingReview
// Notes:       Enforces BR-07 record-level security; strictly validates document types (.pdf, .png, .jpg) and 5MB limit
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.Profile;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrganizationService _organizationService;
    private readonly IVerificationService _verificationService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProfileController> _logger;

    private static readonly string[] AllowedDocumentExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private static readonly string[] AllowedLogoExtensions = [".png", ".jpg", ".jpeg", ".svg"];
    private const long MaxDocumentSize = 5 * 1024 * 1024; // 5 MB
    private const long MaxLogoSize = 2 * 1024 * 1024;     // 2 MB

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IOrganizationService organizationService,
        IVerificationService verificationService,
        IWebHostEnvironment env,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _organizationService = organizationService;
        _verificationService = verificationService;
        _env = env;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // 1. VIEW PROFILE & DOCUMENTS
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var org = await _organizationService.GetByUserIdAsync(user.Id);
        if (org == null)
        {
            // Admin users do not have an Organization profile
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            return NotFound("Organization profile not found.");
        }

        var documents = await _verificationService.GetDocumentsByOrganizationIdAsync(org.Id);

        var viewModel = new ProfileViewModel
        {
            OrganizationId = org.Id,
            OwnerEmail = user.Email ?? string.Empty,
            OwnerFullName = user.FullName,
            Role = org.Type,
            Name = org.Name,
            Description = org.Description,
            City = org.City,
            ContactPhone = org.ContactPhone,
            LogoPath = org.LogoPath,
            InstagramUrl = org.InstagramUrl,
            PortfolioText = org.PortfolioText,
            Address = org.Address,
            VerificationStatus = org.VerificationStatus,
            VerificationNote = org.VerificationNote,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt,
            Documents = documents,
            UploadModel = new UploadVerificationDocumentViewModel { OrganizationId = org.Id }
        };

        return View(viewModel);
    }

    // ─────────────────────────────────────────────────────────────
    // 2. EDIT PROFILE
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var org = await _organizationService.GetByUserIdAsync(user.Id);
        if (org == null)
        {
            return NotFound("Organization not found.");
        }

        var model = new EditProfileViewModel
        {
            OrganizationId = org.Id,
            Role = org.Type,
            Name = org.Name,
            City = org.City,
            ContactPhone = org.ContactPhone,
            Description = org.Description,
            InstagramUrl = org.InstagramUrl,
            PortfolioText = org.PortfolioText,
            Address = org.Address
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // BR-07: Record-level authorization - user can only edit their own organization
        var userOrg = await _organizationService.GetByUserIdAsync(user.Id);
        if (userOrg == null || userOrg.Id != model.OrganizationId)
        {
            _logger.LogWarning("Security violation: User {UserId} attempted to modify Organization {OrgId}", user.Id, model.OrganizationId);
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            model.Role = userOrg.Type;
            return View(model);
        }

        // Handle optional logo upload
        string? logoRelativePath = null;
        if (model.LogoFile != null && model.LogoFile.Length > 0)
        {
            var ext = Path.GetExtension(model.LogoFile.FileName).ToLowerInvariant();
            if (!AllowedLogoExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.LogoFile), "Invalid logo file format. Allowed: .png, .jpg, .jpeg, .svg.");
                model.Role = userOrg.Type;
                return View(model);
            }

            if (model.LogoFile.Length > MaxLogoSize)
            {
                ModelState.AddModelError(nameof(model.LogoFile), "Logo size cannot exceed 2 MB.");
                model.Role = userOrg.Type;
                return View(model);
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "logos");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.LogoFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.LogoFile.CopyToAsync(stream);
            }

            logoRelativePath = $"/uploads/logos/{uniqueFileName}";
        }

        userOrg.Name = model.Name;
        userOrg.City = model.City;
        userOrg.ContactPhone = model.ContactPhone;
        userOrg.Description = model.Description;
        userOrg.InstagramUrl = model.InstagramUrl;
        userOrg.PortfolioText = model.PortfolioText;
        userOrg.Address = model.Address;

        if (logoRelativePath != null)
        {
            userOrg.LogoPath = logoRelativePath;
        }

        var updated = await _organizationService.UpdateProfileAsync(userOrg);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Failed to update profile. Please try again.");
            model.Role = userOrg.Type;
            return View(model);
        }

        TempData["SuccessMessage"] = "Your organization profile has been successfully updated.";
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────────────────────
    // 3. UPLOAD VERIFICATION DOCUMENT
    // ─────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(UploadVerificationDocumentViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // BR-07: Record-level authorization - user can only upload documents for their own organization
        var userOrg = await _organizationService.GetByUserIdAsync(user.Id);
        if (userOrg == null || userOrg.Id != model.OrganizationId)
        {
            _logger.LogWarning("Security violation: User {UserId} attempted to upload document for Organization {OrgId}", user.Id, model.OrganizationId);
            return Forbid();
        }

        if (model.File == null || model.File.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid document file to upload.";
            return RedirectToAction(nameof(Index));
        }

        var ext = Path.GetExtension(model.File.FileName).ToLowerInvariant();
        if (!AllowedDocumentExtensions.Contains(ext))
        {
            TempData["ErrorMessage"] = "Unsupported file type. Allowed formats: .pdf, .png, .jpg, .jpeg.";
            return RedirectToAction(nameof(Index));
        }

        if (model.File.Length > MaxDocumentSize)
        {
            TempData["ErrorMessage"] = "File size exceeds the 5 MB limit. Please upload a smaller file.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "verification");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.File.FileName)}";
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/verification/{uniqueFileName}";
            var docType = string.IsNullOrWhiteSpace(model.DocumentType) ? "Verification Proof" : model.DocumentType.Trim();

            await _verificationService.SubmitVerificationDocumentAsync(userOrg.Id, docType, relativePath);

            TempData["SuccessMessage"] = "Verification document uploaded successfully! Your account status is now Pending Review by Admin.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload verification document for Org {OrgId}", userOrg.Id);
            TempData["ErrorMessage"] = "An unexpected error occurred while uploading the file. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}

