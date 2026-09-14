using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sonrisa.Web.Data;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresRuntimeTests
{
    [PostgresFact]
    public async Task Duplicate_source_identity_preserves_the_first_magnitude()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var firstId = Guid.NewGuid();
        var externalId = $"fixture-{Guid.NewGuid():N}";
        Assert.Equal(1, await InsertEvent(db, firstId, externalId, "{\"magnitude\":4.2}"));
        Assert.Equal(0, await InsertEvent(db, Guid.NewGuid(), externalId, "{\"magnitude\":8.1}"));
        var magnitude = await db.Database.SqlQueryRaw<string>(
            "SELECT data->>'magnitude' AS \"Value\" FROM public.source_events WHERE id = {0}", firstId).SingleAsync();
        Assert.Equal("4.2", magnitude);
    }

    [PostgresFact]
    public async Task Invalid_json_and_status_cannot_enter_source_events()
    {
        foreach (var json in new[] { "{}", "[]", "{\"magnitude\":null}", "{\"magnitude\":\"5\"}",
                     "{\"magnitude\":5,\"other\":1}" })
        {
            await using var db = await PostgresServiceTests.OpenVerifiedContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => InsertEvent(db, Guid.NewGuid(), $"fixture-{Guid.NewGuid():N}", json));
            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Equal("ck_source_events_data", exception.ConstraintName);
        }

        await using (var db = await PostgresServiceTests.OpenVerifiedContext())
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => InsertEvent(
                db, Guid.NewGuid(), $"fixture-{Guid.NewGuid():N}", "{\"magnitude\":5}", "unknown"));
            Assert.Equal("ck_source_events_status", exception.ConstraintName);
        }
    }

    [PostgresFact]
    public async Task Intent_identity_preserves_destination_and_slack_claim_is_single_use()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = Guid.NewGuid();
        var alert = Guid.NewGuid();
        var sourceEvent = Guid.NewGuid();
        var delivery = Guid.NewGuid();
        await InsertConfiguration(db, owner, alert);
        await InsertEvent(db, sourceEvent, $"fixture-{Guid.NewGuid():N}", "{\"magnitude\":5}");
        Assert.Equal(1, await InsertDelivery(db, delivery, sourceEvent, alert, "slack", "C12345678", "pending"));
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE public.users SET slack_destination = {"C87654321"} WHERE id = {owner}");
        Assert.Equal(0, await InsertDelivery(db, Guid.NewGuid(), sourceEvent, alert, "slack", "C87654321", "pending"));
        var destination = await db.Database.SqlQueryRaw<string>(
            "SELECT destination AS \"Value\" FROM public.notification_deliveries WHERE id = {0}", delivery).SingleAsync();
        Assert.Equal("C12345678", destination);
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.notification_deliveries SET status = 'processing', last_error = NULL
            WHERE id = {delivery} AND channel = 'slack' AND status = 'pending'
            """));
        Assert.Equal(0, await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.notification_deliveries SET status = 'processing', last_error = NULL
            WHERE id = {delivery} AND channel = 'slack' AND status = 'pending'
            """));
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.notification_deliveries SET status = 'sent', sent_at = now()
            WHERE id = {delivery} AND status = 'processing'
            """));
    }

    [PostgresFact]
    public async Task Intent_checks_reject_invalid_status_and_missing_sent_timestamp()
    {
        foreach (var (channel, destination, status, error, constraint) in new[]
        {
            ("email", "person@example.test", "unsupported", (string?)null, "ck_notification_deliveries_status"),
            ("slack", "bad channel", "pending", (string?)null, "ck_notification_deliveries_destination"),
            ("slack", "C12345678", "sent", (string?)null, "ck_notification_deliveries_status"),
            ("email", "person@example.test", "sent", (string?)null, "ck_notification_deliveries_status")
        })
        {
            await using var db = await PostgresServiceTests.OpenVerifiedContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var owner = Guid.NewGuid();
            var alert = Guid.NewGuid();
            var sourceEvent = Guid.NewGuid();
            await InsertConfiguration(db, owner, alert);
            await InsertEvent(db, sourceEvent, $"fixture-{Guid.NewGuid():N}", "{\"magnitude\":5}");
            var exception = await Assert.ThrowsAsync<PostgresException>(() => InsertDelivery(
                db, Guid.NewGuid(), sourceEvent, alert, channel, destination, status, error));
            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Equal(constraint, exception.ConstraintName);
        }
    }

    [PostgresFact]
    public async Task Email_active_states_are_legal_and_legacy_unsupported_remains_unclaimable()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = Guid.NewGuid();
        var alert = Guid.NewGuid();
        var sourceEvent = Guid.NewGuid();
        await InsertConfiguration(db, owner, alert);
        await InsertEvent(db, sourceEvent, $"fixture-{Guid.NewGuid():N}", "{\"magnitude\":5}");
        var active = Guid.NewGuid();
        Assert.Equal(1, await InsertDelivery(db, active, sourceEvent, alert, "email", "person@example.test", "pending"));
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.notification_deliveries SET status = 'processing'
            WHERE id = {active} AND channel = 'email' AND status = 'pending'
            """));
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.notification_deliveries SET last_error = 'delivery_outcome_unknown'
            WHERE id = {active} AND channel = 'email' AND status = 'processing'
            """));
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.notification_deliveries SET status = 'sent', sent_at = now(), last_error = NULL
            WHERE id = {active} AND channel = 'email' AND status = 'processing'
            """));
        Assert.Equal("sent", await db.Database.SqlQueryRaw<string>(
            "SELECT status AS \"Value\" FROM public.notification_deliveries WHERE id = {0}", active).SingleAsync());

        var legacyEvent = Guid.NewGuid();
        await InsertEvent(db, legacyEvent, $"fixture-{Guid.NewGuid():N}", "{\"magnitude\":5}");
        var legacy = Guid.NewGuid();
        Assert.Equal(1, await InsertDelivery(db, legacy, legacyEvent, alert, "email", "person@example.test",
            "unsupported", "transport_not_implemented"));
        Assert.Equal(0, await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.notification_deliveries SET status = 'processing'
            WHERE id = {legacy} AND channel IN ('slack', 'email') AND status = 'pending'
            """));
        Assert.Equal("unsupported", await db.Database.SqlQueryRaw<string>(
            "SELECT status AS \"Value\" FROM public.notification_deliveries WHERE id = {0}", legacy).SingleAsync());
    }

    [PostgresFact]
    public async Task Delivery_foreign_keys_restrict_deleting_the_source_event()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = Guid.NewGuid();
        var alert = Guid.NewGuid();
        var sourceEvent = Guid.NewGuid();
        await InsertConfiguration(db, owner, alert);
        await InsertEvent(db, sourceEvent, $"fixture-{Guid.NewGuid():N}", "{\"magnitude\":5}");
        await InsertDelivery(db, Guid.NewGuid(), sourceEvent, alert, "email", "person@example.test", "unsupported",
            "transport_not_implemented");
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM public.source_events WHERE id = {sourceEvent}"));
        Assert.Equal(PostgresErrorCodes.RestrictViolation, exception.SqlState);
    }

    [PostgresFact]
    public async Task Delivery_foreign_keys_restrict_deleting_the_alert()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = Guid.NewGuid();
        var alert = Guid.NewGuid();
        var sourceEvent = Guid.NewGuid();
        await InsertConfiguration(db, owner, alert);
        await InsertEvent(db, sourceEvent, $"fixture-{Guid.NewGuid():N}", "{\"magnitude\":5}");
        await InsertDelivery(db, Guid.NewGuid(), sourceEvent, alert, "email", "person@example.test", "unsupported",
            "transport_not_implemented");
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM public.alerts WHERE id = {alert}"));
        Assert.Equal(PostgresErrorCodes.RestrictViolation, exception.SqlState);
    }

    private static Task<int> InsertEvent(AppDbContext db, Guid id, string externalId, string json,
        string status = "pending") => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.source_events
                (id, contract_version, source, external_id, event_type, occurred_at, title, data, processing_status)
            VALUES ({id}, {1}, {"demo.usgs"}, {externalId}, {"earthquake"}, {DateTimeOffset.UtcNow},
                    {"Fixture earthquake"}, {json}::jsonb, {status})
            ON CONFLICT (source, external_id) DO NOTHING
            """);

    private static async Task InsertConfiguration(AppDbContext db, Guid owner, Guid alert)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.users (id, email_destination, slack_destination, revision)
            VALUES ({owner}, {"person@example.test"}, {"C12345678"}, {Guid.NewGuid()})
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.alerts (id, owner_id, name, event_type, revision,
                condition_field, condition_operator, condition_value_type, condition_value)
            VALUES ({alert}, {owner}, {"Fixture alert"}, {"earthquake"}, {Guid.NewGuid()},
                {"magnitude"}, {"gte"}, {"number"}, {5.0})
            """);
    }

    private static Task<int> InsertDelivery(AppDbContext db, Guid id, Guid sourceEvent, Guid alert,
        string channel, string destination, string status, string? error = null) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.notification_deliveries
                (id, source_event_id, alert_id, channel, destination, status, last_error)
            VALUES ({id}, {sourceEvent}, {alert}, {channel}, {destination}, {status}, {error})
            ON CONFLICT (source_event_id, alert_id, channel) DO NOTHING
            """);
}
