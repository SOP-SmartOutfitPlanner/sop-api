using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddReportReporterTableFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunity_User",
                table: "ReportCommunities");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportReporters_ReportCommunities_ReportCommunityId",
                table: "ReportReporters");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportReporters_User_UserId",
                table: "ReportReporters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReportReporters",
                table: "ReportReporters");

            migrationBuilder.DropIndex(
                name: "IX_ReportReporters_ReportCommunityId",
                table: "ReportReporters");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ReportCommunities");

            migrationBuilder.RenameTable(
                name: "ReportReporters",
                newName: "ReportReporter");

            migrationBuilder.RenameColumn(
                name: "ReportCommunityId",
                table: "ReportReporter",
                newName: "ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportReporters_UserId",
                table: "ReportReporter",
                newName: "IX_ReportReporter_UserId");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "ReportCommunities",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ReportReporter",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<long>(
                name: "UserId1",
                table: "ReportReporter",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK__ReportReporter__3214EC07",
                table: "ReportReporter",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ReportReporter_ReportId_UserId",
                table: "ReportReporter",
                columns: new[] { "ReportId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportReporter_UserId1",
                table: "ReportReporter",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunities_User_UserId",
                table: "ReportCommunities",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportReporter_ReportCommunity",
                table: "ReportReporter",
                column: "ReportId",
                principalTable: "ReportCommunities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportReporter_User",
                table: "ReportReporter",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportReporter_User_UserId1",
                table: "ReportReporter",
                column: "UserId1",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunities_User_UserId",
                table: "ReportCommunities");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportReporter_ReportCommunity",
                table: "ReportReporter");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportReporter_User",
                table: "ReportReporter");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportReporter_User_UserId1",
                table: "ReportReporter");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ReportReporter__3214EC07",
                table: "ReportReporter");

            migrationBuilder.DropIndex(
                name: "IX_ReportReporter_ReportId_UserId",
                table: "ReportReporter");

            migrationBuilder.DropIndex(
                name: "IX_ReportReporter_UserId1",
                table: "ReportReporter");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "ReportReporter");

            migrationBuilder.RenameTable(
                name: "ReportReporter",
                newName: "ReportReporters");

            migrationBuilder.RenameColumn(
                name: "ReportId",
                table: "ReportReporters",
                newName: "ReportCommunityId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportReporter_UserId",
                table: "ReportReporters",
                newName: "IX_ReportReporters_UserId");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "ReportCommunities",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ReportCommunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ReportReporters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReportReporters",
                table: "ReportReporters",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ReportReporters_ReportCommunityId",
                table: "ReportReporters",
                column: "ReportCommunityId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunity_User",
                table: "ReportCommunities",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportReporters_ReportCommunities_ReportCommunityId",
                table: "ReportReporters",
                column: "ReportCommunityId",
                principalTable: "ReportCommunities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportReporters_User_UserId",
                table: "ReportReporters",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
