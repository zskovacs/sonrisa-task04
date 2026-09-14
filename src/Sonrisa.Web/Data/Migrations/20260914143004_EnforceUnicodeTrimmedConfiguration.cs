using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sonrisa.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUnicodeTrimmedConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_destinations",
                schema: "public",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_alerts_name",
                schema: "public",
                table: "alerts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_destinations",
                schema: "public",
                table: "users",
                sql: "(email_destination IS NOT NULL OR slack_destination IS NOT NULL) AND (email_destination IS NULL OR (email_destination = btrim(email_destination, U&'\\0009\\000A\\000B\\000C\\000D\\0020\\0085\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') AND char_length(email_destination) BETWEEN 1 AND 254)) AND (slack_destination IS NULL OR (slack_destination = btrim(slack_destination, U&'\\0009\\000A\\000B\\000C\\000D\\0020\\0085\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') AND char_length(slack_destination) BETWEEN 2 AND 80 AND slack_destination ~ '^[A-Z0-9]+$'))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_alerts_name",
                schema: "public",
                table: "alerts",
                sql: "name = btrim(name, U&'\\0009\\000A\\000B\\000C\\000D\\0020\\0085\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') AND char_length(name) BETWEEN 1 AND 120");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_destinations",
                schema: "public",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_alerts_name",
                schema: "public",
                table: "alerts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_destinations",
                schema: "public",
                table: "users",
                sql: "(email_destination IS NOT NULL OR slack_destination IS NOT NULL) AND (email_destination IS NULL OR (email_destination = btrim(email_destination) AND char_length(email_destination) BETWEEN 1 AND 254)) AND (slack_destination IS NULL OR (slack_destination = btrim(slack_destination) AND char_length(slack_destination) BETWEEN 2 AND 80 AND slack_destination ~ '^[A-Z0-9]+$'))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_alerts_name",
                schema: "public",
                table: "alerts",
                sql: "name = btrim(name) AND char_length(name) BETWEEN 1 AND 120");
        }
    }
}
