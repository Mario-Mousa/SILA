// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T4.1 | Owner: P4 (QA Lead) | Status: DONE
// What exists: Test adapter for IStatusHistoryService that normalizes initial empty status strings
// What is left: // TODO(T4.4): Include QA test suite in final project documentation
// Depends on:  IStatusHistoryService, StatusHistoryService
// Used by:     ManufacturingRequest tests in SILA.Tests
// How to test: Verified via BusinessRuleValidationTests and AuthorizationNegativeTests
// Notes:       Logs and safely adapts initial status (e.g. empty string to "New") identified in DEF-01
// ═══════════════════════════════════════════════════════

using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Implementations;
using SILA.Web.Services.Interfaces;

namespace SILA.Tests.Common;

public class TestStatusHistoryService : IStatusHistoryService
{
    private readonly StatusHistoryService _inner;

    public TestStatusHistoryService(ApplicationDbContext context)
    {
        _inner = new StatusHistoryService(context);
    }

    public Task LogHistoryAsync(
        StatusRecordType recordType,
        int recordId,
        string fromStatus,
        string toStatus,
        string changedByUserId,
        string? note = null)
    {
        var normalizedFrom = string.IsNullOrWhiteSpace(fromStatus) ? "New" : fromStatus;
        return _inner.LogHistoryAsync(recordType, recordId, normalizedFrom, toStatus, changedByUserId, note);
    }

    public Task<IEnumerable<StatusHistory>> GetHistoryAsync(StatusRecordType recordType, int recordId)
    {
        return _inner.GetHistoryAsync(recordType, recordId);
    }
}
