using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveEventService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserEventRegistrationCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventRegistrations_Users_UserId",
                table: "EventRegistrations");

            migrationBuilder.AddColumn<bool>(
                name: "IsWaitlistOpen",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_EventRegistrations_Users_UserId",
                table: "EventRegistrations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventRegistrations_Users_UserId",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "IsWaitlistOpen",
                table: "Events");

            migrationBuilder.AddForeignKey(
                name: "FK_EventRegistrations_Users_UserId",
                table: "EventRegistrations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
