// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Service interface for append-only status audit logging (BR-08)
// What is left: // TODO(T2.4): Implement StatusHistoryService ensuring records cannot be updated or deleted
// Depends on:  T1.1 (StatusHistory entity, StatusRecordType enum)
// Used by:     T2.4, T3.2, T3.3, T3.4
// How to test: Compile project; ensure DI registration compiles
// Notes:       BR-08: Every transition in requests and orders records previous/new status, actor, timestamp, note
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Services.Interfaces;

/// <summary>
/// Service contract for recording and querying immutable audit trails of status transitions (BR-08).
/// </summary>
public interface IStatusHistoryService
{
    /// <summary>
    /// Appends a new immutable status transition audit record.
    /// </summary>
    Task LogHistoryAsync(
        StatusRecordType recordType,
        int recordId,
        string fromStatus,
        string toStatus,
        string changedByUserId,
        string? note = null);

    /// <summary>
    /// Retrieves full chronologically-ordered status history for a specific request or order.
    /// </summary>
    Task<IEnumerable<StatusHistory>> GetHistoryAsync(StatusRecordType recordType, int recordId);
}

