using Microsoft.EntityFrameworkCore;
using Sonrisa.Web.Alerts;

namespace Sonrisa.Web.Runtime;

public static class RuntimeModelConfiguration
{
    private const string TrimCharacters = """U&'\0009\000A\000B\000C\000D\0020\0085\00A0\1680\2000\2001\2002\2003\2004\2005\2006\2007\2008\2009\200A\2028\2029\202F\205F\3000'""";
    private const string EmptyUuid = "'00000000-0000-0000-0000-000000000000'::uuid";

    public static void ConfigureRuntimeModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SourceEvent>(entity =>
        {
            entity.ToTable("source_events", "public", table =>
            {
                table.HasCheckConstraint("ck_source_events_identity",
                    $"id <> {EmptyUuid} AND contract_version = 1 AND source IN ('usgs', 'demo.usgs') " +
                    "AND external_id ~ '^[!-~]+$' AND event_type = 'earthquake' " +
                    $"AND title = btrim(title, {TrimCharacters}) AND char_length(title) BETWEEN 1 AND 300 " +
                    "AND title !~ '[[:cntrl:]]' " +
                    $"AND (source_url IS NULL OR (source_url = btrim(source_url, {TrimCharacters}) " +
                    "AND source_url ~ '^https?://[^[:space:][:cntrl:]]+$'))");
                table.HasCheckConstraint("ck_source_events_data", """
                    CASE WHEN jsonb_typeof(data) = 'object'
                        AND jsonb_typeof(data->'magnitude') = 'number'
                        AND data = jsonb_build_object('magnitude', data->'magnitude')
                    THEN ((data->>'magnitude')::double precision > '-Infinity'::double precision
                        AND (data->>'magnitude')::double precision < 'Infinity'::double precision) IS TRUE
                    ELSE false END
                    """);
                table.HasCheckConstraint("ck_source_events_status", "processing_status IN ('pending', 'evaluated')");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ContractVersion).HasColumnName("contract_version").HasColumnType("integer");
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(128).IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").HasColumnType("text").IsRequired();
            entity.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            entity.Property(e => e.SourceUrl).HasColumnName("source_url").HasMaxLength(2048);
            entity.Property(e => e.Data).HasColumnName("data").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at")
                .HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.ProcessingStatus).HasColumnName("processing_status")
                .HasColumnType("text").IsRequired().HasDefaultValue("pending");
            entity.HasIndex(e => new { e.Source, e.ExternalId }).IsUnique()
                .HasDatabaseName("ux_source_events_source_external_id");
            entity.HasIndex(e => new { e.ReceivedAt, e.Id }).HasDatabaseName("ix_source_events_pending")
                .HasFilter("processing_status = 'pending'");
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("notification_deliveries", "public", table =>
            {
                table.HasCheckConstraint("ck_notification_deliveries_identity",
                    $"id <> {EmptyUuid} AND source_event_id <> {EmptyUuid} AND alert_id <> {EmptyUuid}");
                table.HasCheckConstraint("ck_notification_deliveries_destination",
                    $"destination = btrim(destination, {TrimCharacters}) AND char_length(destination) BETWEEN 2 AND 254 " +
                    "AND ((channel = 'slack' AND char_length(destination) <= 80 AND destination ~ '^[A-Z0-9]+$') " +
                    "OR (channel = 'email' AND destination ~ '^[^[:space:][:cntrl:]@]+@[^[:space:][:cntrl:]@]+$'))");
                table.HasCheckConstraint("ck_notification_deliveries_status", """
                    channel IN ('slack', 'email')
                    AND status IN ('pending', 'processing', 'sent', 'failed', 'unsupported')
                    AND (last_error IS NULL OR last_error ~ '^[a-z][a-z0-9_]*$')
                    AND ((channel = 'email' AND status = 'unsupported' AND sent_at IS NULL
                        AND last_error = 'transport_not_implemented')
                      OR (channel = 'slack' AND (
                        (status = 'pending' AND sent_at IS NULL AND last_error IS NULL)
                        OR (status = 'processing' AND sent_at IS NULL
                            AND (last_error IS NULL OR last_error = 'delivery_outcome_unknown'))
                        OR (status = 'sent' AND sent_at IS NOT NULL AND last_error IS NULL)
                        OR (status = 'failed' AND sent_at IS NULL AND last_error IS NOT NULL)))) IS TRUE
                    """);
            });
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(d => d.SourceEventId).HasColumnName("source_event_id").HasColumnType("uuid");
            entity.Property(d => d.AlertId).HasColumnName("alert_id").HasColumnType("uuid");
            entity.Property(d => d.Channel).HasColumnName("channel").HasColumnType("text").IsRequired();
            entity.Property(d => d.Destination).HasColumnName("destination").HasMaxLength(254).IsRequired();
            entity.Property(d => d.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(d => d.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(d => d.SentAt).HasColumnName("sent_at").HasColumnType("timestamp with time zone");
            entity.Property(d => d.LastError).HasColumnName("last_error").HasMaxLength(200);
            entity.HasIndex(d => new { d.SourceEventId, d.AlertId, d.Channel }).IsUnique()
                .HasDatabaseName("ux_notification_deliveries_event_alert_channel");
            entity.HasIndex(d => d.AlertId).HasDatabaseName("ix_notification_deliveries_alert_id");
            entity.HasOne<SourceEvent>().WithMany().HasForeignKey(d => d.SourceEventId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Alert>().WithMany().HasForeignKey(d => d.AlertId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
