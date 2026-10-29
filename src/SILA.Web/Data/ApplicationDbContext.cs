// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.1 | Owner: P2 | Status: DONE
// What exists: ApplicationDbContext with all DbSets, relationships, decimal precision, check constraints, indexes
// What is left: // TODO(T1.2): Configure Identity options and seed default roles in DbSeeder
// Depends on:  Entities and Enums
// Used by:     T1.2, T2.1, T2.2, T2.4, T3.1, T3.2, T3.3, T3.4
// How to test: dotnet ef database update applies all tables, FKs, and constraints to clean database
// Notes:       Enforces decimal(18,2), AvailableQuantity >= 0 check constraint, RowVersion, Restrict delete behavior
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SILA.Web.Models.Entities;

namespace SILA.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<VerificationDocument> VerificationDocuments => Set<VerificationDocument>();
    public DbSet<MaterialListing> MaterialListings => Set<MaterialListing>();
    public DbSet<MaterialOrder> MaterialOrders => Set<MaterialOrder>();
    public DbSet<MaterialOrderLine> MaterialOrderLines => Set<MaterialOrderLine>();
    public DbSet<ManufacturingRequest> ManufacturingRequests => Set<ManufacturingRequest>();
    public DbSet<RequestOffer> RequestOffers => Set<RequestOffer>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Global delete behavior: Restrict on all foreign keys (Part 12.2)
        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // 2. Global decimal precision: decimal(18,2) for currency and financial figures (Part 6.1)
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // 3. Organization entity configuration
        builder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Name).IsRequired().HasMaxLength(150);
            entity.Property(o => o.City).IsRequired().HasMaxLength(100);
            entity.Property(o => o.ContactPhone).IsRequired().HasMaxLength(30);
            entity.Property(o => o.Description).HasMaxLength(1000);
            entity.Property(o => o.LogoPath).HasMaxLength(300);
            entity.Property(o => o.InstagramUrl).HasMaxLength(300);
            entity.Property(o => o.PortfolioText).HasMaxLength(2000);
            entity.Property(o => o.Address).HasMaxLength(300);
            entity.Property(o => o.VerificationNote).HasMaxLength(500);

            entity.HasIndex(o => o.Type);
            entity.HasIndex(o => o.VerificationStatus);

            entity.HasOne(o => o.OwnerUser)
                .WithOne(u => u.Organization)
                .HasForeignKey<Organization>(o => o.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. VerificationDocument configuration
        builder.Entity<VerificationDocument>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DocumentType).IsRequired().HasMaxLength(50);
            entity.Property(d => d.FilePath).IsRequired().HasMaxLength(500);

            entity.HasOne(d => d.Organization)
                .WithMany(o => o.VerificationDocuments)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 5. MaterialListing configuration
        builder.Entity<MaterialListing>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Name).IsRequired().HasMaxLength(150);
            entity.Property(l => l.Category).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Unit).IsRequired().HasMaxLength(30);
            entity.Property(l => l.Description).HasMaxLength(2000);

            // Concurrency token (RowVersion)
            entity.Property(l => l.RowVersion).IsRowVersion();

            // Check constraint: AvailableQuantity >= 0 (BR-01, Part 9 T1.1)
            entity.ToTable(t => t.HasCheckConstraint("CK_MaterialListing_AvailableQuantity_NonNegative", "[AvailableQuantity] >= 0"));

            entity.HasIndex(l => l.IsActive);
            entity.HasIndex(l => l.Category);

            entity.HasOne(l => l.SupplierOrganization)
                .WithMany(o => o.MaterialListings)
                .HasForeignKey(l => l.SupplierOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 6. MaterialOrder configuration
        builder.Entity<MaterialOrder>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.RejectionReason).HasMaxLength(500);

            entity.HasIndex(o => o.Status);

            entity.HasOne(o => o.BuyerOrganization)
                .WithMany(b => b.BuyerOrders)
                .HasForeignKey(o => o.BuyerOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.SupplierOrganization)
                .WithMany(s => s.SupplierOrders)
                .HasForeignKey(o => o.SupplierOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. MaterialOrderLine configuration
        builder.Entity<MaterialOrderLine>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.MaterialNameSnapshot).IsRequired().HasMaxLength(150);
            entity.Property(l => l.UnitSnapshot).IsRequired().HasMaxLength(30);

            entity.HasOne(l => l.MaterialOrder)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.MaterialOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.MaterialListing)
                .WithMany(m => m.OrderLines)
                .HasForeignKey(l => l.MaterialListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. ManufacturingRequest configuration
        builder.Entity<ManufacturingRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ProductType).IsRequired().HasMaxLength(100);
            entity.Property(r => r.DesignSpecification).IsRequired().HasMaxLength(2000);
            entity.Property(r => r.MaterialDescription).IsRequired().HasMaxLength(2000);
            entity.Property(r => r.AttachmentPath).HasMaxLength(500);
            entity.Property(r => r.Notes).HasMaxLength(1000);
            entity.Property(r => r.DecisionNote).HasMaxLength(1000);

            entity.HasIndex(r => r.Status);

            entity.HasOne(r => r.BrandOrganization)
                .WithMany(b => b.BrandRequests)
                .HasForeignKey(r => r.BrandOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.FactoryOrganization)
                .WithMany(f => f.FactoryRequests)
                .HasForeignKey(r => r.FactoryOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. RequestOffer configuration
        builder.Entity<RequestOffer>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Note).HasMaxLength(500);

            entity.HasOne(o => o.ManufacturingRequest)
                .WithMany(r => r.Offers)
                .HasForeignKey(o => o.ManufacturingRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.OfferedByOrganization)
                .WithMany()
                .HasForeignKey(o => o.OfferedByOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 10. StatusHistory configuration
        builder.Entity<StatusHistory>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.FromStatus).IsRequired().HasMaxLength(50);
            entity.Property(h => h.ToStatus).IsRequired().HasMaxLength(50);
            entity.Property(h => h.Note).HasMaxLength(500);

            entity.HasIndex(h => new { h.RecordType, h.RecordId });

            entity.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. StockMovement configuration
        builder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasOne(s => s.MaterialListing)
                .WithMany(m => m.StockMovements)
                .HasForeignKey(s => s.MaterialListingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.MaterialOrder)
                .WithMany(o => o.StockMovements)
                .HasForeignKey(s => s.MaterialOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 12. Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
            entity.Property(n => n.LinkUrl).HasMaxLength(300);

            entity.HasIndex(n => new { n.RecipientUserId, n.IsRead });

            entity.HasOne(n => n.RecipientUser)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
