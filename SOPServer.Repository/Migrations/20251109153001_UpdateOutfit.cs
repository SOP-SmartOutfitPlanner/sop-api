using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOutfit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OutfitId1",
                table: "CollectionOutfit",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionOutfit_OutfitId1",
                table: "CollectionOutfit",
                column: "OutfitId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionOutfit_Outfit_OutfitId1",
                table: "CollectionOutfit",
                column: "OutfitId1",
                principalTable: "Outfit",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionOutfit_Outfit_OutfitId1",
                table: "CollectionOutfit");

            migrationBuilder.DropIndex(
                name: "IX_CollectionOutfit_OutfitId1",
                table: "CollectionOutfit");

            migrationBuilder.DropColumn(
                name: "OutfitId1",
                table: "CollectionOutfit");
        }
    }
}
