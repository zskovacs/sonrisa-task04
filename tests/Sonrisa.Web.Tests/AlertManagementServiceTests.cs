using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class AlertManagementServiceTests
{
    [Fact]
    public async Task Missing_database_configuration_is_unavailable()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
        var service = Service(db);
        Assert.Equal(AlertStatus.Unavailable, (await service.ListAsync()).Status);
        Assert.Equal(AlertStatus.Unavailable, (await service.GetAsync(Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Malformed_database_configuration_is_unavailable_without_exception()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Guid.NewGuid().ToString("N")).Options);
        var service = Service(db);
        Assert.Equal(AlertStatus.Unavailable, (await service.ListAsync()).Status);
    }

    private static AlertManagementService Service(AppDbContext db) => new(db,
        new TestOwner(Guid.NewGuid()), new AlertInputValidator(),
        NullLogger<AlertManagementService>.Instance);

    private sealed record TestOwner(Guid OwnerId) : ICurrentOwner;
}
