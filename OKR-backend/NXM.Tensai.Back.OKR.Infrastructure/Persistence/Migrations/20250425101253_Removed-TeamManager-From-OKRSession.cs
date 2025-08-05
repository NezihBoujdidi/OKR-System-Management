using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class RemovedTeamManagerFromOKRSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KeyResultTasks_AspNetUsers_UserId",
                table: "KeyResultTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRSessions_AspNetUsers_TeamManagerId",
                table: "OKRSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRSessions_AspNetUsers_UserId",
                table: "OKRSessions");

            migrationBuilder.DropIndex(
                name: "IX_OKRSessions_TeamManagerId",
                table: "OKRSessions");

            migrationBuilder.DropIndex(
                name: "IX_KeyResultTasks_UserId",
                table: "KeyResultTasks");

            migrationBuilder.DropColumn(
                name: "LastActivityDate",
                table: "OKRSessions");

            migrationBuilder.DropColumn(
                name: "TeamManagerId",
                table: "OKRSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityDate",
                table: "OKRSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "TeamManagerId",
                table: "OKRSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OKRSessions_TeamManagerId",
                table: "OKRSessions",
                column: "TeamManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResultTasks_UserId",
                table: "KeyResultTasks",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KeyResultTasks_AspNetUsers_UserId",
                table: "KeyResultTasks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRSessions_AspNetUsers_TeamManagerId",
                table: "OKRSessions",
                column: "TeamManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRSessions_AspNetUsers_UserId",
                table: "OKRSessions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
