using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateItemUsageCountAndWornHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrequencyWorn",
                table: "Item");

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "Item",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WornAtHistoryJson",
                table: "Item",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "WornAtHistoryJson",
                table: "Item");

            migrationBuilder.AddColumn<string>(
                name: "FrequencyWorn",
                table: "Item",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
