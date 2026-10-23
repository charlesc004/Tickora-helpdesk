using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SlaAlertIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                table: "TicketActivity",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketActivity_TicketId_EventKey",
                table: "TicketActivity",
                columns: new[] { "TicketId", "EventKey" },
                unique: true,
                filter: "[EventKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketActivity_TicketId_EventKey",
                table: "TicketActivity");

            migrationBuilder.DropColumn(
                name: "EventKey",
                table: "TicketActivity");
        }
    }
}
