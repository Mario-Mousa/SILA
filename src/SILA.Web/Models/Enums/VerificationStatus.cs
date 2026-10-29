namespace SILA.Web.Models.Enums;

/// <summary>
/// Tracks account verification progress by Admin.
/// Unverified -> PendingReview -> Verified or Rejected.
/// </summary>
public enum VerificationStatus
{
    Unverified = 1,
    PendingReview = 2,
    Verified = 3,
    Rejected = 4
}
