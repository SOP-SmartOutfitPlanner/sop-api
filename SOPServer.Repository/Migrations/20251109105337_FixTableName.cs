using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class FixTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutfitItems");

            migrationBuilder.CreateTable(
                name: "OutfitItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutfitId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OutfitIt__3214EC07", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutfitItem_Item",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutfitItem_Outfit",
                        column: x => x.OutfitId,
                        principalTable: "Outfit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutfitItem_ItemId",
                table: "OutfitItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitItem_OutfitId",
                table: "OutfitItem",
                column: "OutfitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutfitItem");

            migrationBuilder.CreateTable(
                name: "OutfitItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    OutfitId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OutfitIt__3214EC07", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutfitItems_Item",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutfitItems_Outfit",
                        column: x => x.OutfitId,
                        principalTable: "Outfit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutfitItems_ItemId",
                table: "OutfitItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitItems_OutfitId",
                table: "OutfitItems",
                column: "OutfitId");
        }
    }
}
