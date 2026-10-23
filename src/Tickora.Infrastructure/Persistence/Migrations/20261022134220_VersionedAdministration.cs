using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionedAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Responses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Responses",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Categories",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Categories");
        }
    }
}
