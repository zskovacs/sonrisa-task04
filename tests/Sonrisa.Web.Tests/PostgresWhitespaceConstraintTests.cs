using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class PostgresWhitespaceConstraintTests
{
    [PostgresFact]
    public async Task Direct_writes_reject_whitespace_only_alert_names()
    {
        foreach (var value in new[] { "\t", "\n", "\u00a0" })
        {
            await using var db = await PostgresServiceTests.OpenVerifiedContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var owner = Guid.NewGuid();
                var revision = Guid.NewGuid();
                var email = "whitespace-test@example.test";
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO public.users (id, email_destination, revision) VALUES ({owner}, {email}, {revision})");
                var alertId = Guid.NewGuid();
                var alertRevision = Guid.NewGuid();
                var eventType = "earthquake";
                var field = "magnitude";
                var comparison = "gte";
                var valueType = "number";
                var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                    db.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO public.alerts
                            (id, owner_id, name, event_type, revision, condition_field,
                             condition_operator, condition_value_type, condition_value)
                        VALUES ({alertId}, {owner}, {value}, {eventType},
                                {alertRevision}, {field}, {comparison}, {valueType}, {5.0})
                        """));
                Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
                Assert.Equal("ck_alerts_name", exception.ConstraintName);
            }
            finally { await transaction.RollbackAsync(); }
        }
    }

    [PostgresFact]
    public async Task Direct_writes_reject_whitespace_only_sole_email_destinations()
    {
        foreach (var value in new[] { "\t", "\n", "\u00a0" })
        {
            await using var db = await PostgresServiceTests.OpenVerifiedContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var owner = Guid.NewGuid();
                var revision = Guid.NewGuid();
                var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                    db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO public.users (id, email_destination, revision) VALUES ({owner}, {value}, {revision})"));
                Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
                Assert.Equal("ck_users_destinations", exception.ConstraintName);
            }
            finally { await transaction.RollbackAsync(); }
        }
    }
}
