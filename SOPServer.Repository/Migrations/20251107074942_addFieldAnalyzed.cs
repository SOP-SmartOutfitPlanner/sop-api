using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class addFieldAnalyzed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnalyzed",
                table: "Item",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnalyzed",
                table: "Item");
        }
    }
}
