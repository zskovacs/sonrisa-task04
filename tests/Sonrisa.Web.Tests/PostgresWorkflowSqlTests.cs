using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using Sonrisa.Web.Data;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresWorkflowSqlTests
{
    private static readonly string N8nDirectory = FindN8nDirectory();

    [PostgresFact]
    public async Task Exact_source_insert_keeps_first_normalized_snapshot_and_concurrent_writes_create_one_row()
    {
        await using var db = await Open();
        var firstExternalId = $"sql-{Guid.NewGuid():N}";
        var racingExternalId = $"sql-{Guid.NewGuid():N}";
        try
        {
            Assert.Equal(1, await InsertSource(db, firstExternalId, 4.9));
            Assert.Equal(0, await InsertSource(db, firstExternalId, 5.1));
            Assert.Equal(4.9, await Magnitude(db, firstExternalId));

            async Task<int> Race(double magnitude)
            {
                await using var connection = await Open();
                return await InsertSource(connection, racingExternalId, magnitude);
            }
            var results = await Task.WhenAll(Race(5), Race(5.1));
            Assert.Equal(new[] { 0, 1 }, results.Order());
            Assert.Equal(1L, await Scalar<long>(db,
                "SELECT count(*) FROM public.source_events WHERE source = 'demo.usgs' AND external_id = $1",
                Text(firstExternalId)));
            Assert.Equal(1L, await Scalar<long>(db,
                "SELECT count(*) FROM public.source_events WHERE source = 'demo.usgs' AND external_id = $1",
                Text(racingExternalId)));
        }
        finally
        {
            await Execute(db, """
                DELETE FROM public.notification_deliveries WHERE source_event_id IN
                    (SELECT id FROM public.source_events WHERE source = 'demo.usgs' AND external_id IN ($1, $2))
                """, Text(firstExternalId), Text(racingExternalId));
            await Execute(db, "DELETE FROM public.source_events WHERE source = 'demo.usgs' AND external_id IN ($1, $2)",
                Text(firstExternalId), Text(racingExternalId));
        }
    }

    [PostgresFact]
    public async Task Exact_candidate_and_evaluator_match_across_owners_then_complete_even_with_no_owned_intents()
    {
        await using var db = await Open();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var firstOwner = Guid.NewGuid();
        var secondOwner = Guid.NewGuid();
        var firstAlert = Guid.NewGuid();
        var secondAlert = Guid.NewGuid();
        var disabledAlert = Guid.NewGuid();
        await AddOwner(db, firstOwner, "C11111111", "first@example.test");
        await AddOwner(db, secondOwner, "C22222222", null);
        await AddAlert(db, firstAlert, firstOwner, 5, true);
        await AddAlert(db, secondAlert, secondOwner, 5, true);
        await AddAlert(db, disabledAlert, secondOwner, 4, false);

        foreach (var (magnitude, expectedMatches) in new[] { (4.9, 0), (5.0, 2), (5.1, 2) })
        {
            var externalId = $"sql-{Guid.NewGuid():N}";
            Assert.Equal(1, await InsertSource(db, externalId, magnitude));
            var eventId = await EventId(db, externalId);
            var oldestPendingId = await WorkflowScalar<Guid>(db, "select-pending-event.sql");
            Assert.Equal("pending", await Scalar<string>(db,
                "SELECT processing_status FROM public.source_events WHERE id = $1", Uuid(oldestPendingId)));
            var envelope = await Candidate(db, eventId);
            Assert.Equal(eventId, Guid.Parse(envelope.RootElement.GetProperty("event").GetProperty("id").GetString()!));
            var candidateIds = envelope.RootElement.GetProperty("alerts").EnumerateArray()
                .Select(a => Guid.Parse(a.GetProperty("id").GetString()!)).ToHashSet();
            Assert.Contains(firstAlert, candidateIds);
            Assert.Contains(secondAlert, candidateIds);
            Assert.DoesNotContain(disabledAlert, candidateIds);

            using var evaluated = await RunCode("evaluate-alerts", envelope.RootElement.GetRawText());
            Assert.Equal(eventId, Guid.Parse(evaluated.RootElement.GetProperty("event_id").GetString()!));
            var ownedIntents = evaluated.RootElement.GetProperty("intents").EnumerateArray()
                .Where(i => new[] { firstAlert, secondAlert, disabledAlert }.Contains(Guid.Parse(i.GetProperty("alert_id").GetString()!)))
                .ToArray();
            Assert.Equal(expectedMatches == 0 ? 0 : 3, ownedIntents.Length);
            Assert.DoesNotContain(ownedIntents, i => Guid.Parse(i.GetProperty("alert_id").GetString()!) == disabledAlert);
            var inserted = await WorkflowScalar<int>(db, "insert-delivery-intents.sql", Json(evaluated.RootElement.GetRawText()));
            Assert.True(inserted >= ownedIntents.Length);
            Assert.Equal(ownedIntents.Length, await Scalar<long>(db,
                "SELECT count(*) FROM public.notification_deliveries WHERE source_event_id = $1 AND alert_id IN ($2, $3, $4)",
                Uuid(eventId), Uuid(firstAlert), Uuid(secondAlert), Uuid(disabledAlert)));
            Assert.Equal(eventId, await WorkflowScalar<Guid>(db, "complete-event.sql", Uuid(eventId)));
            Assert.Equal("evaluated", await Scalar<string>(db,
                "SELECT processing_status FROM public.source_events WHERE id = $1", Uuid(eventId)));
            Assert.Equal(0, await WorkflowRows(db, "select-event-candidates.sql", Uuid(eventId)));
        }
    }

    [PostgresFact]
    public async Task Empty_intent_summary_allows_completion_and_malformed_insert_leaves_event_pending()
    {
        await using var db = await Open();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var externalId = $"sql-{Guid.NewGuid():N}";
        Assert.Equal(1, await InsertSource(db, externalId, double.MinValue));
        var eventId = await EventId(db, externalId);
        using var envelope = await Candidate(db, eventId);
        foreach (var alert in envelope.RootElement.GetProperty("alerts").EnumerateArray())
            Assert.True(alert.GetProperty("condition_value").GetDouble() > double.MinValue,
                "No finite magnitude can be below an enabled alert at double.MinValue.");
        using var evaluated = await RunCode("evaluate-alerts", envelope.RootElement.GetRawText());
        Assert.Empty(evaluated.RootElement.GetProperty("intents").EnumerateArray());
        Assert.Equal(0, await WorkflowScalar<int>(db, "insert-delivery-intents.sql", Json(evaluated.RootElement.GetRawText())));
        Assert.Equal("pending", await Scalar<string>(db,
            "SELECT processing_status FROM public.source_events WHERE id = $1", Uuid(eventId)));

        // A savepoint isolates the expected statement error without aborting the fixture transaction.
        await transaction.CreateSavepointAsync("bad_intents");
        await Assert.ThrowsAsync<PostgresException>(() => WorkflowScalar<int>(db,
            "insert-delivery-intents.sql", Json("{")));
        await transaction.RollbackToSavepointAsync("bad_intents");
        Assert.Equal("pending", await Scalar<string>(db,
            "SELECT processing_status FROM public.source_events WHERE id = $1", Uuid(eventId)));
        Assert.Equal(eventId, await WorkflowScalar<Guid>(db, "complete-event.sql", Uuid(eventId)));
    }

    [PostgresFact]
    public async Task Partial_committed_evaluation_replay_keeps_original_destination_and_finishes()
    {
        await using var db = await Open();
        var owner = Guid.NewGuid();
        var alert = Guid.NewGuid();
        var externalId = $"sql-{Guid.NewGuid():N}";
        Guid? eventId = null;
        try
        {
            await AddOwner(db, owner, "C33333333", null);
            await AddAlert(db, alert, owner, 5, true);
            Assert.Equal(1, await InsertSource(db, externalId, 5));
            eventId = await EventId(db, externalId);
            using (var firstEnvelope = await Candidate(db, eventId.Value))
            using (var firstEvaluation = await RunCode("evaluate-alerts", firstEnvelope.RootElement.GetRawText()))
            {
                Assert.True(await WorkflowScalar<int>(db, "insert-delivery-intents.sql",
                    Json(firstEvaluation.RootElement.GetRawText())) >= 1);
                Assert.Equal(0, await WorkflowScalar<int>(db, "insert-delivery-intents.sql",
                    Json(firstEvaluation.RootElement.GetRawText())));
            }
            Assert.Equal("pending", await Scalar<string>(db,
                "SELECT processing_status FROM public.source_events WHERE id = $1", Uuid(eventId.Value)));
            await Execute(db, "UPDATE public.users SET slack_destination = $1 WHERE id = $2",
                Text("C44444444"), Uuid(owner));
            using (var secondEnvelope = await Candidate(db, eventId.Value))
            using (var secondEvaluation = await RunCode("evaluate-alerts", secondEnvelope.RootElement.GetRawText()))
                await WorkflowScalar<int>(db, "insert-delivery-intents.sql",
                    Json(secondEvaluation.RootElement.GetRawText()));
            Assert.Equal("C33333333", await Scalar<string>(db,
                "SELECT destination FROM public.notification_deliveries WHERE source_event_id = $1 AND alert_id = $2 AND channel = 'slack'",
                Uuid(eventId.Value), Uuid(alert)));
            Assert.Equal(1L, await Scalar<long>(db,
                "SELECT count(*) FROM public.notification_deliveries WHERE source_event_id = $1 AND alert_id = $2 AND channel = 'slack'",
                Uuid(eventId.Value), Uuid(alert)));
            Assert.Equal(eventId, await WorkflowScalar<Guid>(db, "complete-event.sql", Uuid(eventId.Value)));
            Assert.Equal(0, await WorkflowRows(db, "complete-event.sql", Uuid(eventId.Value)));
        }
        finally
        {
            await Cleanup(db, eventId, externalId, [alert], [owner]);
        }
    }

    [PostgresFact]
    public async Task Exact_claim_is_single_winner_and_outcome_queries_keep_email_inert()
    {
        await using var db = await Open();
        var owner = Guid.NewGuid();
        var alerts = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var externalId = $"sql-{Guid.NewGuid():N}";
        Guid? eventId = null;
        try
        {
            await AddOwner(db, owner, "C55555555", "claim@example.test");
            foreach (var alert in alerts) await AddAlert(db, alert, owner, 5, true);
            Assert.Equal(1, await InsertSource(db, externalId, 5));
            eventId = await EventId(db, externalId);
            using (var envelope = await Candidate(db, eventId.Value))
            using (var evaluation = await RunCode("evaluate-alerts", envelope.RootElement.GetRawText()))
                Assert.True(await WorkflowScalar<int>(db, "insert-delivery-intents.sql",
                    Json(evaluation.RootElement.GetRawText())) >= 6);
            var slackIds = new List<Guid>();
            foreach (var alert in alerts)
                slackIds.Add(await Scalar<Guid>(db,
                    "SELECT id FROM public.notification_deliveries WHERE source_event_id = $1 AND alert_id = $2 AND channel = 'slack'",
                    Uuid(eventId.Value), Uuid(alert)));
            var emailId = await Scalar<Guid>(db,
                "SELECT id FROM public.notification_deliveries WHERE source_event_id = $1 AND alert_id = $2 AND channel = 'email'",
                Uuid(eventId.Value), Uuid(alerts[0]));

            async Task<int> Claim()
            {
                await using var contender = await Open();
                return await WorkflowRows(contender, "claim-slack-delivery.sql", Uuid(slackIds[0]));
            }
            Assert.Equal(new[] { 0, 1 }, (await Task.WhenAll(Claim(), Claim())).Order());
            Assert.Equal(0, await WorkflowRows(db, "claim-slack-delivery.sql", Uuid(slackIds[0])));
            Assert.Equal(0, await WorkflowRows(db, "claim-slack-delivery.sql", Uuid(emailId)));
            Assert.Equal(1, await WorkflowRows(db, "select-claimed-delivery.sql", Uuid(slackIds[0])));
            Assert.Equal(1, await WorkflowRows(db, "record-slack-sent.sql", Uuid(slackIds[0])));
            Assert.Equal(0, await WorkflowRows(db, "record-slack-sent.sql", Uuid(slackIds[0])));
            Assert.Equal("sent", await Scalar<string>(db,
                "SELECT status FROM public.notification_deliveries WHERE id = $1 AND sent_at IS NOT NULL", Uuid(slackIds[0])));

            Assert.Equal(1, await WorkflowRows(db, "claim-slack-delivery.sql", Uuid(slackIds[1])));
            Assert.Equal(1, await WorkflowRows(db, "record-slack-failed.sql", Uuid(slackIds[1])));
            Assert.Equal("failed:slack_rejected", await Scalar<string>(db,
                "SELECT status || ':' || last_error FROM public.notification_deliveries WHERE id = $1", Uuid(slackIds[1])));
            Assert.Equal(1, await WorkflowRows(db, "claim-slack-delivery.sql", Uuid(slackIds[2])));
            Assert.Equal(1, await WorkflowRows(db, "record-slack-unknown.sql", Uuid(slackIds[2])));
            Assert.Equal("processing:delivery_outcome_unknown", await Scalar<string>(db,
                "SELECT status || ':' || last_error FROM public.notification_deliveries WHERE id = $1", Uuid(slackIds[2])));
            Assert.Equal("unsupported", await Scalar<string>(db,
                "SELECT status FROM public.notification_deliveries WHERE id = $1", Uuid(emailId)));
            Assert.Equal(0, await WorkflowRows(db, "record-slack-sent.sql", Uuid(emailId)));
        }
        finally
        {
            await Cleanup(db, eventId, externalId, alerts, [owner]);
        }
    }

    private static async Task<AppDbContext> Open()
    {
        var db = await PostgresServiceTests.OpenVerifiedContext();
        await db.Database.OpenConnectionAsync();
        return db;
    }

    private static async Task<int> InsertSource(AppDbContext db, string externalId, double magnitude)
    {
        var feed = new { type = "FeatureCollection", features = new[]
        {
            new { type = "Feature", id = externalId,
                properties = new { type = "earthquake", mag = magnitude, time = 1789401600000L,
                    title = "Workflow SQL fixture", url = "https://example.test/quake" } }
        } };
        using var normalized = await RunCode("normalize-earthquakes", JsonSerializer.Serialize(new { source = "demo.usgs", feed }));
        Assert.Empty(normalized.RootElement.GetProperty("diagnostics").EnumerateArray());
        return await WorkflowScalar<int>(db, "insert-source-events.sql",
            Json(normalized.RootElement.GetProperty("events").GetRawText()));
    }

    private static Task<Guid> EventId(AppDbContext db, string externalId) => Scalar<Guid>(db,
        "SELECT id FROM public.source_events WHERE source = 'demo.usgs' AND external_id = $1", Text(externalId));

    private static async Task<double> Magnitude(AppDbContext db, string externalId) =>
        double.Parse(await Scalar<string>(db,
            "SELECT data->>'magnitude' FROM public.source_events WHERE source = 'demo.usgs' AND external_id = $1",
            Text(externalId)), System.Globalization.CultureInfo.InvariantCulture);

    private static async Task<JsonDocument> Candidate(AppDbContext db, Guid eventId)
    {
        await using var command = Command(db, Sql("select-event-candidates.sql"), Uuid(eventId));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var envelope = JsonDocument.Parse("{\"event\":" + reader.GetString(0) + ",\"alerts\":" + reader.GetString(1) + "}");
        Assert.False(await reader.ReadAsync());
        return envelope;
    }

    private static async Task<JsonDocument> RunCode(string name, string input)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo("node")
        {
            ArgumentList = { Path.Combine(N8nDirectory, "run-code.mjs"), name },
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false
        } };
        process.Start();
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, $"Exact Node body failed: {error}");
        return JsonDocument.Parse(output);
    }

    private static async Task AddOwner(AppDbContext db, Guid id, string slack, string? email) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.users (id, email_destination, slack_destination, revision)
            VALUES ({id}, {email}, {slack}, {Guid.NewGuid()})
            """);

    private static async Task AddAlert(AppDbContext db, Guid id, Guid owner, double threshold, bool enabled) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.alerts (id, owner_id, name, enabled, event_type, revision,
                condition_field, condition_operator, condition_value_type, condition_value)
            VALUES ({id}, {owner}, {"Workflow SQL fixture"}, {enabled}, {"earthquake"}, {Guid.NewGuid()},
                {"magnitude"}, {"gte"}, {"number"}, {threshold})
            """);

    private static async Task Cleanup(AppDbContext db, Guid? eventId, string externalId,
        IReadOnlyCollection<Guid> alerts, IReadOnlyCollection<Guid> owners)
    {
        if (eventId is { } id)
            await Execute(db, "DELETE FROM public.notification_deliveries WHERE source_event_id = $1", Uuid(id));
        await Execute(db, "DELETE FROM public.source_events WHERE source = 'demo.usgs' AND external_id = $1", Text(externalId));
        foreach (var alert in alerts)
            await Execute(db, "DELETE FROM public.alerts WHERE id = $1", Uuid(alert));
        foreach (var owner in owners)
            await Execute(db, "DELETE FROM public.users WHERE id = $1", Uuid(owner));
    }

    private static async Task<T> WorkflowScalar<T>(AppDbContext db, string file, params NpgsqlParameter[] parameters) =>
        await Scalar<T>(db, Sql(file), parameters);

    private static async Task<T> Scalar<T>(AppDbContext db, string sql, params NpgsqlParameter[] parameters)
    {
        await using var command = Command(db, sql, parameters);
        return (T)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Query returned no row."));
    }

    private static async Task<int> WorkflowRows(AppDbContext db, string file, NpgsqlParameter parameter)
    {
        await using var command = Command(db, Sql(file), parameter);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = 0;
        while (await reader.ReadAsync()) rows++;
        return rows;
    }

    private static async Task Execute(AppDbContext db, string sql, params NpgsqlParameter[] parameters)
    {
        await using var command = Command(db, sql, parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static NpgsqlCommand Command(AppDbContext db, string sql, params NpgsqlParameter[] parameters)
    {
        var command = ((NpgsqlConnection)db.Database.GetDbConnection()).CreateCommand();
        command.CommandText = sql;
        command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.Parameters.AddRange(parameters);
        return command;
    }

    private static NpgsqlParameter Json(string value) => new() { NpgsqlDbType = NpgsqlDbType.Jsonb, Value = value };
    private static NpgsqlParameter Uuid(Guid value) => new() { NpgsqlDbType = NpgsqlDbType.Uuid, Value = value };
    private static NpgsqlParameter Text(string value) => new() { NpgsqlDbType = NpgsqlDbType.Text, Value = value };
    private static string Sql(string file) => File.ReadAllText(Path.Combine(N8nDirectory, "sql", file));

    private static string FindN8nDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "n8n");
            if (File.Exists(Path.Combine(path, "run-code.mjs"))) return path;
        }
        throw new DirectoryNotFoundException("Cannot locate n8n/run-code.mjs.");
    }
}
