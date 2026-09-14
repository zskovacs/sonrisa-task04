using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class OwnerAndModelTests
{
    [Fact]
    public void Missing_owner_configuration_uses_fixed_owner()
    {
        var configuration = new ConfigurationBuilder().Build();
        Assert.Equal(ConfiguredCurrentOwner.DefaultOwnerId, new ConfiguredCurrentOwner(configuration).OwnerId);
        Assert.NotEqual(Guid.Empty, new ConfiguredCurrentOwner(configuration).OwnerId);
    }

    [Fact]
    public void Explicit_owner_configuration_uses_that_owner()
    {
        var ownerId = Guid.NewGuid();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["MvpOwner:Id"] = ownerId.ToString() }).Build();
        Assert.Equal(ownerId, new ConfiguredCurrentOwner(configuration).OwnerId);
    }

    [Fact]
    public void Design_time_context_has_no_connection_fallback()
    {
        using var db = AppDbContextFactory.CreateDbContext(new ConfigurationBuilder().Build());
        Assert.Null(db.Database.GetConnectionString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Invalid_explicit_owner_configuration_fails_without_echoing_value(string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["MvpOwner:Id"] = value }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => new ConfiguredCurrentOwner(configuration));
        Assert.Equal("Invalid MvpOwner:Id configuration.", error.Message);
    }

    [Fact]
    public void Alert_mapping_has_exact_relational_contract()
    {
        using var db = NewContext();
        var alert = db.Model.FindEntityType(typeof(Alert))!;
        Assert.Equal("public", alert.GetSchema());
        Assert.Equal("alerts", alert.GetTableName());
        var columns = alert.GetProperties().ToDictionary(
            property => property.GetColumnName(StoreObjectIdentifier.Table("alerts", "public"))!,
            property => property.GetColumnType()!);
        Assert.Equal(new Dictionary<string, string>
        {
            ["id"] = "uuid", ["owner_id"] = "uuid", ["name"] = "character varying(120)",
            ["event_type"] = "text", ["enabled"] = "boolean", ["revision"] = "uuid",
            ["condition_field"] = "text", ["condition_operator"] = "text",
            ["condition_value_type"] = "text", ["condition_value"] = "double precision"
        }, columns);
        Assert.All(alert.GetProperties(), property => Assert.False(property.IsNullable));
        Assert.True(alert.FindProperty(nameof(Alert.Revision))!.IsConcurrencyToken);
        Assert.Equal(["Id"], alert.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.Equal(2, alert.GetIndexes().Count());
        Assert.Contains(alert.GetIndexes(), i => i.GetDatabaseName() == "ix_alerts_owner_id"
            && i.Properties.Select(p => p.Name).SequenceEqual(["OwnerId"]));
        Assert.Contains(alert.GetIndexes(), i => i.GetDatabaseName() == "ix_alerts_enabled_event_type"
            && i.GetFilter() == "enabled = true" && i.Properties.Select(p => p.Name).SequenceEqual(["EventType"]));
        var designAlert = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Alert))!;
        var checks = designAlert.GetCheckConstraints().ToArray();
        Assert.Equal(["ck_alerts_codes", "ck_alerts_finite_value", "ck_alerts_ids", "ck_alerts_name"],
            checks.Select(c => c.Name!).Order().ToArray());
        Assert.Contains(checks, check => check.Sql.Contains("condition_value") && check.Sql.Contains("Infinity") && check.Sql.Contains("-Infinity"));
        Assert.Contains(checks, check => check.Sql.Contains("event_type") && check.Sql.Contains("earthquake"));
    }

    [Fact]
    public void User_profile_mapping_has_owner_fk_and_shared_destination_checks()
    {
        using var db = NewContext();
        var user = db.Model.FindEntityType(typeof(UserNotificationSettings))!;
        Assert.Equal("public", user.GetSchema());
        Assert.Equal("users", user.GetTableName());
        Assert.Equal(["Id"], user.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.True(user.FindProperty(nameof(UserNotificationSettings.Revision))!.IsConcurrencyToken);
        var columns = user.GetProperties().ToDictionary(
            property => property.GetColumnName(StoreObjectIdentifier.Table("users", "public"))!,
            property => property.GetColumnType()!);
        Assert.Equal(new Dictionary<string, string>
        {
            ["id"] = "uuid", ["email_destination"] = "character varying(254)",
            ["slack_destination"] = "character varying(80)", ["revision"] = "uuid"
        }, columns);
        var alert = db.Model.FindEntityType(typeof(Alert))!;
        var foreignKey = Assert.Single(alert.GetForeignKeys());
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(nameof(Alert.OwnerId), Assert.Single(foreignKey.Properties).Name);
        var designUser = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(UserNotificationSettings))!;
        Assert.Equal(["ck_users_destinations", "ck_users_ids"],
            designUser.GetCheckConstraints().Select(c => c.Name!).Order().ToArray());
    }

    private static AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
}
