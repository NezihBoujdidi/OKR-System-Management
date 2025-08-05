using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class UpdateOKRSessionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OKRSessions_AspNetUsers_UserId",
                table: "OKRSessions");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "OKRSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OKRSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityDate",
                table: "OKRSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "OKRSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamManagerId",
                table: "OKRSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OKRSessions_IsActive",
                table: "OKRSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OKRSessions_TeamManagerId",
                table: "OKRSessions",
                column: "TeamManagerId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OKRSessions_AspNetUsers_TeamManagerId",
                table: "OKRSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRSessions_AspNetUsers_UserId",
                table: "OKRSessions");

            migrationBuilder.DropIndex(
                name: "IX_OKRSessions_IsActive",
                table: "OKRSessions");

            migrationBuilder.DropIndex(
                name: "IX_OKRSessions_TeamManagerId",
                table: "OKRSessions");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "OKRSessions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OKRSessions");

            migrationBuilder.DropColumn(
                name: "LastActivityDate",
                table: "OKRSessions");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "OKRSessions");

            migrationBuilder.DropColumn(
                name: "TeamManagerId",
                table: "OKRSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_OKRSessions_AspNetUsers_UserId",
                table: "OKRSessions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
