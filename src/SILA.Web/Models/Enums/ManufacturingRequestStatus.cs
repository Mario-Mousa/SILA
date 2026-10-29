namespace SILA.Web.Models.Enums;

/// <summary>
/// Lifecycle status of a Manufacturing Request sent from a Brand Owner to a Factory Owner.
/// Never shared with or mixed into Material Order workflow.
/// </summary>
public enum ManufacturingRequestStatus
{
    PendingApproval = 1,
    CounterOffered = 2,
    Accepted = 3,
    Rejected = 4,
    Cancelled = 5
}
