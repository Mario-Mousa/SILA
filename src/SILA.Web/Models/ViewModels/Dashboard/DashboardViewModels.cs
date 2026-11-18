// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.4 | Owner: P4 | Status: DONE
// What exists: ViewModels for live role dashboards and the unified Sent/Received hub
// What is left: none
// Depends on:  T1.1 (Organization, ManufacturingRequest, MaterialOrder, Enums)
// Used by:     DashboardController, Views/Dashboard/*
// How to test: Visit /Dashboard and /Requests as Brand, Factory, or Supplier; verify live metrics and tabs
// Notes:       Part 7.1: ViewModels used exclusively for views, never raw entities
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;

namespace SILA.Web.Models.ViewModels.Dashboard;

/// <summary>
/// Unified representation of a single workflow item (either a ManufacturingRequest or a MaterialOrder)
/// for display in the Sent/Received Hub and Recent Activity widgets.
/// </summary>
public class SentReceivedItemViewModel
{
    public int Id { get; set; }

    /// <summary>
    /// Distinguishes workflow: "ManufacturingRequest" or "MaterialOrder".
    /// </summary>
    public string WorkflowType { get; set; } = string.Empty;

    /// <summary>
    /// User-facing label: "Manufacturing Request" or "Material Order".
    /// </summary>
    public string WorkflowTypeLabel => WorkflowType == "ManufacturingRequest" ? "Manufacturing Request" : "Material Order";

    /// <summary>
    /// Short summary title (e.g. product name or order identifier).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Role of the other party (e.g. "Target Factory", "Client Brand", "Supplier", "Buyer").
    /// </summary>
    public string CounterpartyRole { get; set; } = string.Empty;

    /// <summary>
    /// Business name of the opposite organization.
    /// </summary>
    public string CounterpartyName { get; set; } = string.Empty;

    /// <summary>
    /// Submission or placement timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Current workflow status enum value as string.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Formatted price or total in EGP (e.g. "250.00 EGP / unit" or "15,000.00 EGP").
    /// </summary>
    public string FormattedAmount { get; set; } = string.Empty;

    /// <summary>
    /// Direct link to the details page of the record.
    /// </summary>
    public string DetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating whether the current organization must take action (e.g. counter-offer, review).
    /// </summary>
    public bool RequiresAction { get; set; }

    /// <summary>
    /// Prompt text when action is required.
    /// </summary>
    public string? ActionPrompt { get; set; }
}

/// <summary>
/// ViewModel for the unified Sent/Received hub page (Requests page) featuring distinct Sent and Received tabs.
/// </summary>
public class SentReceivedHubViewModel
{
    public string ActiveTab { get; set; } = "sent";
    public Organization? Organization { get; set; }
    public string UserRole { get; set; } = string.Empty;

    public List<SentReceivedItemViewModel> SentItems { get; set; } = new();
    public List<SentReceivedItemViewModel> ReceivedItems { get; set; } = new();

    public int SentCount => SentItems.Count;
    public int ReceivedCount => ReceivedItems.Count;
    public int ActionRequiredCount => SentItems.Count(i => i.RequiresAction) + ReceivedItems.Count(i => i.RequiresAction);
}

/// <summary>
/// ViewModel for live role dashboards with live metrics, quick actions, and recent activity widget.
/// </summary>
public class RoleDashboardViewModel
{
    public Organization? Organization { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Card 1
    public int Metric1Value { get; set; }
    public string Metric1Label { get; set; } = string.Empty;
    public string Metric1Subtext { get; set; } = string.Empty;
    public string Metric1StatusBadge { get; set; } = string.Empty;

    // Card 2
    public int Metric2Value { get; set; }
    public string Metric2Label { get; set; } = string.Empty;
    public string Metric2Subtext { get; set; } = string.Empty;
    public string Metric2StatusBadge { get; set; } = string.Empty;

    // Card 3
    public int Metric3Value { get; set; }
    public string Metric3Label { get; set; } = string.Empty;
    public string Metric3Subtext { get; set; } = string.Empty;
    public string Metric3StatusBadge { get; set; } = string.Empty;

    // Notification unread count
    public int UnreadNotificationsCount { get; set; }

    // Recent Activity widget (latest 5 items)
    public List<SentReceivedItemViewModel> RecentActivity { get; set; } = new();
}

