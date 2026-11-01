// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for sending account verification emails via SMTP
// What is left: // TODO(T2.1): Implement SmtpEmailSender using SMTP configuration from user-secrets
// Depends on:  none
// Used by:     T2.1 (AccountController registration confirmation)
// How to test: Compile project; ensure DI registration compiles
// Notes:       Email is used ONLY for account verification (D-04); free SMTP provider
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for sending verification emails.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}

