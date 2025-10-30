using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RenameOutfitColumnsAndAddIsSaved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isFavorite",
                table: "Outfit",
                newName: "IsFavorite");

            migrationBuilder.AddColumn<bool>(
                name: "IsSaved",
                table: "Outfit",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSaved",
                table: "Outfit");

            migrationBuilder.RenameColumn(
                name: "IsFavorite",
                table: "Outfit",
                newName: "isFavorite");
        }
    }
}
