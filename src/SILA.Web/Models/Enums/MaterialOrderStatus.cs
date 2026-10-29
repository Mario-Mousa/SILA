namespace SILA.Web.Models.Enums;

/// <summary>
/// Lifecycle status of a Material Order placed by a Buyer (Brand or Factory) to a Supplier.
/// Never shared with or mixed into Manufacturing Request workflow.
/// </summary>
public enum MaterialOrderStatus
{
    PendingReview = 1,
    Confirmed = 2,
    Rejected = 3,
    Dispatched = 4,
    Delivered = 5,
    Cancelled = 6
}
