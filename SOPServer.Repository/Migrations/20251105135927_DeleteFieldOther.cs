using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class DeleteFieldOther : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Other",
                table: "Style");

            migrationBuilder.DropColumn(
                name: "Other",
                table: "Job");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Other",
                table: "Style",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Other",
                table: "Job",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
