using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSubscriptionBenefits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_User_UserId",
                table: "UserSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptionTransaction_UserSubscriptions_UserSubscriptionId",
                table: "UserSubscriptionTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSubscriptions",
                table: "UserSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionPlans",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "AISuggestionLimit",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "ChatRateLimit",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "IsSuggestWeather",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "ItemLimit",
                table: "SubscriptionPlans");

            migrationBuilder.RenameTable(
                name: "UserSubscriptions",
                newName: "UserSubscription");

            migrationBuilder.RenameTable(
                name: "SubscriptionPlans",
                newName: "SubscriptionPlan");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscription",
                newName: "IX_UserSubscription_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscription",
                newName: "IX_UserSubscription_SubscriptionPlanId");

            migrationBuilder.AddColumn<string>(
                name: "BenefitUsed",
                table: "UserSubscription",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SubscriptionPlan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SubscriptionPlan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BenefitLimit",
                table: "SubscriptionPlan",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK__UserSubscription__3214EC07",
                table: "UserSubscription",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK__SubscriptionPlan__3214EC07",
                table: "SubscriptionPlan",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscription_SubscriptionPlan",
                table: "UserSubscription",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscription_User",
                table: "UserSubscription",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptionTransaction_UserSubscription_UserSubscriptionId",
                table: "UserSubscriptionTransaction",
                column: "UserSubscriptionId",
                principalTable: "UserSubscription",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscription_SubscriptionPlan",
                table: "UserSubscription");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscription_User",
                table: "UserSubscription");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptionTransaction_UserSubscription_UserSubscriptionId",
                table: "UserSubscriptionTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK__UserSubscription__3214EC07",
                table: "UserSubscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK__SubscriptionPlan__3214EC07",
                table: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "BenefitUsed",
                table: "UserSubscription");

            migrationBuilder.DropColumn(
                name: "BenefitLimit",
                table: "SubscriptionPlan");

            migrationBuilder.RenameTable(
                name: "UserSubscription",
                newName: "UserSubscriptions");

            migrationBuilder.RenameTable(
                name: "SubscriptionPlan",
                newName: "SubscriptionPlans");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscription_UserId",
                table: "UserSubscriptions",
                newName: "IX_UserSubscriptions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscription_SubscriptionPlanId",
                table: "UserSubscriptions",
                newName: "IX_UserSubscriptions_SubscriptionPlanId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SubscriptionPlans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SubscriptionPlans",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AISuggestionLimit",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatRateLimit",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuggestWeather",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ItemLimit",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSubscriptions",
                table: "UserSubscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionPlans",
                table: "SubscriptionPlans",
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

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptionTransaction_UserSubscriptions_UserSubscriptionId",
                table: "UserSubscriptionTransaction",
                column: "UserSubscriptionId",
                principalTable: "UserSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
