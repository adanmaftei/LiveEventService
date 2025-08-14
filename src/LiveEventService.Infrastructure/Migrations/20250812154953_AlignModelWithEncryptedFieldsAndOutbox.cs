using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveEventService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignModelWithEncryptedFieldsAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure Outbox table exists (idempotent for local/dev where older migrations may be missing)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""OutboxMessages"" (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""EventType"" character varying(512) NOT NULL,
                    ""Payload"" text NOT NULL,
                    ""OccurredOn"" timestamp with time zone NOT NULL,
                    ""Status"" integer NOT NULL,
                    ""TryCount"" integer NOT NULL,
                    ""LastError"" text NULL,
                    ""NextAttemptAt"" timestamp with time zone NULL,
                    ""ClaimedBy"" text NULL,
                    ""ClaimedAt"" timestamp with time zone NULL
                );
            ");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_OutboxMessages_Status_NextAttemptAt"" ON ""OutboxMessages"" (""Status"", ""NextAttemptAt"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_OutboxMessages_Status_ClaimedAt"" ON ""OutboxMessages"" (""Status"", ""ClaimedAt"");");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            // Note: Outbox table and performance indexes are created by previous migrations.
            // This migration focuses only on adjusting User field lengths for encryption tolerance.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op for Outbox in Down to avoid dropping data in local/dev
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
