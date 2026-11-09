// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.1 | Owner: P1 | Status: DONE
// What exists: SMTP email sender for account verification confirmation links
// What is left: none
// Depends on:  IEmailSender interface, IConfiguration (user-secrets / appsettings)
// Used by:     AccountController
// How to test: Set Smtp:* secrets or observe console log fallback in Development; register new account -> email sent
// Notes:       Email is strictly for account verification (D-04). If Smtp:Host is unconfigured, logs link safely in dev
// ═══════════════════════════════════════════════════════

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = _configuration["Smtp:Host"];
        var portStr = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@sila.com";

        // Fallback for development if SMTP is not configured in user-secrets
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("SMTP is not configured (Smtp:Host or Smtp:Username missing). " +
                               "Simulating email dispatch for verification to {Email}. Subject: {Subject}", email, subject);
            _logger.LogInformation("Simulated Email Content for {Email}:\n{Body}", email, htmlMessage);
            return;
        }

        int port = 587;
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var parsedPort))
        {
            port = parsedPort;
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, "SILA Platform"),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        mailMessage.To.Add(email);

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Verification email successfully dispatched to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
            throw;
        }
    }
}
