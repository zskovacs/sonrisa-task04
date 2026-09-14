using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Sonrisa.Web.Data;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class RuntimeModelTests
{
    [Fact]
    public void Runtime_entities_expose_the_two_persistent_contracts()
    {
        using var db = NewContext();
        var source = db.Model.FindEntityType("Sonrisa.Web.Runtime.SourceEvent");
        var delivery = db.Model.FindEntityType("Sonrisa.Web.Runtime.NotificationDelivery");
        Assert.NotNull(source);
        Assert.NotNull(delivery);

        Assert.Equal("public", source.GetSchema());
        Assert.Equal("source_events", source.GetTableName());
        Assert.Equal(new Dictionary<string, string>
        {
            ["id"] = "uuid", ["contract_version"] = "integer", ["source"] = "character varying(32)",
            ["external_id"] = "character varying(128)", ["event_type"] = "text",
            ["occurred_at"] = "timestamp with time zone", ["title"] = "character varying(300)",
            ["source_url"] = "character varying(2048)", ["data"] = "jsonb",
            ["received_at"] = "timestamp with time zone", ["processing_status"] = "text"
        }, Columns(source));
        Assert.Equal(["Id"], source.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.Contains(source.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name)
            .SequenceEqual(["Source", "ExternalId"]));
        Assert.Contains(source.GetIndexes(), i => i.GetDatabaseName() == "ix_source_events_pending"
            && i.GetFilter() == "processing_status = 'pending'"
            && i.Properties.Select(p => p.Name).SequenceEqual(["ReceivedAt", "Id"]));

        Assert.Equal("public", delivery.GetSchema());
        Assert.Equal("notification_deliveries", delivery.GetTableName());
        Assert.Equal(new Dictionary<string, string>
        {
            ["id"] = "uuid", ["source_event_id"] = "uuid", ["alert_id"] = "uuid",
            ["channel"] = "text", ["destination"] = "character varying(254)",
            ["status"] = "text", ["created_at"] = "timestamp with time zone",
            ["sent_at"] = "timestamp with time zone", ["last_error"] = "character varying(200)"
        }, Columns(delivery));
        Assert.Equal(["Id"], delivery.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.Contains(delivery.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name)
            .SequenceEqual(["SourceEventId", "AlertId", "Channel"]));
        Assert.Equal(2, delivery.GetForeignKeys().Count());
        Assert.All(delivery.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [Fact]
    public void Runtime_constraints_defend_json_status_and_destination_boundaries()
    {
        using var db = NewContext();
        var model = db.GetService<IDesignTimeModel>().Model;
        var source = model.FindEntityType("Sonrisa.Web.Runtime.SourceEvent");
        var delivery = model.FindEntityType("Sonrisa.Web.Runtime.NotificationDelivery");
        Assert.NotNull(source);
        Assert.NotNull(delivery);
        Assert.Equal(["ck_source_events_data", "ck_source_events_identity", "ck_source_events_status"],
            source.GetCheckConstraints().Select(c => c.Name!).Order().ToArray());
        Assert.Equal(["ck_notification_deliveries_destination", "ck_notification_deliveries_identity",
                "ck_notification_deliveries_status"],
            delivery.GetCheckConstraints().Select(c => c.Name!).Order().ToArray());
        var status = delivery.GetCheckConstraints().Single(c => c.Name == "ck_notification_deliveries_status").Sql;
        Assert.Contains("channel IN ('slack', 'email')", status);
        Assert.Contains("channel = 'email' AND status = 'unsupported'", status);
        Assert.Contains("channel IN ('slack', 'email') AND (", status);
    }

    private static Dictionary<string, string> Columns(IEntityType entity) => entity.GetProperties().ToDictionary(
        p => p.GetColumnName(StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema()))!,
        p => p.GetColumnType()!);

    private static AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
}
