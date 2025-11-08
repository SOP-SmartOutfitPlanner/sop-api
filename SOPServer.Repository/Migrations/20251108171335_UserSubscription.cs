using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UserSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscription_SubscriptionPlans_SubscriptionPlanId",
                table: "UserSubscription");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscription_User_UserId",
                table: "UserSubscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSubscription",
                table: "UserSubscription");

            migrationBuilder.RenameTable(
                name: "UserSubscription",
                newName: "UserSubscriptions");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscription_UserId",
                table: "UserSubscriptions",
                newName: "IX_UserSubscriptions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscription_SubscriptionPlanId",
                table: "UserSubscriptions",
                newName: "IX_UserSubscriptions_SubscriptionPlanId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSubscriptions",
                table: "UserSubscriptions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_User_UserId",
                table: "UserSubscriptions",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_User_UserId",
                table: "UserSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSubscriptions",
                table: "UserSubscriptions");

            migrationBuilder.RenameTable(
                name: "UserSubscriptions",
                newName: "UserSubscription");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscription",
                newName: "IX_UserSubscription_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscription",
                newName: "IX_UserSubscription_SubscriptionPlanId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSubscription",
                table: "UserSubscription",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscription_SubscriptionPlans_SubscriptionPlanId",
                table: "UserSubscription",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscription_User_UserId",
                table: "UserSubscription",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
