using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sonrisa.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnableEmailDeliveryStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_deliveries_status",
                schema: "public",
                table: "notification_deliveries");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_deliveries_status",
                schema: "public",
                table: "notification_deliveries",
                sql: "channel IN ('slack', 'email')\nAND status IN ('pending', 'processing', 'sent', 'failed', 'unsupported')\nAND (last_error IS NULL OR last_error ~ '^[a-z][a-z0-9_]*$')\nAND ((channel = 'email' AND status = 'unsupported' AND sent_at IS NULL\n    AND last_error = 'transport_not_implemented')\n  OR (channel IN ('slack', 'email') AND (\n    (status = 'pending' AND sent_at IS NULL AND last_error IS NULL)\n    OR (status = 'processing' AND sent_at IS NULL\n        AND (last_error IS NULL OR last_error = 'delivery_outcome_unknown'))\n    OR (status = 'sent' AND sent_at IS NOT NULL AND last_error IS NULL)\n    OR (status = 'failed' AND sent_at IS NULL AND last_error IS NOT NULL)))) IS TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM public.notification_deliveries
                        WHERE channel = 'email' AND status <> 'unsupported'
                    ) THEN
                        RAISE EXCEPTION 'Cannot restore the previous delivery constraint while active email delivery states exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_deliveries_status",
                schema: "public",
                table: "notification_deliveries");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_deliveries_status",
                schema: "public",
                table: "notification_deliveries",
                sql: "channel IN ('slack', 'email')\nAND status IN ('pending', 'processing', 'sent', 'failed', 'unsupported')\nAND (last_error IS NULL OR last_error ~ '^[a-z][a-z0-9_]*$')\nAND ((channel = 'email' AND status = 'unsupported' AND sent_at IS NULL\n    AND last_error = 'transport_not_implemented')\n  OR (channel = 'slack' AND (\n    (status = 'pending' AND sent_at IS NULL AND last_error IS NULL)\n    OR (status = 'processing' AND sent_at IS NULL\n        AND (last_error IS NULL OR last_error = 'delivery_outcome_unknown'))\n    OR (status = 'sent' AND sent_at IS NOT NULL AND last_error IS NULL)\n    OR (status = 'failed' AND sent_at IS NULL AND last_error IS NOT NULL)))) IS TRUE");
        }
    }
}
