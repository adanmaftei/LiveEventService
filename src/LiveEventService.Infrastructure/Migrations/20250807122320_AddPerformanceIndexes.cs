using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveEventService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Performance indexes for EventRegistrations

            // 1. Composite index for waitlist queries (EventId + Status + PositionInQueue)
            // This is critical for waitlist operations and position calculations
            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_EventId_Status_PositionInQueue",
                table: "EventRegistrations",
                columns: new[] { "EventId", "Status", "PositionInQueue" });

            // 2. Composite index for chronological registration queries (EventId + Status + CreatedAt)
            // Useful for determining registration order and waitlist position calculations
            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_EventId_Status_CreatedAt",
                table: "EventRegistrations",
                columns: new[] { "EventId", "Status", "CreatedAt" });

            // 3. Composite index for user status queries (UserId + Status)
            // Optimizes queries for user's registration status across events
            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_UserId_Status",
                table: "EventRegistrations",
                columns: new[] { "UserId", "Status" });

            // 4. Partial index for waitlisted registrations only
            // Highly optimized for waitlist-specific operations
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_EventRegistrations_EventId_Waitlisted_Position"" 
                ON ""EventRegistrations"" (""EventId"", ""PositionInQueue"") 
                WHERE ""Status"" = 2"); // 2 = Waitlisted status (int enum value)

            // Performance indexes for Events

            // 5. Composite index for published events by date (IsPublished + StartDate)
            // Optimizes the common query for listing published events chronologically
            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished_StartDate",
                table: "Events",
                columns: new[] { "IsPublished", "StartDate" });

            // 5b. Organizer lists by date (OrganizerId + StartDate)
            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizerId_StartDate",
                table: "Events",
                columns: new[] { "OrganizerId", "StartDate" });

            // 6. Index for event capacity queries (Capacity)
            // Useful for finding events with available capacity
            migrationBuilder.CreateIndex(
                name: "IX_Events_Capacity",
                table: "Events",
                column: "Capacity");

            // Performance indexes for Users

            // 7. Index for active users (IsActive)
            // Optimizes queries filtering by active users
            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            // 8. Registration filter + sort (EventId, Status, RegistrationDate)
            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_EventId_Status_RegistrationDate",
                table: "EventRegistrations",
                columns: new[] { "EventId", "Status", "RegistrationDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop performance indexes in reverse order

            // Drop Users indexes
            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            // Drop Events indexes
            migrationBuilder.DropIndex(
                name: "IX_Events_Capacity",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsPublished_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_OrganizerId_StartDate",
                table: "Events");

            // Drop EventRegistrations indexes
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_EventRegistrations_EventId_Waitlisted_Position\"");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_UserId_Status",
                table: "EventRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_EventId_Status_CreatedAt",
                table: "EventRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_EventId_Status_PositionInQueue",
                table: "EventRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_EventId_Status_RegistrationDate",
                table: "EventRegistrations");
        }
    }
}
