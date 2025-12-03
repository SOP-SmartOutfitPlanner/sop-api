using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedItemsAndOutfitsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaveItemFromPost",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveItemFromPost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaveItemFromPost_Item",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaveItemFromPost_Post",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaveItemFromPost_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SaveOutfitFromCollection",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    OutfitId = table.Column<long>(type: "bigint", nullable: false),
                    CollectionId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveOutfitFromCollection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaveOutfitFromCollection_Collection",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaveOutfitFromCollection_Outfit",
                        column: x => x.OutfitId,
                        principalTable: "Outfit",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaveOutfitFromCollection_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SaveOutfitFromPost",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    OutfitId = table.Column<long>(type: "bigint", nullable: false),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveOutfitFromPost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaveOutfitFromPost_Outfit",
                        column: x => x.OutfitId,
                        principalTable: "Outfit",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaveOutfitFromPost_Post",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaveOutfitFromPost_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaveItemFromPost_ItemId",
                table: "SaveItemFromPost",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveItemFromPost_PostId",
                table: "SaveItemFromPost",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveItemFromPost_UserId_ItemId_PostId",
                table: "SaveItemFromPost",
                columns: new[] { "UserId", "ItemId", "PostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaveOutfitFromCollection_CollectionId",
                table: "SaveOutfitFromCollection",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveOutfitFromCollection_OutfitId",
                table: "SaveOutfitFromCollection",
                column: "OutfitId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveOutfitFromCollection_UserId_OutfitId_CollectionId",
                table: "SaveOutfitFromCollection",
                columns: new[] { "UserId", "OutfitId", "CollectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaveOutfitFromPost_OutfitId",
                table: "SaveOutfitFromPost",
                column: "OutfitId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveOutfitFromPost_PostId",
                table: "SaveOutfitFromPost",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveOutfitFromPost_UserId_OutfitId_PostId",
                table: "SaveOutfitFromPost",
                columns: new[] { "UserId", "OutfitId", "PostId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaveItemFromPost");

            migrationBuilder.DropTable(
                name: "SaveOutfitFromCollection");

            migrationBuilder.DropTable(
                name: "SaveOutfitFromPost");
        }
    }
}
