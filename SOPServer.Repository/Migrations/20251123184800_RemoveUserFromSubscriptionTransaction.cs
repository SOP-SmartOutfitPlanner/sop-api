using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserFromSubscriptionTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptionTransaction_User_UserId",
                table: "UserSubscriptionTransaction");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptionTransaction_UserId",
                table: "UserSubscriptionTransaction");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserSubscriptionTransaction");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserSubscriptionTransaction",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptionTransaction_UserId",
                table: "UserSubscriptionTransaction",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptionTransaction_User_UserId",
                table: "UserSubscriptionTransaction",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
