// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.4 | Owner: P4 | Status: DONE
// What exists: Comprehensive DbSeeder provisioning Identity roles, Admin, >=2 verified organizations per role,
//               >=50 realistic Egyptian textile material listings, sample manufacturing requests with negotiation rounds,
//               sample material orders with line snapshots and stock movements, status history records, and notifications.
// What is left: none
// Depends on:  T1.1 (ApplicationDbContext, Entities, Enums), T1.2 (Identity, Roles)
// Used by:     Program.cs (runs on application startup in Development mode)
// How to test: Wipe local DB / update database; run application; verify AspNetUsers, Organizations,
//              MaterialListings (>=50), ManufacturingRequests, and MaterialOrders tables are populated
// Notes:       Idempotent execution: checks if entities already exist prior to insertion
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Data.Seed;

public static class DbSeeder
{
    public const string RoleAdmin = "Admin";
    public const string RoleBrand = "Brand";
    public const string RoleFactory = "Factory";
    public const string RoleSupplier = "Supplier";

    public static readonly string[] Roles = [RoleAdmin, RoleBrand, RoleFactory, RoleSupplier];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        try
        {
            // ────────────────────────────────────────────────────────────────
            // 1. Seed Roles
            // ────────────────────────────────────────────────────────────────
            foreach (var roleName in Roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Seeded Identity role: {Role}", roleName);
                    }
                }
            }

            // ────────────────────────────────────────────────────────────────
            // 2. Seed Default Admin User (D-02)
            // ────────────────────────────────────────────────────────────────
            const string adminEmail = "admin@sila.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "SILA System Administrator",
                    EmailConfirmed = true,
                    PhoneNumber = "01000000000"
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, RoleAdmin);
                    logger.LogInformation("Seeded default Admin user: {Email}", adminEmail);
                }
            }

            // ────────────────────────────────────────────────────────────────
            // 3. Seed Organizations & Users (>= 2 per role: Brand, Factory, Supplier)
            // ────────────────────────────────────────────────────────────────
            var (brandOrg1, brandUser1) = await EnsureUserAndOrgAsync(
                userManager, context, "brand1@sila.com", "Brand@123456", "Ahmed Mansour", "01011112222",
                RoleBrand, OrganizationType.Brand, "Nile Chic Apparel", "Cairo",
                instagram: "https://instagram.com/nilechic", portfolio: "Contemporary Egyptian streetwear using premium local cotton.");

            var (brandOrg2, brandUser2) = await EnsureUserAndOrgAsync(
                userManager, context, "brand2@sila.com", "Brand@123456", "Laila El-Gohary", "01033334444",
                RoleBrand, OrganizationType.Brand, "Lotus Haute Couture", "Giza",
                instagram: "https://instagram.com/lotuscouture", portfolio: "Modern evening dresses and tailored blazers from Egyptian linen and wool.");

            var (factoryOrg1, factoryUser1) = await EnsureUserAndOrgAsync(
                userManager, context, "factory1@sila.com", "Factory@123456", "Mahmoud Soliman", "01122223333",
                RoleFactory, OrganizationType.Factory, "Delta Garments & Apparel Co.", "10th of Ramadan",
                address: "Industrial Zone B3, Plot 14", description: "Specialized in denim manufacturing, woven shirts, and outerwear with 500+ sewing stations.");

            var (factoryOrg2, factoryUser2) = await EnsureUserAndOrgAsync(
                userManager, context, "factory2@sila.com", "Factory@123456", "Nader El-Sayed", "01144445555",
                RoleFactory, OrganizationType.Factory, "Alexandria Modern Garments", "Borg El Arab",
                address: "Block 12, Industrial Sector 2", description: "High-capacity factory for circular knits, polo shirts, activewear, and casual trousers.");

            var (supplierOrg1, supplierUser1) = await EnsureUserAndOrgAsync(
                userManager, context, "supplier1@sila.com", "Supplier@123456", "Mostafa Radwan", "01211113333",
                RoleSupplier, OrganizationType.Supplier, "Misr Fine Weaving & Yarns", "El-Mahalla El-Kubra",
                description: "Egypt's premier textile mill producing Giza 45/86 combed cotton, poplin, twill, and jersey.");

            var (supplierOrg2, supplierUser2) = await EnsureUserAndOrgAsync(
                userManager, context, "supplier2@sila.com", "Supplier@123456", "Tarek Abdel-Rahman", "01233335555",
                RoleSupplier, OrganizationType.Supplier, "Al-Ahram Fabrics & Textiles", "Shubra El-Kheima",
                description: "Wholesale distributor specializing in raw denim, linen blends, poly-taffeta, and winter wool.");

            // ────────────────────────────────────────────────────────────────
            // 4. Seed >= 50 Material Listings across various Egyptian fabric categories
            // ────────────────────────────────────────────────────────────────
            if (!await context.MaterialListings.AnyAsync())
            {
                var listings = new List<MaterialListing>();

                // 100% Egyptian Cotton (Supplier 1)
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Giza 86 Combed Cotton Poplin (60s)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 145.00m, AvailableQuantity = 1800, Description = "Ultra-smooth lightweight poplin woven from long-staple Giza 86 cotton. Ideal for luxury business shirts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Giza 45 Luxury Cotton Sateen (100s)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 295.00m, AvailableQuantity = 950, Description = "Finest extra-long staple Egyptian cotton with a subtle lustrous sheen and silky hand feel." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Premium Cotton Oxford Shirting (40s)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 120.00m, AvailableQuantity = 2200, Description = "Classic basketweave Oxford cotton for casual button-down shirts and uniforms." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Heavy Cotton Canvas Duck (12oz)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 175.00m, AvailableQuantity = 1400, Description = "Rugged heavyweight unbleached cotton canvas for workwear jackets, tote bags, and aprons." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Fine Cotton Voile (80s Semi-Sheer)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 95.00m, AvailableQuantity = 1600, Description = "Airy, breathable semi-sheer cotton fabric ideal for summer blouses and scarf linings." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Organic Combed Cotton Batiste (70s)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 135.00m, AvailableQuantity = 1100, Description = "Certified organic soft plain weave batiste, hypoallergenic and gentle for infants." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Structured Cotton Gabardine (280 GSM)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 190.00m, AvailableQuantity = 1300, Description = "Tightly woven steep-twill cotton gabardine for trench coats, tailored skirts, and trousers." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Yarn-Dyed Gingham Cotton (50s)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 110.00m, AvailableQuantity = 1500, Description = "Classic 1/4-inch checkered pattern in navy and white for casual shirting and dresses." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Cotton Chambray (Indigo Tint 5oz)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 130.00m, AvailableQuantity = 2000, Description = "Plain weave fabric featuring white weft and dyed indigo warp for casual shirts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Egyptian Cotton Muslin (Raw Ecru)", Category = "100% Egyptian Cotton", Unit = "Meter", UnitPrice = 65.00m, AvailableQuantity = 3500, Description = "Natural unbleached plain cotton for pattern drafting, draping, and garment prototyping." });

                // Denim & Twill (Supplier 2)
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Raw Indigo Rigid Denim (13.5oz)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 185.00m, AvailableQuantity = 2500, Description = "Pure indigo rope-dyed selvedge-compatible rigid denim for heritage 5-pocket jeans." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Comfort Stretch Indigo Denim (11.5oz, 2% Elastane)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 195.00m, AvailableQuantity = 3000, Description = "High-recovery stretch denim offering premium comfort for skinny and slim-fit trousers." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Heavy Bull Twill (340 GSM - Khaki)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 160.00m, AvailableQuantity = 1750, Description = "Sturdy 3/1 right-hand twill cotton for utility cargo pants, chore jackets, and caps." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Sulfur Black Warp & Weft Denim (12oz)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 175.00m, AvailableQuantity = 1900, Description = "Deep stay-black denim dyed with sulfur dyes for fade-resistant black apparel." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Washed Broken Twill Denim (12.5oz)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 180.00m, AvailableQuantity = 1400, Description = "Zigzag twill weave designed to eliminate leg twisting in washed jeans." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Lightweight Indigo Chambray Denim (7oz)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 140.00m, AvailableQuantity = 2100, Description = "Supple lightweight denim suitable for denim shirts, casual tunics, and summer shorts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Chino Cotton Twill (240 GSM - Navy)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 135.00m, AvailableQuantity = 2800, Description = "Mercerized combed cotton twill with wrinkle-resistant finish for business casual chinos." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Heavyweight Indigo Slub Denim (14.5oz)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 210.00m, AvailableQuantity = 1200, Description = "Rich vintage slub texture for raw denim enthusiasts and trucker jackets." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Crosshatch Stretch Denim (10oz - Blue Grey)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 170.00m, AvailableQuantity = 1600, Description = "Unique crosshatch weave combining horizontal and vertical slub yarns." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Army Green Cotton Herringbone Twill (9oz)", Category = "Denim & Twill", Unit = "Meter", UnitPrice = 150.00m, AvailableQuantity = 1500, Description = "Military-spec chevron twill weave for overshirts, field jackets, and durable shorts." });

                // Linen & Blends (Supplier 1 & 2)
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Pure Egyptian Flax Linen (200 GSM - Natural)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 240.00m, AvailableQuantity = 1300, Description = "100% natural Egyptian flax linen with organic slub texture, moisture-wicking and cool." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Enzyme-Washed Softened Linen (170 GSM)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 260.00m, AvailableQuantity = 1100, Description = "Pre-washed for a buttery soft drape that avoids stiff initial wrinkles. Perfect for dresses." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Cotton-Linen Blend (55% Linen / 45% Cotton)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 175.00m, AvailableQuantity = 2200, Description = "Best of both fibers: the breathability of linen tempered by the softness and crease-resistance of cotton." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Chambray Weave Linen (180 GSM - Sky Blue)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 250.00m, AvailableQuantity = 950, Description = "Yarn-dyed colored warp with white weft producing a subtle, breezy two-tone texture." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Heavy Suiting Linen (300 GSM - Sand Beige)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 285.00m, AvailableQuantity = 850, Description = "Substantial linen weight with excellent structure for unlined summer blazers and trousers." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Linen-Viscose Drape Blend (190 GSM - Olive)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 195.00m, AvailableQuantity = 1400, Description = "Fluid, flowing drape combining crisp flax with smooth viscose for flowing skirts and jumpsuits." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Fine Linen Gauze (110 GSM - Off-White)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 210.00m, AvailableQuantity = 750, Description = "Lightweight open-weave gauze for beachwear, bohemian shirts, and artistic layering." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Striped Cotton-Linen Shirting (150 GSM)", Category = "Linen & Blends", Unit = "Meter", UnitPrice = 185.00m, AvailableQuantity = 1600, Description = "Crisp ticking stripe pattern in warm earth tones for coastal resort collections." });

                // Knits & Circular Jersey (Supplier 1)
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Single Jersey Combed Cotton (180 GSM - White)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 210.00m, AvailableQuantity = 1200, Description = "Classic single-knit 100% Egyptian cotton for high-end graphic tees and basic t-shirts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Heavyweight Compact Jersey (240 GSM - Black)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 240.00m, AvailableQuantity = 1500, Description = "Compact spun anti-pilling jersey with dense structure for streetwear oversized cuts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Cotton-Elastane Single Jersey (95/5 - 200 GSM)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 230.00m, AvailableQuantity = 1800, Description = "Four-way stretch cotton knit with exceptional shape recovery for fitted tops and bodywear." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "French Terry Loopback Cotton (320 GSM)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 265.00m, AvailableQuantity = 1400, Description = "Premium unbrushed loopback knit for hoodies, crewnecks, and athletic sweatpants." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Brushed Fleece Cotton-Poly (360 GSM)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 280.00m, AvailableQuantity = 1300, Description = "Plush brushed back winter fleece providing superior warmth for heavyweight sweatshirts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Interlock Double Knit Cotton (220 GSM)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 250.00m, AvailableQuantity = 1100, Description = "Reversible double-knit with smooth face on both sides. Will not curl at edges." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Cotton Pique Polo Fabric (220 GSM)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 225.00m, AvailableQuantity = 1700, Description = "Geometric honeycomb waffle texture for classic breathable polo shirts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "2x2 Tubular Rib Knit (300 GSM)", Category = "Knits & Jersey", Unit = "Kg", UnitPrice = 220.00m, AvailableQuantity = 900, Description = "Elastic heavy ribbing for cuffs, collars, and waistbands of hoodies and jackets." });

                // Synthetics & Technical (Supplier 2)
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Water-Repellent Poly Microfiber (110 GSM)", Category = "Synthetics & Technical", Unit = "Meter", UnitPrice = 95.00m, AvailableQuantity = 3200, Description = "DWR-coated lightweight microfiber for windbreakers, board shorts, and track jackets." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Ripstop Technical Nylon (70D - Charcoal)", Category = "Synthetics & Technical", Unit = "Meter", UnitPrice = 125.00m, AvailableQuantity = 2100, Description = "Tear-resistant reinforced grid nylon for outdoor gear, cargo vests, and backpacks." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Athletic Breathable Birdseye Mesh (150 GSM)", Category = "Synthetics & Technical", Unit = "Meter", UnitPrice = 85.00m, AvailableQuantity = 2500, Description = "Moisture-wicking pinhole mesh knit for sportswear jerseys, gym apparel, and pocket bags." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Scuba Crepe Double Knit (290 GSM - Burgundy)", Category = "Synthetics & Technical", Unit = "Meter", UnitPrice = 165.00m, AvailableQuantity = 1400, Description = "Structured poly-spandex double knit with elegant crepe texture for cocktail dresses." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Matte Poly Taffeta Lining (60 GSM)", Category = "Synthetics & Technical", Unit = "Meter", UnitPrice = 55.00m, AvailableQuantity = 4500, Description = "Anti-static smooth silky taffeta for interior lining of blazers, coats, and skirts." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Four-Way Stretch Spandex Athletic Knit", Category = "Synthetics & Technical", Unit = "Meter", UnitPrice = 180.00m, AvailableQuantity = 1600, Description = "High-compression nylon/spandex for yoga tights, cycling apparel, and activewear." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Taslan Textured Nylon (130 GSM - Forest Green)", Category = "Synthetics & Technical", Unit = "Meter", UnitPrice = 135.00m, AvailableQuantity = 1900, Description = "Air-textured nylon with soft cotton-like hand feel for casual jackets and track pants." });

                // Wool & Suiting (Supplier 2)
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Fine Worsted Wool Suiting (Super 120s)", Category = "Wool & Suiting", Unit = "Meter", UnitPrice = 450.00m, AvailableQuantity = 600, Description = "Imported Australian Merino wool woven for bespoke business suits and executive blazers." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Wool-Cashmere Overcoating (480 GSM - Camel)", Category = "Wool & Suiting", Unit = "Meter", UnitPrice = 520.00m, AvailableQuantity = 450, Description = "Luxurious blend (90% Wool / 10% Cashmere) offering supreme warmth for winter trench coats." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Flannel Wool Blend (320 GSM - Charcoal Grey)", Category = "Wool & Suiting", Unit = "Meter", UnitPrice = 360.00m, AvailableQuantity = 750, Description = "Lightly brushed woolen flannel with soft matte surface for autumn trousers and jackets." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Tropical Wool Plain Weave (220 GSM)", Category = "Wool & Suiting", Unit = "Meter", UnitPrice = 390.00m, AvailableQuantity = 800, Description = "Breathable porous weave engineered for warm-climate formal suiting and uniforms." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Houndstooth Wool Blend (280 GSM)", Category = "Wool & Suiting", Unit = "Meter", UnitPrice = 340.00m, AvailableQuantity = 700, Description = "Classic black and white houndstooth pattern for stylish blazers, skirts, and caps." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg2.Id, Name = "Wool-Poly Twill Suiting (Easy-Care 260 GSM)", Category = "Wool & Suiting", Unit = "Meter", UnitPrice = 240.00m, AvailableQuantity = 1500, Description = "Durable crease-resistant blend for hospitality and corporate uniform programs." });

                // Silk, Rayon & Viscose (Supplier 1)
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Rayon Challis Printed Drape (120 GSM)", Category = "Silk, Rayon & Viscose", Unit = "Meter", UnitPrice = 110.00m, AvailableQuantity = 2300, Description = "Ultra-soft fluid rayon with vibrant floral prints for flowing maxi dresses and blouses." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Modal Single Jersey (190 GSM - Dusty Rose)", Category = "Silk, Rayon & Viscose", Unit = "Meter", UnitPrice = 160.00m, AvailableQuantity = 1400, Description = "Eco-friendly beechwood modal fibers delivering silky softness for loungewear and tees." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Viscose Crepe de Chine (140 GSM - Black)", Category = "Silk, Rayon & Viscose", Unit = "Meter", UnitPrice = 145.00m, AvailableQuantity = 1900, Description = "Pebbled crepe texture with graceful drape for professional blouses and evening dresses." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Bamboo Viscose Knit (210 GSM - Sage)", Category = "Silk, Rayon & Viscose", Unit = "Meter", UnitPrice = 175.00m, AvailableQuantity = 1100, Description = "Naturally antibacterial, moisture-wicking sustainable knit for intimates and sleepwear." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Silk Satin Charmeuse Blend (95/5 Silk-Spandex)", Category = "Silk, Rayon & Viscose", Unit = "Meter", UnitPrice = 380.00m, AvailableQuantity = 500, Description = "High-shine luxury satin face with comfortable slight stretch for bridal and evening wear." });
                listings.Add(new MaterialListing { SupplierOrganizationId = supplierOrg1.Id, Name = "Washed Cupro Silk-Touch Fabric (135 GSM)", Category = "Silk, Rayon & Viscose", Unit = "Meter", UnitPrice = 220.00m, AvailableQuantity = 850, Description = "Cotton linter regenerated fiber with peach-skin velvety finish and breathable silk feel." });

                context.MaterialListings.AddRange(listings);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} material listings across 7 categories.", listings.Count);
            }

            // ────────────────────────────────────────────────────────────────
            // 5. Seed Sample Manufacturing Requests with Negotiation Rounds (D-01, D-03, D-11)
            // ────────────────────────────────────────────────────────────────
            if (!await context.ManufacturingRequests.AnyAsync())
            {
                // Request 1: Brand 1 -> Factory 1 (PendingApproval)
                var req1 = new ManufacturingRequest
                {
                    BrandOrganizationId = brandOrg1.Id,
                    FactoryOrganizationId = factoryOrg1.Id,
                    ProductType = "Slim-Fit Indigo Denim Jeans",
                    DesignSpecification = "Classic 5-pocket denim pants, copper rivets, contrast tobacco stitching, tapered leg.",
                    MaterialDescription = "13.5oz Raw Indigo Rigid Cotton Denim with heavy pocket lining.",
                    Quantity = 500,
                    CurrentOfferPrice = 220.00m,
                    Notes = "Target delivery within 45 days. Sample piece required prior to bulk cut.",
                    Status = ManufacturingRequestStatus.PendingApproval,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                };
                context.ManufacturingRequests.Add(req1);
                await context.SaveChangesAsync();

                context.RequestOffers.Add(new RequestOffer
                {
                    ManufacturingRequestId = req1.Id,
                    OfferedByOrganizationId = brandOrg1.Id,
                    UnitPrice = 220.00m,
                    Note = "Initial offer for 500 units batch.",
                    RoundNumber = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                });

                // Request 2: Brand 2 -> Factory 1 (CounterOffered - multi-round negotiation)
                var req2 = new ManufacturingRequest
                {
                    BrandOrganizationId = brandOrg2.Id,
                    FactoryOrganizationId = factoryOrg1.Id,
                    ProductType = "Oversized Heavyweight Cotton T-Shirts",
                    DesignSpecification = "Drop-shoulder unisex silhouette, 2.5cm ribbed collar, twin-needle stitching on hem.",
                    MaterialDescription = "240 GSM Compact Combed Egyptian Cotton Jersey.",
                    Quantity = 1000,
                    CurrentOfferPrice = 165.00m,
                    Notes = "Packaging in individual biodegradable polybags.",
                    Status = ManufacturingRequestStatus.CounterOffered,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                };
                context.ManufacturingRequests.Add(req2);
                await context.SaveChangesAsync();

                context.RequestOffers.AddRange(
                    new RequestOffer
                    {
                        ManufacturingRequestId = req2.Id,
                        OfferedByOrganizationId = brandOrg2.Id,
                        UnitPrice = 135.00m,
                        Note = "Initial proposed price for 1000 t-shirts.",
                        RoundNumber = 1,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new RequestOffer
                    {
                        ManufacturingRequestId = req2.Id,
                        OfferedByOrganizationId = factoryOrg1.Id,
                        UnitPrice = 165.00m,
                        Note = "Heavyweight compact knit requires specialized silicon needle cooling and slower machine speed.",
                        RoundNumber = 2,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    });

                // Request 3: Brand 1 -> Factory 2 (Accepted)
                var req3 = new ManufacturingRequest
                {
                    BrandOrganizationId = brandOrg1.Id,
                    FactoryOrganizationId = factoryOrg2.Id,
                    ProductType = "Summer Casual Linen Blazer",
                    DesignSpecification = "Unconstructed two-button notch lapel blazer with patch pockets and horn buttons.",
                    MaterialDescription = "Pure Egyptian Flax Linen (200 GSM) with viscose sleeve lining.",
                    Quantity = 300,
                    CurrentOfferPrice = 385.00m,
                    Status = ManufacturingRequestStatus.Accepted,
                    DecisionNote = "Agreed on 385 EGP per blazer. Production starting next Monday.",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-4)
                };
                context.ManufacturingRequests.Add(req3);
                await context.SaveChangesAsync();

                context.RequestOffers.AddRange(
                    new RequestOffer
                    {
                        ManufacturingRequestId = req3.Id,
                        OfferedByOrganizationId = brandOrg1.Id,
                        UnitPrice = 350.00m,
                        RoundNumber = 1,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new RequestOffer
                    {
                        ManufacturingRequestId = req3.Id,
                        OfferedByOrganizationId = factoryOrg2.Id,
                        UnitPrice = 410.00m,
                        Note = "Flax fraying requires bias-binding on interior seams.",
                        RoundNumber = 2,
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    new RequestOffer
                    {
                        ManufacturingRequestId = req3.Id,
                        OfferedByOrganizationId = brandOrg1.Id,
                        UnitPrice = 385.00m,
                        Note = "Revised compromise offer for volume of 300.",
                        RoundNumber = 3,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    });

                await context.SaveChangesAsync();
                logger.LogInformation("Seeded sample Manufacturing Requests with negotiation rounds.");
            }

            // ────────────────────────────────────────────────────────────────
            // 6. Seed Sample Material Orders & Snapshots (BR-02, BR-04)
            // ────────────────────────────────────────────────────────────────
            if (!await context.MaterialOrders.AnyAsync())
            {
                var poplinListing = await context.MaterialListings.FirstOrDefaultAsync(l => l.Name.Contains("Giza 86 Combed Cotton Poplin"));
                var jerseyListing = await context.MaterialListings.FirstOrDefaultAsync(l => l.Name.Contains("Single Jersey Combed Cotton"));
                var denimListing = await context.MaterialListings.FirstOrDefaultAsync(l => l.Name.Contains("Raw Indigo Rigid Denim"));
                var twillListing = await context.MaterialListings.FirstOrDefaultAsync(l => l.Name.Contains("Heavy Bull Twill"));

                if (poplinListing != null && jerseyListing != null)
                {
                    // Order 1: Buyer (Brand 1) -> Supplier 1 (PendingReview)
                    var order1 = new MaterialOrder
                    {
                        BuyerOrganizationId = brandOrg1.Id,
                        SupplierOrganizationId = supplierOrg1.Id,
                        Status = MaterialOrderStatus.PendingReview,
                        TotalAmount = (200 * poplinListing.UnitPrice) + (150 * jerseyListing.UnitPrice),
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    };
                    context.MaterialOrders.Add(order1);
                    await context.SaveChangesAsync();

                    context.MaterialOrderLines.AddRange(
                        new MaterialOrderLine
                        {
                            MaterialOrderId = order1.Id,
                            MaterialListingId = poplinListing.Id,
                            MaterialNameSnapshot = poplinListing.Name,
                            UnitSnapshot = poplinListing.Unit,
                            UnitPriceSnapshot = poplinListing.UnitPrice,
                            Quantity = 200,
                            LineTotal = 200 * poplinListing.UnitPrice
                        },
                        new MaterialOrderLine
                        {
                            MaterialOrderId = order1.Id,
                            MaterialListingId = jerseyListing.Id,
                            MaterialNameSnapshot = jerseyListing.Name,
                            UnitSnapshot = jerseyListing.Unit,
                            UnitPriceSnapshot = jerseyListing.UnitPrice,
                            Quantity = 150,
                            LineTotal = 150 * jerseyListing.UnitPrice
                        });
                }

                if (denimListing != null && twillListing != null)
                {
                    // Order 2: Buyer (Factory 1) -> Supplier 2 (Confirmed with StockMovements)
                    var order2 = new MaterialOrder
                    {
                        BuyerOrganizationId = factoryOrg1.Id,
                        SupplierOrganizationId = supplierOrg2.Id,
                        Status = MaterialOrderStatus.Confirmed,
                        TotalAmount = (400 * denimListing.UnitPrice) + (300 * twillListing.UnitPrice),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        UpdatedAt = DateTime.UtcNow.AddDays(-3)
                    };
                    context.MaterialOrders.Add(order2);
                    await context.SaveChangesAsync();

                    context.MaterialOrderLines.AddRange(
                        new MaterialOrderLine
                        {
                            MaterialOrderId = order2.Id,
                            MaterialListingId = denimListing.Id,
                            MaterialNameSnapshot = denimListing.Name,
                            UnitSnapshot = denimListing.Unit,
                            UnitPriceSnapshot = denimListing.UnitPrice,
                            Quantity = 400,
                            LineTotal = 400 * denimListing.UnitPrice
                        },
                        new MaterialOrderLine
                        {
                            MaterialOrderId = order2.Id,
                            MaterialListingId = twillListing.Id,
                            MaterialNameSnapshot = twillListing.Name,
                            UnitSnapshot = twillListing.Unit,
                            UnitPriceSnapshot = twillListing.UnitPrice,
                            Quantity = 300,
                            LineTotal = 300 * twillListing.UnitPrice
                        });

                    // StockMovements for confirmed order
                    context.StockMovements.AddRange(
                        new StockMovement
                        {
                            MaterialListingId = denimListing.Id,
                            MaterialOrderId = order2.Id,
                            QuantityChange = -400,
                            CreatedAt = DateTime.UtcNow.AddDays(-3)
                        },
                        new StockMovement
                        {
                            MaterialListingId = twillListing.Id,
                            MaterialOrderId = order2.Id,
                            QuantityChange = -300,
                            CreatedAt = DateTime.UtcNow.AddDays(-3)
                        });
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Seeded sample Material Orders with frozen line snapshots.");
            }

            // ────────────────────────────────────────────────────────────────
            // 7. Seed StatusHistory Audit Trail (BR-08)
            // ────────────────────────────────────────────────────────────────
            if (!await context.StatusHistories.AnyAsync())
            {
                var req = await context.ManufacturingRequests.FirstOrDefaultAsync();
                var ord = await context.MaterialOrders.FirstOrDefaultAsync(o => o.Status == MaterialOrderStatus.Confirmed);

                if (req != null)
                {
                    context.StatusHistories.Add(new StatusHistory
                    {
                        RecordType = StatusRecordType.Request,
                        RecordId = req.Id,
                        FromStatus = "None",
                        ToStatus = ManufacturingRequestStatus.PendingApproval.ToString(),
                        ChangedByUserId = brandUser1.Id,
                        Note = "Manufacturing request submitted with initial offer price.",
                        ChangedAt = req.CreatedAt
                    });
                }

                if (ord != null)
                {
                    context.StatusHistories.AddRange(
                        new StatusHistory
                        {
                            RecordType = StatusRecordType.Order,
                            RecordId = ord.Id,
                            FromStatus = "None",
                            ToStatus = MaterialOrderStatus.PendingReview.ToString(),
                            ChangedByUserId = factoryUser1.Id,
                            Note = "Material order submitted to supplier catalog.",
                            ChangedAt = ord.CreatedAt
                        },
                        new StatusHistory
                        {
                            RecordType = StatusRecordType.Order,
                            RecordId = ord.Id,
                            FromStatus = MaterialOrderStatus.PendingReview.ToString(),
                            ToStatus = MaterialOrderStatus.Confirmed.ToString(),
                            ChangedByUserId = supplierUser2.Id,
                            Note = "Quantities verified in warehouse. Order confirmed and inventory deducted.",
                            ChangedAt = ord.UpdatedAt ?? DateTime.UtcNow
                        });
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Seeded immutable StatusHistory audit records.");
            }

            // ────────────────────────────────────────────────────────────────
            // 8. Seed In-App Notifications (BR-10)
            // ────────────────────────────────────────────────────────────────
            if (!await context.Notifications.AnyAsync())
            {
                context.Notifications.AddRange(
                    new Notification
                    {
                        RecipientUserId = brandUser1.Id,
                        Message = "Welcome to SILA! Your Brand account has been verified by the administrator.",
                        IsRead = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    new Notification
                    {
                        RecipientUserId = factoryUser1.Id,
                        Message = "New Manufacturing Request received from Nile Chic Apparel: 'Slim-Fit Indigo Denim Jeans' (500 units).",
                        LinkUrl = "/Requests",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new Notification
                    {
                        RecipientUserId = supplierUser1.Id,
                        Message = "New Material Order #1 received from Nile Chic Apparel awaiting your review.",
                        LinkUrl = "/Orders",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new Notification
                    {
                        RecipientUserId = brandUser2.Id,
                        Message = "Factory Counter-Offer received for 'Oversized Heavyweight Cotton T-Shirts': 165.00 EGP per unit.",
                        LinkUrl = "/Requests",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    });

                await context.SaveChangesAsync();
                logger.LogInformation("Seeded in-app user notifications.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    /// <summary>
    /// Helper to create an Identity user and linked Organization atomically if not already existing.
    /// </summary>
    private static async Task<(Organization Org, ApplicationUser User)> EnsureUserAndOrgAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        string email,
        string password,
        string fullName,
        string phone,
        string roleName,
        OrganizationType orgType,
        string orgName,
        string city,
        string? address = null,
        string? description = null,
        string? instagram = null,
        string? portfolio = null)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                PhoneNumber = phone,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, roleName);
        }

        var org = await context.Organizations.FirstOrDefaultAsync(o => o.OwnerUserId == user.Id);
        if (org == null)
        {
            org = new Organization
            {
                OwnerUserId = user.Id,
                Type = orgType,
                Name = orgName,
                City = city,
                ContactPhone = phone,
                Address = address,
                Description = description,
                InstagramUrl = instagram,
                PortfolioText = portfolio,
                VerificationStatus = VerificationStatus.Verified,
                VerificationNote = "Verified in development seed.",
                CreatedAt = DateTime.UtcNow.AddDays(-14)
            };

            context.Organizations.Add(org);
            await context.SaveChangesAsync();
        }

        return (org, user);
    }
}
