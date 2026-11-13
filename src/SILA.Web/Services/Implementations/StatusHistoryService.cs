// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.4 | Owner: P4 | Status: DONE
// What exists: Implemented StatusHistoryService ensuring immutable, append-only status transition logging (BR-08)
// What is left: // TODO(T3.2): Invoke LogHistoryAsync on all MaterialOrder status changes (Confirm, Reject, Dispatch, Deliver, Cancel)
//               // TODO(T3.3): Invoke LogHistoryAsync on all ManufacturingRequest status changes (CounterOffer, Accept, Reject, Revise, Cancel)
//               // TODO(T3.4): Consume GetHistoryAsync in Request and Order detail views
// Depends on:  T1.1 (StatusHistory entity, StatusRecordType enum), T1.2 (IStatusHistoryService)
// Used by:     T2.4, T3.2, T3.3, T3.4
// How to test: Log transitions; verify chronologically ordered retrieval; verify absence of update/delete operations
// Notes:       BR-08: Every transition in requests and orders records previous/new status, actor, timestamp, note.
//              Rows are append-only and cannot be updated or deleted.
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Interfaces;

namespace SILA.Web.Services.Implementations;

/// <summary>
/// Service implementation for recording and querying immutable audit trails of status transitions (BR-08).
/// Enforces an append-only architecture: historical entries can never be modified or deleted.
/// </summary>
public class StatusHistoryService : IStatusHistoryService
{
    private readonly ApplicationDbContext _context;

    public StatusHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task LogHistoryAsync(
        StatusRecordType recordType,
        int recordId,
        string fromStatus,
        string toStatus,
        string changedByUserId,
        string? note = null)
    {
        if (recordId <= 0)
        {
            throw new ArgumentException("Record ID must be a positive integer.", nameof(recordId));
        }

        if (string.IsNullOrWhiteSpace(fromStatus))
        {
            throw new ArgumentException("Source status cannot be empty.", nameof(fromStatus));
        }

        if (string.IsNullOrWhiteSpace(toStatus))
        {
            throw new ArgumentException("Destination status cannot be empty.", nameof(toStatus));
        }

        if (string.IsNullOrWhiteSpace(changedByUserId))
        {
            throw new ArgumentException("ChangedBy user ID cannot be empty.", nameof(changedByUserId));
        }

        var sanitizedNote = note != null && note.Length > 500 ? note[..500] : note;

        var historyEntry = new StatusHistory
        {
            RecordType = recordType,
            RecordId = recordId,
            FromStatus = fromStatus.Length > 50 ? fromStatus[..50] : fromStatus,
            ToStatus = toStatus.Length > 50 ? toStatus[..50] : toStatus,
            ChangedByUserId = changedByUserId,
            Note = sanitizedNote,
            ChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.StatusHistories.Add(historyEntry);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StatusHistory>> GetHistoryAsync(StatusRecordType recordType, int recordId)
    {
        if (recordId <= 0)
        {
            return Enumerable.Empty<StatusHistory>();
        }

        // Chronologically ordered audit trail (oldest to newest for progression display)
        var history = await _context.StatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.RecordType == recordType && h.RecordId == recordId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        return history;
    }
}
