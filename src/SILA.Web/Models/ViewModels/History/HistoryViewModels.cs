// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.4 | Owner: P4 | Status: DONE
// What exists: ViewModels for Status History audit log view and chronological transition tracking
// What is left: none
// Depends on:  T1.1 (StatusHistory, StatusRecordType), T2.4 (IStatusHistoryService)
// Used by:     HistoryController, Views/History/*
// How to test: Visit /History; verify chronological entries, actor names, transitions, and notes
// Notes:       BR-08: Immutable audit trails; ViewModels snapshot historic data for view presentation
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.History;

public class StatusHistoryItemViewModel
{
    public int Id { get; set; }
    public StatusRecordType RecordType { get; set; }
    public string RecordTypeLabel => RecordType == StatusRecordType.Request ? "Manufacturing Request" : "Material Order";
    public int RecordId { get; set; }
    public string RecordTitle { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string ChangedByUserName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
    public string DetailsUrl { get; set; } = string.Empty;

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.UtcNow - ChangedAt;
            if (elapsed.TotalMinutes < 1) return "Just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
            return ChangedAt.ToString("dd MMM yyyy HH:mm");
        }
    }
}

public class StatusHistoryIndexViewModel
{
    public List<StatusHistoryItemViewModel> Entries { get; set; } = new();
    public string Filter { get; set; } = "All"; // "All", "Request", "Order"
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
