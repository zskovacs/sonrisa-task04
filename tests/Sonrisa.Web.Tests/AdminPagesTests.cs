using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

[CollectionDefinition("Admin database", DisableParallelization = true)]
public sealed class AdminDatabaseCollection;

[Collection("Admin database")]
public sealed class AdminPagesTests
{
    [PostgresFact]
    public async Task Admin_pages_show_cross_owner_configuration_without_destinations_or_mutation()
    {
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        var baselineOwners = await db.Users.CountAsync();
        var baselineAlerts = await db.Alerts.CountAsync();
        var baselineEnabled = await db.Alerts.CountAsync(a => a.Enabled);
        var baselineSlack = await db.Users.CountAsync(u => u.SlackDestination != null);
        var baselineEmail = await db.Users.CountAsync(u => u.EmailDestination != null);
        var emailOwner = Guid.NewGuid();
        var slackOwner = Guid.NewGuid();
        var bothOwner = Guid.NewGuid();
        var noAlertOwner = Guid.NewGuid();
        var emailAlert = Guid.NewGuid();
        var slackAlert = Guid.NewGuid();
        var bothAlert = Guid.NewGuid();
        var marker = Guid.NewGuid().ToString("N");
        var emailDestination = $"admin-{marker}@example.test";
        var slackDestination = $"C{marker.ToUpperInvariant()}";
        db.Users.AddRange(
            new UserNotificationSettings { Id = emailOwner, Revision = Guid.NewGuid(), EmailDestination = emailDestination },
            new UserNotificationSettings { Id = slackOwner, Revision = Guid.NewGuid(), SlackDestination = slackDestination },
            new UserNotificationSettings { Id = bothOwner, Revision = Guid.NewGuid(), EmailDestination = $"both-{marker}@example.test", SlackDestination = $"D{marker.ToUpperInvariant()}" },
            new UserNotificationSettings { Id = noAlertOwner, Revision = Guid.NewGuid(), EmailDestination = $"empty-{marker}@example.test" });
        db.Alerts.AddRange(
            NewAlert(emailAlert, emailOwner, $"Email <{marker}>&", true, 5.25),
            NewAlert(slackAlert, slackOwner, $"Slack {marker}", false, 6.5),
            NewAlert(bothAlert, bothOwner, $"Both {marker}", true, 4.75));
        await db.SaveChangesAsync();

        try
        {
            using var factory = new AdminFactory(Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE"), emailOwner);
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var overview = await GetOk(client, "/admin");
            var users = await GetOk(client, "/admin/users");
            var alerts = await GetOk(client, "/admin/alerts");

            AssertMetric(overview, "Owners", baselineOwners + 4);
            AssertMetric(overview, "Alerts", baselineAlerts + 3);
            AssertMetric(overview, "Enabled alerts", baselineEnabled + 2);
            AssertMetric(overview, "Disabled alerts", baselineAlerts - baselineEnabled + 1);
            AssertMetric(overview, "Owners with Slack", baselineSlack + 2);
            AssertMetric(overview, "Owners with Email", baselineEmail + 3);
            Assert.Contains("href=\"/admin/users\"", overview, StringComparison.Ordinal);
            Assert.Contains("href=\"/admin/alerts\"", overview, StringComparison.Ordinal);

            AssertOwnerRow(users, emailOwner, "1", "1", "No", "Yes");
            AssertOwnerRow(users, slackOwner, "1", "0", "Yes", "No");
            AssertOwnerRow(users, bothOwner, "1", "1", "Yes", "Yes");
            AssertOwnerRow(users, noAlertOwner, "0", "0", "No", "Yes");

            var emailRow = RowContaining(alerts, marker, "Email &lt;");
            Assert.Contains(emailOwner.ToString("D"), emailRow, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Email &lt;", emailRow, StringComparison.Ordinal);
            Assert.Contains("&gt;&amp;", emailRow, StringComparison.Ordinal);
            Assert.Contains("earthquake", emailRow, StringComparison.Ordinal);
            Assert.Contains("Enabled", emailRow, StringComparison.Ordinal);
            Assert.Contains("magnitude &gt;= 5.25", emailRow, StringComparison.Ordinal);
            Assert.Contains("Email", emailRow, StringComparison.Ordinal);
            var slackRow = RowContaining(alerts, marker, "Slack ");
            Assert.Contains(slackOwner.ToString("D"), slackRow, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Disabled", slackRow, StringComparison.Ordinal);
            Assert.Contains("magnitude &gt;= 6.5", slackRow, StringComparison.Ordinal);
            Assert.Contains("Slack", slackRow, StringComparison.Ordinal);
            var bothRow = RowContaining(alerts, marker, "Both ");
            Assert.Contains(bothOwner.ToString("D"), bothRow, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Slack + Email", WebUtility.HtmlDecode(bothRow), StringComparison.Ordinal);
            Assert.Contains("magnitude &gt;= 4.75", bothRow, StringComparison.Ordinal);

            foreach (var html in new[] { overview, users, alerts })
            {
                Assert.DoesNotContain(emailDestination, html, StringComparison.Ordinal);
                Assert.DoesNotContain(slackDestination, html, StringComparison.Ordinal);
                Assert.DoesNotContain($"both-{marker}@example.test", html, StringComparison.Ordinal);
                Assert.DoesNotContain($"D{marker.ToUpperInvariant()}", html, StringComparison.Ordinal);
                Assert.DoesNotContain($"empty-{marker}@example.test", html, StringComparison.Ordinal);
            }
            Assert.DoesNotContain("<form", alerts, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<form", users, StringComparison.OrdinalIgnoreCase);

            var managed = await GetOk(client, "/alerts");
            Assert.Contains($"Email &lt;{marker}&gt;&amp;", managed, StringComparison.Ordinal);
            Assert.DoesNotContain($"Slack {marker}", managed, StringComparison.Ordinal);
            Assert.DoesNotContain($"Both {marker}", managed, StringComparison.Ordinal);
            using (var editGet = await client.GetAsync($"/alerts/{slackAlert:D}/edit"))
                Assert.Equal(HttpStatusCode.NotFound, editGet.StatusCode);
            var token = InputValue(await GetOk(client, "/alerts/create"), "__RequestVerificationToken");
            using (var editPost = await client.PostAsync($"/alerts/{slackAlert:D}/edit", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Input.Name"] = "Tampered",
                ["Input.Threshold"] = "9.5",
                ["Input.Revision"] = (await db.Alerts.AsNoTracking().SingleAsync(a => a.Id == slackAlert)).Revision.ToString("D")
            })))
                Assert.Equal(HttpStatusCode.NotFound, editPost.StatusCode);
            using (var statusPost = await client.PostAsync("/alerts?handler=Status", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["id"] = slackAlert.ToString("D"),
                ["revision"] = (await db.Alerts.AsNoTracking().SingleAsync(a => a.Id == slackAlert)).Revision.ToString("D"),
                ["enabled"] = "true"
            })))
                Assert.Equal(HttpStatusCode.NotFound, statusPost.StatusCode);
            var unchanged = await db.Alerts.AsNoTracking().SingleAsync(a => a.Id == slackAlert);
            Assert.Equal($"Slack {marker}", unchanged.Name);
            Assert.Equal(6.5, unchanged.ConditionValue);
            Assert.False(unchanged.Enabled);
        }
        finally
        {
            await db.Alerts.Where(a => a.Id == emailAlert || a.Id == slackAlert || a.Id == bothAlert).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == emailOwner || u.Id == slackOwner || u.Id == bothOwner || u.Id == noAlertOwner).ExecuteDeleteAsync();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-connection-string")]
    public async Task Admin_routes_return_generic_503_for_missing_or_malformed_configuration(string? connection)
    {
        using var factory = new AdminFactory(connection, Guid.NewGuid());
        using var client = factory.CreateClient();
        foreach (var path in new[] { "/admin", "/admin/users", "/admin/alerts" })
        {
            using var response = await client.GetAsync(path);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("temporarily unavailable", html, StringComparison.Ordinal);
            Assert.DoesNotContain("not-a-connection-string", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Host=", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Admin_routes_return_generic_503_for_parsed_configuration_without_a_host()
    {
        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = string.Empty,
            Database = $"admin-{Guid.NewGuid():N}"
        }.ConnectionString;
        Assert.False(string.IsNullOrWhiteSpace(connection));
        _ = new NpgsqlConnectionStringBuilder(connection);
        using var factory = new AdminFactory(connection, Guid.NewGuid());
        using var client = factory.CreateClient();
        foreach (var path in new[] { "/admin", "/admin/users", "/admin/alerts" })
        {
            using var response = await client.GetAsync(path);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("temporarily unavailable", html, StringComparison.Ordinal);
            Assert.DoesNotContain("ArgumentException", html, StringComparison.Ordinal);
            Assert.DoesNotContain("admin-", html, StringComparison.Ordinal);
        }
    }

    [PostgresFact]
    public async Task Empty_admin_results_show_zero_totals_and_clear_empty_states()
    {
        using var factory = new AdminFactory(Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE"), Guid.NewGuid(), emptyResults: true);
        using var client = factory.CreateClient();
        var overview = await GetOk(client, "/admin");
        var users = await GetOk(client, "/admin/users");
        var alerts = await GetOk(client, "/admin/alerts");
        foreach (var label in new[] { "Owners", "Alerts", "Enabled alerts", "Disabled alerts", "Owners with Slack", "Owners with Email" })
            AssertMetric(overview, label, 0);
        Assert.Contains("No owners yet", users, StringComparison.Ordinal);
        Assert.Contains("No alerts yet", alerts, StringComparison.Ordinal);
    }

    [PostgresFact]
    public async Task Admin_routes_return_generic_503_for_database_timeout()
    {
        using var factory = new AdminFactory(Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE"), Guid.NewGuid(), failQueries: true);
        using var client = factory.CreateClient();
        foreach (var path in new[] { "/admin", "/admin/users", "/admin/alerts" })
        {
            using var response = await client.GetAsync(path);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("temporarily unavailable", html, StringComparison.Ordinal);
            Assert.DoesNotContain("timeout-sentinel", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Condition_is_invariant_and_channel_label_defensively_handles_no_destination()
    {
        var row = new Sonrisa.Web.Pages.Admin.AdminAlertRow(Guid.NewGuid(), "Quake", "earthquake", true,
            "magnitude", "gte", "number", 5.5, false, false);
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("magnitude >= 5.5 (number)", row.Condition);
            Assert.Equal("None", row.Channels);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static Alert NewAlert(Guid id, Guid owner, string name, bool enabled, double value) => new()
    {
        Id = id, OwnerId = owner, Revision = Guid.NewGuid(), Name = name,
        Enabled = enabled, EventType = "earthquake", ConditionField = "magnitude",
        ConditionOperator = "gte", ConditionValueType = "number", ConditionValue = value
    };

    private static async Task<string> GetOk(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static void AssertMetric(string html, string label, int value) =>
        Assert.Matches($"<dt[^>]*>{Regex.Escape(label)}</dt>\\s*<dd[^>]*>{value.ToString(CultureInfo.InvariantCulture)}</dd>", html);

    private static void AssertOwnerRow(string html, Guid owner, string alerts, string enabled, string slack, string email)
    {
        var row = RowContaining(html, owner.ToString("D"));
        Assert.Matches($"<td[^>]*>{alerts}</td>\\s*<td[^>]*>{enabled}</td>\\s*<td[^>]*>{slack}</td>\\s*<td[^>]*>{email}</td>", row);
    }

    private static string RowContaining(string html, string identifier, string prefix = "")
    {
        var row = Regex.Matches(html, "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(match => match.Value)
            .Single(row => row.Contains(prefix + identifier, StringComparison.OrdinalIgnoreCase));
        return row;
    }

    private static string InputValue(string markup, string name)
    {
        var match = Regex.Match(markup, $"name=\"{Regex.Escape(name)}\"[^>]*value=\"([^\"]*)\"");
        Assert.True(match.Success, $"Expected input {name}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private sealed class AdminFactory(string? connection, Guid owner, bool emptyResults = false, bool failQueries = false) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<ICurrentOwner>(new PostgresServiceTests.TestOwner(owner)));
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection);
                if (emptyResults) options.AddInterceptors(new EmptyReaderInterceptor());
                if (failQueries) options.AddInterceptors(new TimeoutInterceptor());
                services.AddSingleton(options.Options);
            });
        }
    }

    private sealed class EmptyReaderInterceptor : DbCommandInterceptor
    {
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            var table = new DataTable();
            table.Load(result);
            await result.DisposeAsync();
            table.Clear();
            return table.CreateDataReader();
        }
    }

    private sealed class TimeoutInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) => throw new TimeoutException("timeout-sentinel");
    }
}
