using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Sonrisa.Web.Data;
using Sonrisa.Web.Health;

var builder = WebApplication.CreateBuilder(args);

const string connectionKey = "ConnectionStrings:ProductDatabase";
var databaseConnection = new ProductDatabaseConnection(
    builder.Configuration.GetConnectionString("ProductDatabase"));

// Connection failures may contain credentials, so keep provider diagnostics out of host logs.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.None);
builder.Logging.AddFilter("Npgsql", LogLevel.None);

builder.Services.AddSingleton(databaseConnection);
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<ProductDatabaseConnection>().Value));
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>(
    "postgresql", tags: ["ready"], timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

if (string.IsNullOrWhiteSpace(databaseConnection.Value))
{
    app.Logger.LogWarning("Missing database configuration key {ConfigurationKey}.", connectionKey);
}

app.MapRazorPages();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
