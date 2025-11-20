using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTransactionCodeToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TransactionCodeNew",
                table: "UserSubscriptionTransaction",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE UserSubscriptionTransaction
                SET TransactionCodeNew = ABS(CAST(CHECKSUM(TransactionCode) AS INT) % 2147483647)
            ");

            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "UserSubscriptionTransaction");

            migrationBuilder.RenameColumn(
                name: "TransactionCodeNew",
                table: "UserSubscriptionTransaction",
                newName: "TransactionCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TransactionCode",
                table: "UserSubscriptionTransaction",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
