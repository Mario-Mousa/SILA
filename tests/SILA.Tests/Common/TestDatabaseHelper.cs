// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T4.1 | Owner: P4 (QA Lead) | Status: DONE
// What exists: Isolated test database factory for integration and concurrency testing on LocalDB
// What is left: // TODO(T4.4): Include QA test suite in final project documentation
// Depends on:  ApplicationDbContext, SQL Server LocalDB
// Used by:     All integration and concurrency tests in SILA.Tests
// How to test: dotnet test
// Notes:       Creates unique database per test run with EnsureCreated, cleans up with EnsureDeleted
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Web.Data;

namespace SILA.Tests.Common;

public static class TestDatabaseHelper
{
    public static string CreateUniqueConnectionString()
    {
        var dbName = $"Sila_Test_{Guid.NewGuid():N}";
        return $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
    }

    public static DbContextOptions<ApplicationDbContext> CreateOptions(string connectionString)
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .EnableSensitiveDataLogging()
            .Options;
    }

    public static async Task<ApplicationDbContext> CreateDatabaseAsync(string connectionString)
    {
        var options = CreateOptions(connectionString);
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public static async Task DestroyDatabaseAsync(string connectionString)
    {
        try
        {
            var options = CreateOptions(connectionString);
            using var context = new ApplicationDbContext(options);
            await context.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Ignore cleanup errors during teardown
        }
    }
}

