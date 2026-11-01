// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for SMTP email confirmation (D-04)
// What is left: // TODO(T2.1): Implement SmtpEmailSender using SMTP configuration from user-secrets
// Depends on:  none
// Used by:     T2.1 (AccountController registration confirmation)
// How to test: Build project; stub throws NotImplementedException until implemented in T2.1
// Notes:       P1 owns implementation in T2.1; used solely for account email confirmation
// ═══════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // TODO(T2.1): Send email using SmtpClient or MailKit using Smtp settings in user-secrets
        throw new NotImplementedException("TODO(T2.1): Implement EmailSender.SendEmailAsync");
    }
}

