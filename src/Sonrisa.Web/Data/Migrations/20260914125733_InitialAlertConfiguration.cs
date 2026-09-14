using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sonrisa.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAlertConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "alerts",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revision = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_field = table.Column<string>(type: "text", nullable: false),
                    condition_operator = table.Column<string>(type: "text", nullable: false),
                    condition_value_type = table.Column<string>(type: "text", nullable: false),
                    condition_value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.id);
                    table.CheckConstraint("ck_alerts_codes", "event_type = 'earthquake' AND condition_field = 'magnitude' AND condition_operator = 'gte' AND condition_value_type = 'number'");
                    table.CheckConstraint("ck_alerts_finite_value", "condition_value > '-Infinity'::double precision AND condition_value < 'Infinity'::double precision");
                    table.CheckConstraint("ck_alerts_ids", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND owner_id <> '00000000-0000-0000-0000-000000000000'::uuid AND revision <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_alerts_name", "name = btrim(name) AND char_length(name) BETWEEN 1 AND 120");
                });

            migrationBuilder.CreateTable(
                name: "alert_channels",
                schema: "public",
                columns: table => new
                {
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_type = table.Column<string>(type: "text", nullable: false),
                    destination = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_channels", x => new { x.alert_id, x.channel_type });
                    table.CheckConstraint("ck_alert_channels_destination", "destination = btrim(destination) AND char_length(destination) BETWEEN 1 AND 254");
                    table.CheckConstraint("ck_alert_channels_type", "channel_type IN ('email', 'slack')");
                    table.ForeignKey(
                        name: "FK_alert_channels_alerts_alert_id",
                        column: x => x.alert_id,
                        principalSchema: "public",
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_enabled_event_type",
                schema: "public",
                table: "alerts",
                column: "event_type",
                filter: "enabled = true");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_owner_id",
                schema: "public",
                table: "alerts",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_channels",
                schema: "public");

            migrationBuilder.DropTable(
                name: "alerts",
                schema: "public");
        }
    }
}
