using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LiveEventService.Infrastructure.Migrations
{
    public partial class AddOutbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    EventType = table.Column<string>(maxLength: 512, nullable: false),
                    Payload = table.Column<string>(nullable: false),
                    OccurredOn = table.Column<DateTime>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    TryCount = table.Column<int>(nullable: false),
                    LastError = table.Column<string>(nullable: true),
                    NextAttemptAt = table.Column<DateTime>(nullable: true),
                    ClaimedBy = table.Column<string>(nullable: true),
                    ClaimedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_NextAttemptAt",
                table: "OutboxMessages",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_ClaimedAt",
                table: "OutboxMessages",
                columns: new[] { "Status", "ClaimedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OutboxMessages");
        }
    }
}


