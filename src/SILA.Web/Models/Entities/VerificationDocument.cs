// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: VerificationDocument entity for role-specific uploaded proofs
// What is left: // TODO(T2.2): Implement document upload with file-type and size limits
// Depends on:  Organization
// Used by:     T2.2
// How to test: Database migration creates VerificationDocuments table with FK to Organizations
// Notes:       Saved outside wwwroot, served only through authorized controller actions
// ═══════════════════════════════════════════════════════

namespace SILA.Web.Models.Entities;

public class VerificationDocument
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string DocumentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
