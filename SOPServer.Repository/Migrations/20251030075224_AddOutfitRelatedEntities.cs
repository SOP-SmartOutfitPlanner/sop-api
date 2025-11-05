using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOutfitRelatedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutfitItems_Item_ItemId",
                table: "OutfitItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OutfitItems_Outfits_OutfitId",
                table: "OutfitItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Outfits_User_UserId",
                table: "Outfits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OutfitItems",
                table: "OutfitItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Outfits",
                table: "Outfits");

            migrationBuilder.DropColumn(
                name: "isUsed",
                table: "Outfits");

            migrationBuilder.RenameTable(
                name: "Outfits",
                newName: "Outfit");

            migrationBuilder.RenameIndex(
                name: "IX_Outfits_UserId",
                table: "Outfit",
                newName: "IX_Outfit_UserId");

            migrationBuilder.AlterColumn<bool>(
                name: "isFavorite",
                table: "Outfit",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Outfit",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Outfit",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK__OutfitIt__3214EC07",
                table: "OutfitItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Outfit__3214EC07",
                table: "Outfit",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserOccasion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    OccasionId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateOccasion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WeatherSnapshot = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserOcca__3214EC07", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOccasion_Occasion",
                        column: x => x.OccasionId,
                        principalTable: "Occasion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserOccasion_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OutfitUsageHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    OutfitId = table.Column<long>(type: "bigint", nullable: false),
                    UserOccassionId = table.Column<long>(type: "bigint", nullable: true),
                    DateUsed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OutfitUs__3214EC07", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutfitUsageHistory_Outfit",
                        column: x => x.OutfitId,
                        principalTable: "Outfit",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutfitUsageHistory_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutfitUsageHistory_UserOccasion",
                        column: x => x.UserOccassionId,
                        principalTable: "UserOccasion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutfitUsageHistory_OutfitId",
                table: "OutfitUsageHistory",
                column: "OutfitId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitUsageHistory_UserId",
                table: "OutfitUsageHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitUsageHistory_UserOccassionId",
                table: "OutfitUsageHistory",
                column: "UserOccassionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOccasion_OccasionId",
                table: "UserOccasion",
                column: "OccasionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOccasion_UserId",
                table: "UserOccasion",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Outfit_User",
                table: "Outfit",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitItems_Item",
                table: "OutfitItems",
                column: "ItemId",
                principalTable: "Item",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitItems_Outfit",
                table: "OutfitItems",
                column: "OutfitId",
                principalTable: "Outfit",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Outfit_User",
                table: "Outfit");

            migrationBuilder.DropForeignKey(
                name: "FK_OutfitItems_Item",
                table: "OutfitItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OutfitItems_Outfit",
                table: "OutfitItems");

            migrationBuilder.DropTable(
                name: "OutfitUsageHistory");

            migrationBuilder.DropTable(
                name: "UserOccasion");

            migrationBuilder.DropPrimaryKey(
                name: "PK__OutfitIt__3214EC07",
                table: "OutfitItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Outfit__3214EC07",
                table: "Outfit");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Outfit");

            migrationBuilder.RenameTable(
                name: "Outfit",
                newName: "Outfits");

            migrationBuilder.RenameIndex(
                name: "IX_Outfit_UserId",
                table: "Outfits",
                newName: "IX_Outfits_UserId");

            migrationBuilder.AlterColumn<bool>(
                name: "isFavorite",
                table: "Outfits",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Outfits",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<bool>(
                name: "isUsed",
                table: "Outfits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutfitItems",
                table: "OutfitItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Outfits",
                table: "Outfits",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitItems_Item_ItemId",
                table: "OutfitItems",
                column: "ItemId",
                principalTable: "Item",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitItems_Outfits_OutfitId",
                table: "OutfitItems",
                column: "OutfitId",
                principalTable: "Outfits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Outfits_User_UserId",
                table: "Outfits",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
