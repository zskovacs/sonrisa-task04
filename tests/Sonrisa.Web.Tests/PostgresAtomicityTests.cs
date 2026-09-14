using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Data;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresAtomicityTests
{
    [PostgresFact]
    public async Task Failed_profile_write_preserves_old_snapshot_and_successful_edit_is_atomic()
    {
        var owner = new PostgresServiceTests.TestOwner(Guid.NewGuid());
        try
        {
            await using var initial = await OpenContext();
            var created = await PostgresServiceTests.Settings(initial, owner).SaveAsync(new UserNotificationSettingsInput
            { EmailDestination = "person@example.test" });
            Assert.Equal(AlertStatus.Success, created.Status);
            var original = created.Value!;

            await using (var failing = await OpenContext(new InvalidProfileInterceptor()))
            {
                var result = await PostgresServiceTests.Settings(failing, owner).SaveAsync(new UserNotificationSettingsInput
                { Revision = original.Revision, SlackDestination = "C12345678" });
                Assert.Equal(AlertStatus.Unavailable, result.Status);
            }
            await AssertOriginal(owner, original);

            await using (var updating = await OpenContext())
            await using (var transaction = await updating.Database.BeginTransactionAsync())
            {
                var result = await PostgresServiceTests.Settings(updating, owner).SaveAsync(new UserNotificationSettingsInput
                { Revision = original.Revision, SlackDestination = "C12345678" });
                Assert.Equal(AlertStatus.Success, result.Status);
                await AssertOriginal(owner, original);
                await transaction.CommitAsync();
            }
            await using var committed = await OpenContext();
            var final = (await PostgresServiceTests.Settings(committed, owner).GetAsync()).Value!;
            Assert.Null(final.EmailDestination);
            Assert.Equal("C12345678", final.SlackDestination);
            Assert.NotEqual(original.Revision, final.Revision);
        }
        finally
        {
            await using var cleanup = await OpenContext();
            await cleanup.Alerts.Where(a => a.OwnerId == owner.OwnerId).ExecuteDeleteAsync();
            await cleanup.Users.Where(u => u.Id == owner.OwnerId).ExecuteDeleteAsync();
        }
    }

    [PostgresFact]
    public async Task Duplicate_first_profile_save_returns_conflict()
    {
        var owner = new PostgresServiceTests.TestOwner(Guid.NewGuid());
        try
        {
            await using var racing = await OpenContext(new CompetingFirstSaveInterceptor(owner));
            var result = await PostgresServiceTests.Settings(racing, owner).SaveAsync(
                new UserNotificationSettingsInput { EmailDestination = "loser@example.test" });
            Assert.Equal(AlertStatus.Conflict, result.Status);
            await using var observer = await OpenContext();
            var winner = (await PostgresServiceTests.Settings(observer, owner).GetAsync()).Value!;
            Assert.Equal("winner@example.test", winner.EmailDestination);
        }
        finally
        {
            await using var cleanup = await OpenContext();
            await cleanup.Users.Where(u => u.Id == owner.OwnerId).ExecuteDeleteAsync();
        }
    }

    private static async Task AssertOriginal(PostgresServiceTests.TestOwner owner, UserNotificationSettingsDetails original)
    {
        await using var observer = await OpenContext();
        var actual = await PostgresServiceTests.Settings(observer, owner).GetAsync();
        Assert.Equal(AlertStatus.Success, actual.Status);
        Assert.Equal(original, actual.Value);
    }

    private static async Task<AppDbContext> OpenContext(params IInterceptor[] interceptors)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE"))
            .AddInterceptors(interceptors).Options);
        try
        {
            var name = await db.Database.SqlQueryRaw<string>("SELECT current_database() AS \"Value\"").SingleAsync();
            if (name != Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE_NAME"))
                throw new InvalidOperationException("SONRISA_TEST_DATABASE_NAME does not match current_database().");
            return db;
        }
        catch { await db.DisposeAsync(); throw; }
    }

    private sealed class InvalidProfileInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<UserNotificationSettings>())
                entry.Entity.SlackDestination = "invalid";
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CompetingFirstSaveInterceptor(PostgresServiceTests.TestOwner owner) : SaveChangesInterceptor
    {
        private bool called;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!called)
            {
                called = true;
                await using var competitor = await OpenContext();
                var winner = await PostgresServiceTests.Settings(competitor, owner).SaveAsync(
                    new UserNotificationSettingsInput { EmailDestination = "winner@example.test" }, cancellationToken);
                Assert.Equal(AlertStatus.Success, winner.Status);
            }
            return result;
        }
    }
}
