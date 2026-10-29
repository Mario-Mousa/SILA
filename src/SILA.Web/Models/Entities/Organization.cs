// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: Organization entity representing Brand, Factory, or Supplier business profile
// What is left: // TODO(T2.1): Create organization row upon user registration
//               // TODO(T2.2): Implement profile viewing/editing and Admin approval
// Depends on:  ApplicationUser, OrganizationType, VerificationStatus
// Used by:     T1.2, T2.1, T2.2, T2.3, T3.1, T3.2, T3.3, T3.4
// How to test: Database migration creates Organizations table with 1-1 to AspNetUsers
// Notes:       One organization = one user account = exactly one role
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Enums;

namespace SILA.Web.Models.Entities;

public class Organization
{
    public int Id { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationUser OwnerUser { get; set; } = null!;

    public OrganizationType Type { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string City { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? LogoPath { get; set; }

    // Brand-specific fields
    public string? InstagramUrl { get; set; }
    public string? PortfolioText { get; set; }

    // Factory-specific fields
    public string? Address { get; set; }

    // Verification
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
    public string? VerificationNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation collections
    public ICollection<VerificationDocument> VerificationDocuments { get; set; } = new List<VerificationDocument>();
    public ICollection<MaterialListing> MaterialListings { get; set; } = new List<MaterialListing>();
    public ICollection<MaterialOrder> BuyerOrders { get; set; } = new List<MaterialOrder>();
    public ICollection<MaterialOrder> SupplierOrders { get; set; } = new List<MaterialOrder>();
    public ICollection<ManufacturingRequest> BrandRequests { get; set; } = new List<ManufacturingRequest>();
    public ICollection<ManufacturingRequest> FactoryRequests { get; set; } = new List<ManufacturingRequest>();
}
