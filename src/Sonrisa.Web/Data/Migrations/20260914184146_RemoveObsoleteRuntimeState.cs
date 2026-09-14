using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sonrisa.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteRuntimeState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_deliveries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "source_events",
                schema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "source_events",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    contract_version = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_events", x => x.id);
                    table.CheckConstraint("ck_source_events_data", "CASE WHEN jsonb_typeof(data) = 'object'\n    AND jsonb_typeof(data->'magnitude') = 'number'\n    AND data = jsonb_build_object('magnitude', data->'magnitude')\nTHEN ((data->>'magnitude')::double precision > '-Infinity'::double precision\n    AND (data->>'magnitude')::double precision < 'Infinity'::double precision) IS TRUE\nELSE false END");
                    table.CheckConstraint("ck_source_events_identity", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND contract_version = 1 AND source IN ('usgs', 'demo.usgs') AND external_id ~ '^[!-~]+$' AND event_type = 'earthquake' AND title = btrim(title, U&'\\0009\\000A\\000B\\000C\\000D\\0020\\0085\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') AND char_length(title) BETWEEN 1 AND 300 AND title !~ '[[:cntrl:]]' AND (source_url IS NULL OR (source_url = btrim(source_url, U&'\\0009\\000A\\000B\\000C\\000D\\0020\\0085\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') AND source_url ~ '^https?://[^[:space:][:cntrl:]]+$'))");
                    table.CheckConstraint("ck_source_events_status", "processing_status IN ('pending', 'evaluated')");
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    destination = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    last_error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.id);
                    table.CheckConstraint("ck_notification_deliveries_destination", "destination = btrim(destination, U&'\\0009\\000A\\000B\\000C\\000D\\0020\\0085\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') AND char_length(destination) BETWEEN 2 AND 254 AND ((channel = 'slack' AND char_length(destination) <= 80 AND destination ~ '^[A-Z0-9]+$') OR (channel = 'email' AND destination ~ '^[^[:space:][:cntrl:]@]+@[^[:space:][:cntrl:]@]+$'))");
                    table.CheckConstraint("ck_notification_deliveries_identity", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND source_event_id <> '00000000-0000-0000-0000-000000000000'::uuid AND alert_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_notification_deliveries_status", "channel IN ('slack', 'email')\nAND status IN ('pending', 'processing', 'sent', 'failed', 'unsupported')\nAND (last_error IS NULL OR last_error ~ '^[a-z][a-z0-9_]*$')\nAND ((channel = 'email' AND status = 'unsupported' AND sent_at IS NULL\n    AND last_error = 'transport_not_implemented')\n  OR (channel IN ('slack', 'email') AND (\n    (status = 'pending' AND sent_at IS NULL AND last_error IS NULL)\n    OR (status = 'processing' AND sent_at IS NULL\n        AND (last_error IS NULL OR last_error = 'delivery_outcome_unknown'))\n    OR (status = 'sent' AND sent_at IS NOT NULL AND last_error IS NULL)\n    OR (status = 'failed' AND sent_at IS NULL AND last_error IS NOT NULL)))) IS TRUE");
                    table.ForeignKey(
                        name: "FK_notification_deliveries_alerts_alert_id",
                        column: x => x.alert_id,
                        principalSchema: "public",
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_source_events_source_event_id",
                        column: x => x.source_event_id,
                        principalSchema: "public",
                        principalTable: "source_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_alert_id",
                schema: "public",
                table: "notification_deliveries",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_deliveries_event_alert_channel",
                schema: "public",
                table: "notification_deliveries",
                columns: new[] { "source_event_id", "alert_id", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_source_events_pending",
                schema: "public",
                table: "source_events",
                columns: new[] { "received_at", "id" },
                filter: "processing_status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ux_source_events_source_external_id",
                schema: "public",
                table: "source_events",
                columns: new[] { "source", "external_id" },
                unique: true);
        }
    }
}
