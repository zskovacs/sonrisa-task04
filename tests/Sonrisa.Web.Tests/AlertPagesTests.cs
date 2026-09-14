using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sonrisa.Web.Data;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class AlertPagesTests : IClassFixture<AlertPagesTests.AlertWebApplicationFactory>
{
    private readonly AlertWebApplicationFactory factory;

    public AlertPagesTests(AlertWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Root_redirects_to_alerts()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/alerts", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_form_shows_fixed_condition_shared_settings_link_and_antiforgery()
    {
        using var client = factory.CreateClient();

        var page = await client.GetStringAsync("/alerts/create");

        Assert.Contains("Earthquake magnitude is greater than or equal to", page, StringComparison.Ordinal);
        Assert.Contains("notification settings", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Input.EmailDestination", page, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", page, StringComparison.Ordinal);
        Assert.DoesNotContain("OwnerId", page, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex("<input(?=[^>]*name=\"Input.Enabled\")(?=[^>]*checked)[^>]*>"), page);
    }

    [Fact]
    public async Task Settings_form_exposes_shared_destinations_and_antiforgery()
    {
        using var emptyFactory = new EmptyAlertWebApplicationFactory();
        using var client = emptyFactory.CreateClient();
        using (var scope = emptyFactory.Services.CreateScope())
            Assert.True(string.IsNullOrWhiteSpace(
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString()));

        using var response = await client.GetAsync("/settings/notifications");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();

        Assert.Contains("Email destination", page, StringComparison.Ordinal);
        Assert.Contains("Slack destination", page, StringComparison.Ordinal);
        Assert.Contains("Save settings", page, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", page, StringComparison.Ordinal);
        Assert.DoesNotContain("OwnerId", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_create_preserves_submitted_input_and_displays_validation()
    {
        using var client = factory.CreateClient();
        var page = await client.GetStringAsync("/alerts/create");
        var antiforgery = Regex.Match(page,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(antiforgery.Success, "The create form must include an antiforgery token.");

        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery.Groups[1].Value),
            new KeyValuePair<string, string>("Input.Name", "My retained alert"),
            new KeyValuePair<string, string>("Input.Threshold", "not-a-number")
        ]);
        using var response = await client.PostAsync("/alerts/create", form);
        var result = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("My retained alert", result, StringComparison.Ordinal);
        Assert.Contains("Enter a finite number using a dot for decimals.", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_boolean_binding_returns_the_create_form_without_losing_input()
    {
        using var client = factory.CreateClient();
        var page = await client.GetStringAsync("/alerts/create");
        var antiforgery = Regex.Match(page,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");

        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery.Groups[1].Value),
            new KeyValuePair<string, string>("Input.Name", "Bound input remains visible"),
            new KeyValuePair<string, string>("Input.Threshold", "5.5"),
            new KeyValuePair<string, string>("Input.Enabled", "not-a-boolean")
        ]);
        using var response = await client.PostAsync("/alerts/create", form);
        var result = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Bound input remains visible", result, StringComparison.Ordinal);
        Assert.Contains("Select whether the alert is enabled.", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_settings_submission_preserves_input_and_maps_email_error()
    {
        using var client = factory.CreateClient();
        using var initial = await client.GetAsync("/settings/notifications");
        var page = await initial.Content.ReadAsStringAsync();
        var antiforgery = Regex.Match(page,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(antiforgery.Success);
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery.Groups[1].Value),
            new KeyValuePair<string, string>("Input.EmailDestination", "Name <person@example.test>")
        ]);
        using var response = await client.PostAsync("/settings/notifications", form);
        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Name &lt;person@example.test&gt;", result, StringComparison.Ordinal);
        Assert.Contains("Enter a single email mailbox.", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_status_binding_is_rejected_before_the_service_call()
    {
        using var client = factory.CreateClient();
        var page = await client.GetStringAsync("/alerts/create");
        var antiforgery = Regex.Match(page,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery.Groups[1].Value),
            new KeyValuePair<string, string>("id", Guid.NewGuid().ToString()),
            new KeyValuePair<string, string>("revision", Guid.NewGuid().ToString()),
            new KeyValuePair<string, string>("enabled", "not-a-boolean")
        ]);

        using var response = await client.PostAsync("/alerts?handler=Status", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_edit_revision_returns_reload_guidance_without_a_database_call()
    {
        using var client = factory.CreateClient();
        var page = await client.GetStringAsync("/alerts/create");
        var antiforgery = Regex.Match(page,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery.Groups[1].Value),
            new KeyValuePair<string, string>("Input.Name", "Revision test"),
            new KeyValuePair<string, string>("Input.Threshold", "5.5"),
            new KeyValuePair<string, string>("Input.Revision", "not-a-guid")
        ]);

        using var response = await client.PostAsync($"/alerts/{Guid.NewGuid():D}/edit", form);
        var result = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Reload this alert before saving.", result, StringComparison.Ordinal);
    }

    public sealed class AlertWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("MvpOwner:Id", "c814a599-da3f-4a9d-8f31-0e021db2047a")
            ]));
            builder.ConfigureServices(IsolateDatabase);
        }
    }

    private sealed class EmptyAlertWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(IsolateDatabase);
        }
    }

    private static void IsolateDatabase(IServiceCollection services)
    {
        // Program captures runtime configuration eagerly; replace the provider options for this test host.
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.AddSingleton(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
    }
}
