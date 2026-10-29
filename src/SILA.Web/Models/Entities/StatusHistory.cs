// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: StatusHistory append-only audit entity for all request & order transitions
// What is left: // TODO(T2.4): Implement IStatusHistoryService to record entries on status changes
// Depends on:  StatusRecordType, ApplicationUser
// Used by:     T2.4, T3.2, T3.3, T3.4
// How to test: Database migration creates StatusHistories table with immutable audit records
// Notes:       BR-08: Append-only; users cannot edit or delete these rows
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Enums;

namespace SILA.Web.Models.Entities;

public class StatusHistory
{
    public int Id { get; set; }

    public StatusRecordType RecordType { get; set; }
    public int RecordId { get; set; }

    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;

    public string ChangedByUserId { get; set; } = string.Empty;
    public ApplicationUser? ChangedByUser { get; set; }

    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
