// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T4.1 | Owner: P4 (QA Lead) | Status: DONE
// What exists: Shared infrastructure service test suite (TC-SHARED-01 to TC-SHARED-05)
// What is left: // TODO(T4.4): Include QA test suite in final project documentation
// Depends on:  NotificationService, StatusHistoryService, ApplicationDbContext
// Used by:     SILA QA automated verification suite
// How to test: dotnet test --filter FullyQualifiedName~SharedServiceTests
// Notes:       Verifies append-only audit trail (BR-08) and in-app notifications (BR-10)
// ═══════════════════════════════════════════════════════

using Microsoft.EntityFrameworkCore;
using SILA.Tests.Common;
using SILA.Web.Data;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;
using SILA.Web.Services.Implementations;
using SILA.Web.Services.Interfaces;
using Xunit;

namespace SILA.Tests.SharedServices;

public class SharedServiceTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private ApplicationDbContext _context = null!;
    private ApplicationUser _user = null!;

    public SharedServiceTests()
    {
        _connectionString = TestDatabaseHelper.CreateUniqueConnectionString();
    }

    public async Task InitializeAsync()
    {
        _context = await TestDatabaseHelper.CreateDatabaseAsync(_connectionString);

        _user = new ApplicationUser
        {
            Id = "shared-test-user",
            UserName = "shareduser@sila.com",
            Email = "shareduser@sila.com",
            FullName = "Shared Test User",
            EmailConfirmed = true
        };

        _context.Users.Add(_user);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
        await TestDatabaseHelper.DestroyDatabaseAsync(_connectionString);
    }

    [Fact]
    public async Task TC_SHARED_01_NotificationCreation_IncrementsUnreadCount()
    {
        var service = new NotificationService(_context);

        int initialCount = await service.GetUnreadCountAsync(_user.Id);
        Assert.Equal(0, initialCount);

        var notification = await service.CreateNotificationAsync(
            _user.Id,
            "New Material Order #101 placed with your organization.",
            "/Orders/Details/101");

        Assert.NotNull(notification);
        Assert.False(notification.IsRead);

        int newCount = await service.GetUnreadCountAsync(_user.Id);
        Assert.Equal(1, newCount);
    }

    [Fact]
    public async Task TC_SHARED_02_MarkAsRead_DecrementsUnreadCount()
    {
        var service = new NotificationService(_context);

        var notif1 = await service.CreateNotificationAsync(_user.Id, "Notice 1", "/url1");
        var notif2 = await service.CreateNotificationAsync(_user.Id, "Notice 2", "/url2");

        Assert.Equal(2, await service.GetUnreadCountAsync(_user.Id));

        bool marked = await service.MarkAsReadAsync(notif1.Id, _user.Id);
        Assert.True(marked);

        Assert.Equal(1, await service.GetUnreadCountAsync(_user.Id));

        // Attempting to mark a notification belonging to another user must fail
        bool crossUserMark = await service.MarkAsReadAsync(notif2.Id, "foreign-user-id");
        Assert.False(crossUserMark);
    }

    [Fact]
    public async Task TC_SHARED_03_MarkAllAsRead_ResetsUnreadCountToZero()
    {
        var service = new NotificationService(_context);

        await service.CreateNotificationAsync(_user.Id, "Alert A", "/a");
        await service.CreateNotificationAsync(_user.Id, "Alert B", "/b");
        await service.CreateNotificationAsync(_user.Id, "Alert C", "/c");

        Assert.Equal(3, await service.GetUnreadCountAsync(_user.Id));

        await service.MarkAllAsReadAsync(_user.Id);

        Assert.Equal(0, await service.GetUnreadCountAsync(_user.Id));
    }

    [Fact]
    public async Task TC_SHARED_04_StatusHistory_LogsTransitionsChronologically()
    {
        var service = new StatusHistoryService(_context);

        int orderId = 555;

        // Transition 1: New -> PendingReview
        await service.LogHistoryAsync(
            StatusRecordType.Order,
            orderId,
            "New",
            "PendingReview",
            _user.Id,
            "Initial order submission");

        // Transition 2: PendingReview -> Confirmed
        await service.LogHistoryAsync(
            StatusRecordType.Order,
            orderId,
            "PendingReview",
            "Confirmed",
            _user.Id,
            "Stock confirmed and deducted");

        // Transition 3: Confirmed -> Dispatched
        await service.LogHistoryAsync(
            StatusRecordType.Order,
            orderId,
            "Confirmed",
            "Dispatched",
            _user.Id,
            "Dispatched via logistics partner");

        var history = (await service.GetHistoryAsync(StatusRecordType.Order, orderId)).ToList();

        Assert.Equal(3, history.Count);
        Assert.Equal("PendingReview", history[0].ToStatus);
        Assert.Equal("Confirmed", history[1].ToStatus);
        Assert.Equal("Dispatched", history[2].ToStatus);

        // Verify order is chronological
        Assert.True(history[0].ChangedAt <= history[1].ChangedAt);
        Assert.True(history[1].ChangedAt <= history[2].ChangedAt);
    }

    [Fact]
    public void TC_SHARED_05_StatusHistoryService_ProvidesNoMutationOrDeletionMethods()
    {
        // Reflection check: IStatusHistoryService must be strictly append-only (BR-08)
        var methods = typeof(IStatusHistoryService).GetMethods();
        var disallowedMethodNames = new[] { "Update", "Delete", "Remove", "Clear", "Edit", "Purge" };

        foreach (var method in methods)
        {
            foreach (var disallowed in disallowedMethodNames)
            {
                Assert.DoesNotContain(disallowed, method.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
