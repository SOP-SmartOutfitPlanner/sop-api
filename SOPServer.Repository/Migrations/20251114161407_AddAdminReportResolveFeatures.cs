using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminReportResolveFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunities_CommentPost_CommentPostId",
                table: "ReportCommunities");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunities_Post_PostId",
                table: "ReportCommunities");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunities_User_UserId",
                table: "ReportCommunities");

            migrationBuilder.RenameColumn(
                name: "CommentPostId",
                table: "ReportCommunities",
                newName: "ResolvedByAdminId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportCommunities_CommentPostId",
                table: "ReportCommunities",
                newName: "IX_ReportCommunities_ResolvedByAdminId");

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "ReportCommunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "ReportCommunities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Post",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "CommentPost",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserSuspensions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByAdminId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSuspensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSuspension_CreatedByAdmin",
                        column: x => x.CreatedByAdminId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSuspension_Report",
                        column: x => x.ReportId,
                        principalTable: "ReportCommunities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSuspension_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserViolations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ViolationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserViolation_Report",
                        column: x => x.ReportId,
                        principalTable: "ReportCommunities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserViolation_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportCommunities_CommentId",
                table: "ReportCommunities",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSuspensions_CreatedByAdminId",
                table: "UserSuspensions",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSuspensions_ReportId",
                table: "UserSuspensions",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSuspensions_UserId",
                table: "UserSuspensions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserViolations_ReportId",
                table: "UserViolations",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_UserViolations_UserId",
                table: "UserViolations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunity_CommentPost",
                table: "ReportCommunities",
                column: "CommentId",
                principalTable: "CommentPost",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunity_Post",
                table: "ReportCommunities",
                column: "PostId",
                principalTable: "Post",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunity_ResolvedByAdmin",
                table: "ReportCommunities",
                column: "ResolvedByAdminId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunity_User",
                table: "ReportCommunities",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunity_CommentPost",
                table: "ReportCommunities");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunity_Post",
                table: "ReportCommunities");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunity_ResolvedByAdmin",
                table: "ReportCommunities");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCommunity_User",
                table: "ReportCommunities");

            migrationBuilder.DropTable(
                name: "UserSuspensions");

            migrationBuilder.DropTable(
                name: "UserViolations");

            migrationBuilder.DropIndex(
                name: "IX_ReportCommunities_CommentId",
                table: "ReportCommunities");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "ReportCommunities");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "ReportCommunities");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "CommentPost");

            migrationBuilder.RenameColumn(
                name: "ResolvedByAdminId",
                table: "ReportCommunities",
                newName: "CommentPostId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportCommunities_ResolvedByAdminId",
                table: "ReportCommunities",
                newName: "IX_ReportCommunities_CommentPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunities_CommentPost_CommentPostId",
                table: "ReportCommunities",
                column: "CommentPostId",
                principalTable: "CommentPost",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunities_Post_PostId",
                table: "ReportCommunities",
                column: "PostId",
                principalTable: "Post",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCommunities_User_UserId",
                table: "ReportCommunities",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
