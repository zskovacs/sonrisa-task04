using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresPageTests
{
    [PostgresFact]
    public async Task Edit_uses_the_current_owner_and_revision_and_rejects_invalid_or_stale_posts()
    {
        var owner = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var originalRevision = Guid.NewGuid();
        var marker = Guid.NewGuid().ToString("N");
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        try
        {
            db.Users.Add(new UserNotificationSettings
            {
                Id = owner, Revision = Guid.NewGuid(), EmailDestination = $"edit-{marker}@example.test"
            });
            db.Alerts.Add(new Alert
            {
                Id = alertId, OwnerId = owner, Revision = originalRevision, Name = $"Original {marker}",
                Enabled = true, EventType = "earthquake", ConditionField = "magnitude",
                ConditionOperator = "gte", ConditionValueType = "number", ConditionValue = 5.5
            });
            await db.SaveChangesAsync();

            using var factory = new DatabasePageFactory(owner, Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE"));
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var edit = await client.GetStringAsync($"/alerts/{alertId:D}/edit");
            Assert.Equal($"Original {marker}", InputValue(edit, "Input.Name"));
            Assert.Equal("5.5", InputValue(edit, "Input.Threshold"));
            Assert.Equal(originalRevision.ToString("D"), InputValue(edit, "Input.Revision"));

            using (var response = await client.PostAsync($"/alerts/{alertId:D}/edit", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = InputValue(edit, "__RequestVerificationToken"),
                ["Input.Name"] = $"Updated {marker}",
                ["Input.Threshold"] = "6.25",
                ["Input.Enabled"] = "true",
                ["Input.Revision"] = originalRevision.ToString("D"),
                ["Input.OwnerId"] = Guid.NewGuid().ToString("D"),
                ["OwnerId"] = Guid.NewGuid().ToString("D")
            })))
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var updated = await db.Alerts.AsNoTracking().SingleAsync(alert => alert.Id == alertId);
            Assert.Equal(owner, updated.OwnerId);
            Assert.Equal($"Updated {marker}", updated.Name);
            Assert.Equal(6.25, updated.ConditionValue);
            Assert.Equal("earthquake", updated.EventType);
            Assert.Equal("magnitude", updated.ConditionField);
            Assert.Equal("gte", updated.ConditionOperator);
            Assert.Equal("number", updated.ConditionValueType);
            Assert.NotEqual(originalRevision, updated.Revision);

            edit = await client.GetStringAsync($"/alerts/{alertId:D}/edit");
            using (var response = await client.PostAsync($"/alerts/{alertId:D}/edit", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = InputValue(edit, "__RequestVerificationToken"),
                ["Input.Name"] = $"Stale {marker}",
                ["Input.Threshold"] = "7.5",
                ["Input.Enabled"] = "false",
                ["Input.Revision"] = originalRevision.ToString("D")
            })))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("This alert changed. Reload it before saving.", await response.Content.ReadAsStringAsync());
            }
            await AssertUnchangedAsync(db, updated);

            foreach (var invalid in new[]
            {
                new Dictionary<string, string> { ["Input.Name"] = "  ", ["Input.Threshold"] = "6.25" },
                new Dictionary<string, string> { ["Input.Name"] = $"Updated {marker}", ["Input.Threshold"] = "not-a-number" }
            })
            {
                edit = await client.GetStringAsync($"/alerts/{alertId:D}/edit");
                invalid["__RequestVerificationToken"] = InputValue(edit, "__RequestVerificationToken");
                invalid["Input.Enabled"] = "true";
                invalid["Input.Revision"] = updated.Revision.ToString("D");
                using var response = await client.PostAsync($"/alerts/{alertId:D}/edit", new FormUrlEncodedContent(invalid));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(invalid["Input.Name"] == "  " ? "Enter a name" : "Enter a finite number", await response.Content.ReadAsStringAsync());
                await AssertUnchangedAsync(db, updated);
            }
        }
        finally
        {
            await db.Alerts.Where(alert => alert.Id == alertId).ExecuteDeleteAsync();
            await db.Users.Where(user => user.Id == owner).ExecuteDeleteAsync();
        }
    }

    [PostgresFact]
    public async Task Settings_first_two_saves_and_status_forms_use_current_revision()
    {
        var owner = Guid.NewGuid();
        var connection = Environment.GetEnvironmentVariable("SONRISA_TEST_DATABASE");
        await using var db = await PostgresServiceTests.OpenVerifiedContext();
        using var factory = new DatabasePageFactory(owner, connection);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        try
        {
            var create = await client.GetStringAsync("/alerts/create");
            var fields = new Dictionary<string, string>
            { ["Input.Name"] = "Status form fixture", ["Input.Threshold"] = "5.5" };
            using (var response = await client.PostAsync("/alerts/create", new FormUrlEncodedContent(fields)))
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            fields["__RequestVerificationToken"] = InputValue(create, "__RequestVerificationToken");
            using (var response = await client.PostAsync("/alerts/create", new FormUrlEncodedContent(fields)))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("Save notification settings before creating an alert.", await response.Content.ReadAsStringAsync());
            }

            var settings = await client.GetStringAsync("/settings/notifications");
            var save = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = InputValue(settings, "__RequestVerificationToken"),
                ["Input.EmailDestination"] = "person@example.test",
                ["Input.OwnerId"] = Guid.NewGuid().ToString()
            };
            using (var response = await client.PostAsync("/settings/notifications", new FormUrlEncodedContent(save)))
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            settings = await client.GetStringAsync("/settings/notifications");
            var firstRevision = InputValue(settings, "Input.Revision");
            Assert.NotEqual(Guid.Empty.ToString(), firstRevision);
            save["__RequestVerificationToken"] = InputValue(settings, "__RequestVerificationToken");
            save["Input.Revision"] = firstRevision;
            save.Remove("Input.EmailDestination");
            save["Input.SlackDestination"] = "C12345678";
            using (var response = await client.PostAsync("/settings/notifications", new FormUrlEncodedContent(save)))
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            settings = await client.GetStringAsync("/settings/notifications");
            Assert.NotEqual(firstRevision, InputValue(settings, "Input.Revision"));
            Assert.Equal("C12345678", (await db.Users.AsNoTracking().SingleAsync(u => u.Id == owner)).SlackDestination);

            create = await client.GetStringAsync("/alerts/create");
            fields["__RequestVerificationToken"] = InputValue(create, "__RequestVerificationToken");
            fields["Input.OwnerId"] = Guid.NewGuid().ToString();
            using (var response = await client.PostAsync("/alerts/create", new FormUrlEncodedContent(fields)))
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var alert = await db.Alerts.AsNoTracking().SingleAsync(a => a.OwnerId == owner);
            Assert.False(alert.Enabled);
            foreach (var desired in new[] { true, false })
            {
                var list = await client.GetStringAsync("/alerts");
                var form = Regex.Match(list, "<form[^>]*action=\"/alerts\\?handler=Status\"[^>]*>(.*?)</form>", RegexOptions.Singleline);
                Assert.True(form.Success);
                var submitted = new Dictionary<string, string>();
                foreach (var key in new[] { "id", "revision", "enabled", "__RequestVerificationToken" })
                    submitted[key] = InputValue(form.Value, key);
                Assert.Equal(desired ? "true" : "false", submitted["enabled"]);
                using var response = await client.PostAsync("/alerts?handler=Status", new FormUrlEncodedContent(submitted));
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                Assert.Equal(desired, await db.Alerts.Where(a => a.Id == alert.Id && a.OwnerId == owner)
                    .Select(a => a.Enabled).SingleAsync());
            }
        }
        finally
        {
            await db.Alerts.Where(a => a.OwnerId == owner).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == owner).ExecuteDeleteAsync();
        }
    }

    private static string InputValue(string markup, string name)
    {
        var match = Regex.Match(markup, $"name=\"{Regex.Escape(name)}\"[^>]*value=\"([^\"]*)\"");
        Assert.True(match.Success, $"Expected input {name}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task AssertUnchangedAsync(AppDbContext db, Alert expected)
    {
        var actual = await db.Alerts.AsNoTracking().SingleAsync(alert => alert.Id == expected.Id);
        Assert.Equal(expected.OwnerId, actual.OwnerId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.ConditionValue, actual.ConditionValue);
        Assert.Equal(expected.Enabled, actual.Enabled);
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.EventType, actual.EventType);
        Assert.Equal(expected.ConditionField, actual.ConditionField);
        Assert.Equal(expected.ConditionOperator, actual.ConditionOperator);
        Assert.Equal(expected.ConditionValueType, actual.ConditionValueType);
    }

    private sealed class DatabasePageFactory(Guid owner, string? connection) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<ICurrentOwner>(new TestOwner(owner)));
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
            });
        }
    }

    private sealed record TestOwner(Guid OwnerId) : ICurrentOwner;
}
