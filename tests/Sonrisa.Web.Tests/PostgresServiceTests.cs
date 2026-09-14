using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresServiceTests
{
    [PostgresFact]
    public async Task Profile_is_required_and_owner_scopes_alerts_and_settings()
    {
        await using var db = await OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var first = new TestOwner(Guid.NewGuid());
        var other = new TestOwner(Guid.NewGuid());
        var ownAlerts = Alerts(db, first);
        var foreignAlerts = Alerts(db, other);
        var ownSettings = Settings(db, first);
        var foreignSettings = Settings(db, other);

        Assert.Equal(AlertStatus.Invalid, (await ownAlerts.CreateAsync(Input())).Status);
        Assert.Equal(AlertStatus.NotFound, (await ownSettings.GetAsync()).Status);
        Assert.Equal(AlertStatus.Success,
            (await ownSettings.SaveAsync(new UserNotificationSettingsInput { EmailDestination = " person@example.test " })).Status);
        Assert.Equal(AlertStatus.NotFound, (await foreignSettings.GetAsync()).Status);
        var created = await ownAlerts.CreateAsync(Input());
        Assert.Equal(AlertStatus.Success, created.Status);
        Assert.Equal(first.OwnerId, (await db.Alerts.SingleAsync(a => a.Id == created.Value!.Id)).OwnerId);
        Assert.Single((await ownAlerts.ListAsync()).Value!);
        Assert.Empty((await foreignAlerts.ListAsync()).Value!);
        Assert.Equal(AlertStatus.NotFound, (await foreignAlerts.GetAsync(created.Value!.Id)).Status);
        Assert.Equal(AlertStatus.NotFound,
            (await foreignAlerts.UpdateAsync(created.Value.Id, Input(created.Value.Revision))).Status);
        Assert.Equal(AlertStatus.NotFound,
            (await foreignAlerts.SetEnabledAsync(created.Value.Id, created.Value.Revision, true)).Status);
    }

    [PostgresFact]
    public async Task Shared_profile_updates_do_not_change_alert_revision_and_stale_edits_conflict()
    {
        await using var db = await OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = new TestOwner(Guid.NewGuid());
        var settings = Settings(db, owner);
        var first = await settings.SaveAsync(new UserNotificationSettingsInput { EmailDestination = "person@example.test" });
        var alert = await Alerts(db, owner).CreateAsync(Input());
        var edit = await settings.SaveAsync(new UserNotificationSettingsInput
        { Revision = first.Value!.Revision, SlackDestination = "C12345678" });
        Assert.Equal(AlertStatus.Success, edit.Status);
        Assert.Null(edit.Value!.EmailDestination);
        Assert.Equal("C12345678", edit.Value.SlackDestination);
        Assert.Equal(AlertStatus.Conflict, (await settings.SaveAsync(new UserNotificationSettingsInput
        { Revision = first.Value.Revision, EmailDestination = "other@example.test" })).Status);
        Assert.Equal(AlertStatus.Conflict, (await settings.SaveAsync(new UserNotificationSettingsInput
        { EmailDestination = "other@example.test" })).Status);
        Assert.Equal(alert.Value!.Revision, (await Alerts(db, owner).GetAsync(alert.Value.Id)).Value!.Revision);
        var joined = await db.Alerts.Where(a => a.Id == alert.Value.Id)
            .Select(a => new { a.Owner.EmailDestination, a.Owner.SlackDestination }).SingleAsync();
        Assert.Null(joined.EmailDestination);
        Assert.Equal("C12345678", joined.SlackDestination);
    }

    [PostgresFact]
    public async Task Alert_stale_revision_and_status_changes_are_enforced()
    {
        await using var db = await OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = new TestOwner(Guid.NewGuid());
        await Settings(db, owner).SaveAsync(new UserNotificationSettingsInput { EmailDestination = "person@example.test" });
        var service = Alerts(db, owner);
        var created = await service.CreateAsync(Input());
        var updated = await service.UpdateAsync(created.Value!.Id, Input(created.Value.Revision));
        Assert.Equal(AlertStatus.Success, updated.Status);
        Assert.Equal(AlertStatus.Conflict, (await service.UpdateAsync(created.Value.Id, Input(created.Value.Revision))).Status);
        Assert.Equal(AlertStatus.Conflict, (await service.SetEnabledAsync(created.Value.Id, created.Value.Revision, true)).Status);
        var enabled = await service.SetEnabledAsync(created.Value.Id, updated.Value!.Revision, true);
        Assert.Equal(AlertStatus.Success, enabled.Status);
        Assert.True(enabled.Value!.Enabled);
    }

    private static AlertInput Input(Guid revision = default) => new()
    { Name = "Quake", Threshold = "5.5", Revision = revision };

    internal static AlertManagementService Alerts(AppDbContext db, ICurrentOwner owner) => new(
        db, owner, new AlertInputValidator(), NullLogger<AlertManagementService>.Instance);
    internal static UserNotificationSettingsService Settings(AppDbContext db, ICurrentOwner owner) => new(
        db, owner, new UserNotificationSettingsInputValidator(), NullLogger<UserNotificationSettingsService>.Instance);

    internal static async Task<AppDbContext> OpenVerifiedContext()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE")).Options);
        try
        {
            var actual = await db.Database.SqlQueryRaw<string>("SELECT current_database() AS \"Value\"").SingleAsync();
            if (actual != Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE_NAME"))
                throw new InvalidOperationException("SONRISA_TEST_DATABASE_NAME does not match current_database().");
            return db;
        }
        catch { await db.DisposeAsync(); throw; }
    }

    internal sealed record TestOwner(Guid OwnerId) : ICurrentOwner;
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE"))
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE_NAME")))
            Skip = "Requires SONRISA_TEST_DATABASE and SONRISA_TEST_DATABASE_NAME.";
    }
}
