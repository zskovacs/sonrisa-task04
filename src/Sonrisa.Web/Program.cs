using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Sonrisa.Web.Data;
using Sonrisa.Web.Health;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Users;
using Sonrisa.Web.Ownership;
using Sonrisa.Web.Observability;

var builder = WebApplication.CreateBuilder(args);

const string connectionKey = "ConnectionStrings:ProductDatabase";
var databaseConnection = new ProductDatabaseConnection(
    builder.Configuration.GetConnectionString("ProductDatabase"));

// Connection failures may contain credentials, so keep provider diagnostics out of host logs.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Npgsql", LogLevel.None);
var observabilityWarnings = builder.AddSonrisaObservability();

builder.Services.AddSingleton(databaseConnection);
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<ProductDatabaseConnection>().Value));
builder.Services.AddSingleton<ICurrentOwner>(new ConfiguredCurrentOwner(builder.Configuration));
builder.Services.AddSingleton<IValidator<AlertInput>, AlertInputValidator>();
builder.Services.AddSingleton<IValidator<UserNotificationSettingsInput>, UserNotificationSettingsInputValidator>();
builder.Services.AddScoped<AlertManagementService>();
builder.Services.AddScoped<UserNotificationSettingsService>();
builder.Services.AddExceptionHandler<SafeExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>(
    "postgresql", tags: ["ready"], timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

foreach (var key in observabilityWarnings)
    app.Logger.LogWarning("OTLP export disabled due to invalid configuration key {ConfigurationKey}.", key);

if (string.IsNullOrWhiteSpace(databaseConnection.Value))
{
    app.Logger.LogWarning("Missing database configuration key {ConfigurationKey}.", connectionKey);
}

app.UseExceptionHandler();
app.UseStaticFiles();
app.MapRazorPages();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
