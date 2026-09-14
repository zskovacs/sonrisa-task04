using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Sonrisa.Web.Data.Migrations;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresMigrationConversionTests
{
    [PostgresFact]
    public async Task Identical_legacy_sets_copy_to_one_shared_profile()
    {
        await using var db = await PrepareTemporaryTables();
        var owner = Guid.NewGuid();
        await AddAlert(db, owner, "email", "person@example.test");
        await AddAlert(db, owner, "email", "person@example.test");
        await RunConversionStatements(db);
        var profiles = await db.Database.SqlQueryRaw<string>(
            "SELECT email_destination AS \"Value\" FROM pg_temp.users").ToListAsync();
        Assert.Equal(["person@example.test"], profiles);
    }

    [PostgresFact]
    public async Task Differing_values_abort_with_fixed_message()
    {
        await AssertRejected([("email", "person@example.test"), ("email", "other@example.test")]);
    }

    [PostgresFact]
    public async Task Differing_channel_presence_aborts_with_fixed_message()
    {
        await using var db = await PrepareTemporaryTables();
        var owner = Guid.NewGuid();
        await AddAlert(db, owner, "email", "person@example.test");
        var second = await AddAlert(db, owner, "email", "person@example.test");
        var type = "slack";
        var destination = "C1";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO pg_temp.alert_channels VALUES ({second}, {type}, {destination})");
        await AssertFixedFailure(db);
    }

    [PostgresFact]
    public async Task Alert_without_channels_aborts_with_fixed_message()
    {
        await using var db = await PrepareTemporaryTables();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO pg_temp.alerts VALUES ({Guid.NewGuid()}, {Guid.NewGuid()})");
        await AssertFixedFailure(db);
    }

    [PostgresFact]
    public async Task Unsupported_legacy_slack_value_aborts_with_fixed_message()
    {
        await AssertRejected([("slack", "invalid"), ("slack", "invalid")]);
    }

    private static async Task AssertRejected((string Type, string Value)[] channels)
    {
        await using var db = await PrepareTemporaryTables();
        var owner = Guid.NewGuid();
        foreach (var channel in channels)
            await AddAlert(db, owner, channel.Type, channel.Value);
        await AssertFixedFailure(db);
    }

    private static async Task AssertFixedFailure(Data.AppDbContext db)
    {
        var exception = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => RunConversionStatements(db));
        Assert.Equal("Legacy alert destinations cannot be converted safely.", exception.MessageText);
    }

    private static async Task<Guid> AddAlert(Data.AppDbContext db, Guid owner, string type, string destination)
    {
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO pg_temp.alerts VALUES ({id}, {owner})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO pg_temp.alert_channels VALUES ({id}, {type}, {destination})");
        return id;
    }

    private static async Task<Data.AppDbContext> PrepareTemporaryTables()
    {
        var db = await PostgresServiceTests.OpenVerifiedContext();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TEMP TABLE alerts (id uuid PRIMARY KEY, owner_id uuid NOT NULL)");
        await db.Database.ExecuteSqlRawAsync("CREATE TEMP TABLE alert_channels (alert_id uuid, channel_type text, destination text)");
        await db.Database.ExecuteSqlRawAsync("CREATE TEMP TABLE users (id uuid PRIMARY KEY, email_destination varchar(254), slack_destination varchar(80), revision uuid NOT NULL)");
        return db;
    }

    private static async Task RunConversionStatements(Data.AppDbContext db)
    {
        var operations = new SharedUserNotificationDestinations().UpOperations.OfType<SqlOperation>().ToArray();
        foreach (var operation in operations.Skip(1))
            await db.Database.ExecuteSqlRawAsync(operation.Sql.Replace("public.", "pg_temp.", StringComparison.Ordinal));
    }
}
