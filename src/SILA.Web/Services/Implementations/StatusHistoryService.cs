// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: Empty service shell for immutable status transition audit logging (BR-08)
// What is left: // TODO(T2.4): Implement StatusHistoryService ensuring records are append-only
// Depends on:  T1.1 (StatusHistory entity, StatusRecordType enum)
// Used by:     T2.4, T3.2, T3.3, T3.4
// How to test: Build project; stub throws NotImplementedException until implemented in T2.4
// Notes:       P4 owns implementation in T2.4
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

public class StatusHistoryService : IStatusHistoryService
{
    private readonly ApplicationDbContext _context;

    public StatusHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task LogHistoryAsync(
        StatusRecordType recordType,
        int recordId,
        string fromStatus,
        string toStatus,
        string changedByUserId,
        string? note = null)
    {
        // TODO(T2.4): Append new StatusHistory record with current timestamp
        throw new NotImplementedException("TODO(T2.4): Implement StatusHistoryService.LogHistoryAsync");
    }

    public Task<IEnumerable<StatusHistory>> GetHistoryAsync(StatusRecordType recordType, int recordId)
    {
        // TODO(T2.4): Retrieve chronological history for the record
        throw new NotImplementedException("TODO(T2.4): Implement StatusHistoryService.GetHistoryAsync");
    }
}

