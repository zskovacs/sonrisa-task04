using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresPageTests
{
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
