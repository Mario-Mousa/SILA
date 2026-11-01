// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: ASP.NET Core Identity, cookie configuration, service DI registrations, DbSeeder trigger
// What is left: // TODO(T2.1): Implement AccountController and login/register endpoints
// Depends on:  T1.1 (ApplicationDbContext, ApplicationUser), T1.2 (Service interfaces & stubs)
// Used by:     All tasks and web runtime
// How to test: dotnet run; verify application starts, database seeds roles & Admin user, and endpoints route
// Notes:       Configures Identity with 4 roles (Brand, Factory, Supplier, Admin); DI for all 8 domain services
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;
using SILA.Web.Data.Seed;
using SILA.Web.Models.Entities;
using SILA.Web.Services.Implementations;
using SILA.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

// 2. Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=SilaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. ASP.NET Core Identity Configuration (Part 5.1, Part 6.1)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User & Sign-in settings
    options.User.RequireUniqueEmail = true;
    // Email confirmation required for live registration; set false in dev if testing without SMTP
    options.SignIn.RequireConfirmedEmail = false; // TODO(T2.1): switch or gate by environment once SMTP is wired
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 4. Authentication Cookie Configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".SILA.Auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 5. Register Services in DI Container (Part 5.4 contracts)
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IOwnershipService, OwnershipService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStatusHistoryService, StatusHistoryService>();
builder.Services.AddScoped<IMaterialListingService, MaterialListingService>();
builder.Services.AddScoped<IMaterialOrderService, MaterialOrderService>();
builder.Services.AddScoped<IManufacturingRequestService, ManufacturingRequestService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// 6. Automatic Role and Admin Seeding in Development
if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services);
}

// 7. Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication must precede Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
