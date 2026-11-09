// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.1 | Owner: P1 | Status: DONE
// What exists: Full AccountController handling registration, atomic user+organization creation, SMTP email confirmation, login with email-check, logout, and role redirection
// What is left: // TODO(T2.2): Connect verification document upload after registration
//               // TODO(T2.3): Wire public role selection buttons to registration view
// Depends on:  T1.1 (ApplicationUser, Organization, Enums), T1.2 (Identity, Roles, IEmailSender)
// Used by:     Views/Account/*, Application authentication pipeline
// How to test: Register Brand/Factory/Supplier -> verify atomic Organization creation -> confirm via email link -> login redirects to role Dashboard
// Notes:       Enforces atomic transaction on user + organization; blocks Admin public sign-up (D-02); enforces email verification before login (D-04)
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Models.ViewModels.Account;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // 1. REGISTRATION
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new RegisterViewModel();
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<OrganizationType>(role, true, out var parsedRole))
        {
            model.Role = parsedRole;
        }

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // D-02: Public registration is strictly for Brand, Factory, or Supplier
        if (!Enum.IsDefined(typeof(OrganizationType), model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Please select a valid organization role.");
            return View(model);
        }

        // Check if email already registered
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            return View(model);
        }

        // Execute atomic creation of ApplicationUser and Organization within a single transaction (BR-11)
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = new ApplicationUser
            {
                UserName = model.Email.Trim().ToLowerInvariant(),
                Email = model.Email.Trim().ToLowerInvariant(),
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                EmailConfirmed = false
            };

            var userResult = await _userManager.CreateAsync(user, model.Password);
            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await transaction.RollbackAsync();
                return View(model);
            }

            // Assign role corresponding to selected OrganizationType
            var roleName = model.Role.ToString();
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await transaction.RollbackAsync();
                return View(model);
            }

            // Create initial 1:1 Organization profile (VerificationStatus = Unverified)
            var organization = new Organization
            {
                OwnerUserId = user.Id,
                Type = model.Role,
                Name = model.OrganizationName.Trim(),
                City = model.City.Trim(),
                ContactPhone = model.PhoneNumber.Trim(),
                VerificationStatus = VerificationStatus.Unverified,
                CreatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Commit the user + organization creation together
            await transaction.CommitAsync();

            _logger.LogInformation("New user {Email} registered as {Role} with organization {OrgName} (ID: {OrgId})",
                user.Email, roleName, organization.Name, organization.Id);

            // Generate email confirmation token and send email (D-04)
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                action: nameof(ConfirmEmail),
                controller: "Account",
                values: new { userId = user.Id, token },
                protocol: Request.Scheme);

            var emailBody = $@"
                <div style='font-family: Segoe UI, Tahoma, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e1e6e2; border-radius: 12px; background-color: #fffdfa; color: #202824;'>
                    <h2 style='color: #153d35; margin-top: 0;'>Welcome to SILA (صِلة)</h2>
                    <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(user.FullName)}</strong>,</p>
                    <p>Thank you for registering <strong>{System.Net.WebUtility.HtmlEncode(organization.Name)}</strong> as a <strong>{organization.Type}</strong> on SILA — Egypt's fashion supply network.</p>
                    <p>To activate your account and proceed with verification, please confirm your email address by clicking the button below:</p>
                    <p style='margin: 32px 0;'>
                        <a href='{callbackUrl}' style='display: inline-block; background-color: #153d35; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: bold;'>Confirm Email Address</a>
                    </p>
                    <p style='color: #68736e; font-size: 13px; line-height: 1.5;'>If the button does not work, copy and paste this URL into your browser:<br/><a href='{callbackUrl}' style='color: #153d35;'>{callbackUrl}</a></p>
                    <hr style='border: none; border-top: 1px solid #e1e6e2; margin: 24px 0;'/>
                    <p style='color: #68736e; font-size: 12px; margin-bottom: 0;'>If you did not register for an account on SILA, please ignore this email.</p>
                </div>";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email — SILA", emailBody);

            TempData["RegisteredEmail"] = user.Email;
            return RedirectToAction(nameof(EmailConfirmationSent));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to complete atomic registration for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating your account. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult EmailConfirmationSent()
    {
        ViewBag.Email = TempData["RegisteredEmail"] as string ?? string.Empty;
        return View();
    }

    // ─────────────────────────────────────────────────────────────
    // 2. EMAIL CONFIRMATION
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            ViewBag.IsSuccess = false;
            ViewBag.Message = "Invalid email confirmation link or expired token.";
            return View();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ViewBag.IsSuccess = false;
            ViewBag.Message = "User account not found.";
            return View();
        }

        if (user.EmailConfirmed)
        {
            ViewBag.IsSuccess = true;
            ViewBag.Message = "Your email has already been confirmed. You can now sign in.";
            return View();
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            _logger.LogInformation("Email confirmed successfully for user {Email}", user.Email);
            ViewBag.IsSuccess = true;
            ViewBag.Message = "Your email address has been confirmed successfully! You can now sign in to your SILA account.";
        }
        else
        {
            _logger.LogWarning("Email confirmation failed for user {Email}: {Errors}",
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            ViewBag.IsSuccess = false;
            ViewBag.Message = "Email confirmation failed. The link may have expired or is invalid.";
        }

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResendConfirmationEmail()
    {
        return View(new ResendConfirmationEmailViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmationEmail(ResendConfirmationEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            // Do not reveal that user does not exist
            TempData["RegisteredEmail"] = model.Email;
            return RedirectToAction(nameof(EmailConfirmationSent));
        }

        if (user.EmailConfirmed)
        {
            TempData["InfoMessage"] = "This account email has already been confirmed. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.Action(
            action: nameof(ConfirmEmail),
            controller: "Account",
            values: new { userId = user.Id, token },
            protocol: Request.Scheme);

        var emailBody = $@"
            <div style='font-family: Segoe UI, Tahoma, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e1e6e2; border-radius: 12px; background-color: #fffdfa; color: #202824;'>
                <h2 style='color: #153d35; margin-top: 0;'>Confirm your SILA Account</h2>
                <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(user.FullName)}</strong>,</p>
                <p>You requested a new confirmation link for your SILA account. Please click the button below to verify your email address:</p>
                <p style='margin: 32px 0;'>
                    <a href='{callbackUrl}' style='display: inline-block; background-color: #153d35; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: bold;'>Confirm Email Address</a>
                </p>
                <p style='color: #68736e; font-size: 13px;'>If the button does not work, use this URL:<br/><a href='{callbackUrl}' style='color: #153d35;'>{callbackUrl}</a></p>
            </div>";

        await _emailSender.SendEmailAsync(user.Email!, "Confirm your email — SILA", emailBody);

        TempData["RegisteredEmail"] = user.Email;
        return RedirectToAction(nameof(EmailConfirmationSent));
    }

    // ─────────────────────────────────────────────────────────────
    // 3. LOGIN & LOGOUT
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                return await RedirectToRoleHomeAsync(currentUser, returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid email address or password.");
            return View(model);
        }

        // Enforce email verification (D-04, T2.1 acceptance criteria)
        if (!user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Your email address has not been confirmed yet. Please check your inbox or resend the confirmation email.");
            ViewBag.UnconfirmedEmail = user.Email;
            ViewBag.ShowResendConfirmation = true;
            return View(model);
        }

        // Sign in user
        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return await RedirectToRoleHomeAsync(user, returnUrl);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ─────────────────────────────────────────────────────────────
    // 4. ROLE REDIRECTION HELPER
    // ─────────────────────────────────────────────────────────────

    private async Task<IActionResult> RedirectToRoleHomeAsync(ApplicationUser user, string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
        {
            return RedirectToAction("Index", "Admin");
        }

        if (roles.Contains("Brand") || roles.Contains("Factory") || roles.Contains("Supplier"))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction("Index", "Home");
    }
}

