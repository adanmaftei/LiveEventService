using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveEventService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureOutboxExists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create OutboxMessages table if missing (idempotent)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op to avoid dropping data in local/dev
        }
    }
}
