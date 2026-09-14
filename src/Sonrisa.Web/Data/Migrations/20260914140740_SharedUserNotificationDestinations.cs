using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sonrisa.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SharedUserNotificationDestinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF runs this migration in one transaction; hold both locks through backfill, FK, and drop.
            migrationBuilder.Sql("LOCK TABLE public.alerts, public.alert_channels IN ACCESS EXCLUSIVE MODE;");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_destination = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    slack_destination = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_destinations", "(email_destination IS NOT NULL OR slack_destination IS NOT NULL) AND (email_destination IS NULL OR (email_destination = btrim(email_destination) AND char_length(email_destination) BETWEEN 1 AND 254)) AND (slack_destination IS NULL OR (slack_destination = btrim(slack_destination) AND char_length(slack_destination) BETWEEN 2 AND 80 AND slack_destination ~ '^[A-Z0-9]+$'))");
                    table.CheckConstraint("ck_users_ids", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND revision <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.Sql("""
                DO $migration$
                DECLARE conflicting boolean;
                BEGIN
                    WITH per_alert AS (
                        SELECT a.id, a.owner_id,
                               max(btrim(c.destination)) FILTER (WHERE c.channel_type = 'email') AS email,
                               max(btrim(c.destination)) FILTER (WHERE c.channel_type = 'slack') AS slack
                        FROM public.alerts a
                        LEFT JOIN public.alert_channels c ON c.alert_id = a.id
                        GROUP BY a.id, a.owner_id
                    )
                    SELECT EXISTS (
                        SELECT 1 FROM per_alert p
                        WHERE (p.email IS NULL AND p.slack IS NULL)
                           OR (p.slack IS NOT NULL AND
                               (char_length(p.slack) NOT BETWEEN 2 AND 80 OR p.slack !~ '^[A-Z0-9]+$'))
                           OR EXISTS (
                               SELECT 1 FROM per_alert q
                               WHERE q.owner_id = p.owner_id
                                 AND (q.email IS DISTINCT FROM p.email
                                      OR q.slack IS DISTINCT FROM p.slack)
                           )
                    ) INTO conflicting;
                    IF conflicting THEN
                        RAISE EXCEPTION 'Legacy alert destinations cannot be converted safely.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.Sql("""
                INSERT INTO public.users (id, email_destination, slack_destination, revision)
                SELECT owner_id,
                       max(email), max(slack), gen_random_uuid()
                FROM (
                    SELECT a.id, a.owner_id,
                           max(btrim(c.destination)) FILTER (WHERE c.channel_type = 'email') AS email,
                           max(btrim(c.destination)) FILTER (WHERE c.channel_type = 'slack') AS slack
                    FROM public.alerts a
                    LEFT JOIN public.alert_channels c ON c.alert_id = a.id
                    GROUP BY a.id, a.owner_id
                ) AS per_alert
                GROUP BY owner_id;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_alerts_users_owner_id",
                schema: "public",
                table: "alerts",
                column: "owner_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(name: "alert_channels", schema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Shared notification destinations cannot be downgraded without data loss.");
        }
    }
}
