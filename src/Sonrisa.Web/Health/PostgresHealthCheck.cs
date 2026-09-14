using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Sonrisa.Web.Data;

namespace Sonrisa.Web.Health;

internal sealed record ProductDatabaseConnection(string? Value);

internal sealed class PostgresHealthCheck(
    IServiceScopeFactory scopeFactory,
    ProductDatabaseConnection connection) : IHealthCheck
{
    private const string FailureDescription = "PostgreSQL is unavailable.";
    private const int ConnectionTimeoutSeconds = 5;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connection.Value))
        {
            return HealthCheckResult.Unhealthy(FailureDescription);
        }

        try
        {
            // Npgsql's connection timeout also bounds a stalled handshake.
            var healthConnection = new NpgsqlConnectionStringBuilder(connection.Value);
            if (healthConnection.Timeout is 0 or > ConnectionTimeoutSeconds)
            {
                healthConnection.Timeout = ConnectionTimeoutSeconds;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.SetConnectionString(healthConnection.ConnectionString);
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy(FailureDescription);
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy(FailureDescription);
        }
    }
}
