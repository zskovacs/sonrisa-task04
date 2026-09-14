using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Sonrisa.Web.Data;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class RuntimeModelTests
{
    [Fact]
    public void Current_product_model_maps_only_configuration_tables()
    {
        using var db = NewContext();
        Assert.Equal(["public.alerts", "public.users"], db.Model.GetEntityTypes()
            .Select(entity => $"{entity.GetSchema()}.{entity.GetTableName()}")
            .Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Forward_migration_drops_delivery_before_source_without_other_changes()
    {
        using var db = NewContext();
        var assembly = db.GetService<IMigrationsAssembly>();
        var migrationType = Assert.Single(assembly.Migrations,
            entry => entry.Key.EndsWith("_RemoveObsoleteRuntimeState", StringComparison.Ordinal)).Value;
        var migration = assembly.CreateMigration(migrationType, "Npgsql.EntityFrameworkCore.PostgreSQL");
        var drops = migration.UpOperations.Select(operation => Assert.IsType<DropTableOperation>(operation))
            .Select(operation => $"{operation.Schema}.{operation.Name}").ToArray();
        Assert.Equal(["public.notification_deliveries", "public.source_events"], drops);
    }

    private static AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
}
