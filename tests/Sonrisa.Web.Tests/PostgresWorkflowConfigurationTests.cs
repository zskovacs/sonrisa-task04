using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresWorkflowConfigurationTests
{
    [PostgresFact]
    public async Task Exact_workflow_query_returns_one_event_and_enabled_alerts_from_all_owners()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var firstOwner = new UserNotificationSettings { Id = Guid.NewGuid(), Revision = Guid.NewGuid(), SlackDestination = "C123ABC" };
        var secondOwner = new UserNotificationSettings { Id = Guid.NewGuid(), Revision = Guid.NewGuid(), EmailDestination = "other@example.test" };
        db.Users.AddRange(firstOwner, secondOwner);
        var firstAlert = NewAlert(firstOwner, enabled: true);
        var secondAlert = NewAlert(secondOwner, enabled: true);
        var disabledAlert = NewAlert(secondOwner, enabled: false);
        db.Alerts.AddRange(firstAlert, secondAlert, disabledAlert);
        await db.SaveChangesAsync();

        using var result = await ExecuteExactQuery(db, """{"contract_version":1,"source":"demo.usgs","external_id":"test","event_type":"earthquake","data":{"magnitude":5}}""");
        Assert.Equal("test", result.RootElement.GetProperty("event").GetProperty("external_id").GetString());
        var alerts = result.RootElement.GetProperty("alerts").EnumerateArray().ToArray();
        Assert.Contains(alerts, a => a.GetProperty("id").GetGuid() == firstAlert.Id && a.GetProperty("slack_destination").GetString() == "C123ABC");
        Assert.Contains(alerts, a => a.GetProperty("id").GetGuid() == secondAlert.Id && a.GetProperty("email_destination").GetString() == "other@example.test");
        Assert.DoesNotContain(alerts, a => a.GetProperty("id").GetGuid() == disabledAlert.Id);
    }

    [PostgresFact]
    public async Task Exact_workflow_query_preserves_event_and_returns_empty_alerts_for_unsupported_event_type()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = new UserNotificationSettings { Id = Guid.NewGuid(), Revision = Guid.NewGuid(), SlackDestination = "C123ABC" };
        db.Users.Add(owner);
        var disabledAlert = NewAlert(owner, enabled: false);
        db.Alerts.Add(disabledAlert);
        await db.SaveChangesAsync();

        using var result = await ExecuteExactQuery(db, """{"contract_version":1,"source":"demo.usgs","external_id":"empty","event_type":"unsupported","data":{"magnitude":5}}""");
        Assert.Equal("empty", result.RootElement.GetProperty("event").GetProperty("external_id").GetString());
        Assert.Empty(result.RootElement.GetProperty("alerts").EnumerateArray());
    }

    private static Alert NewAlert(UserNotificationSettings owner, bool enabled) => new()
    {
        Id = Guid.NewGuid(), Owner = owner, OwnerId = owner.Id, Revision = Guid.NewGuid(),
        Name = "Workflow query test", Enabled = enabled, ConditionValue = 5,
    };

    private static async Task<JsonDocument> ExecuteExactQuery(DbContext db, string eventJson)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "n8n", "sql", "select-enabled-alerts.sql"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Cannot find checked workflow SQL.");
        var sql = await File.ReadAllTextAsync(Path.Combine(directory.FullName, "n8n", "sql", "select-enabled-alerts.sql"));
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand(sql, connection, (NpgsqlTransaction)db.Database.CurrentTransaction!.GetDbTransaction());
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, Value = eventJson });
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult);
        Assert.True(await reader.ReadAsync());
        var result = JsonDocument.Parse($"{{\"event\":{reader.GetString(0)},\"alerts\":{reader.GetString(1)}}}");
        Assert.False(await reader.ReadAsync());
        return result;
    }
}
