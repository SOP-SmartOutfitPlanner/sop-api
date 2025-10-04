using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class DeleteItemGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemGoal");

            migrationBuilder.DropTable(
                name: "Goal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Goal__3214EC07C1B22C27", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemGoal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ItemGoal__3214EC07628E8E1B", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemGoal_Goal",
                        column: x => x.GoalId,
                        principalTable: "Goal",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemGoal_Item",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemGoal_GoalId",
                table: "ItemGoal",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGoal_ItemId",
                table: "ItemGoal",
                column: "ItemId");
        }
    }
}
