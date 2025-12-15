using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTryOnImageUrlToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TryOnImageUrl",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TryOnImageUrl",
                table: "User");
        }
    }
}
