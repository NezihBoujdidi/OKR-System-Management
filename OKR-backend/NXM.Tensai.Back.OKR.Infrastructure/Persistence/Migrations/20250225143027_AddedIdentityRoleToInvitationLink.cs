using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class AddedIdentityRoleToInvitationLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "InvitationLinks");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "InvitationLinks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_InvitationLinks_RoleId",
                table: "InvitationLinks",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationLinks_AspNetRoles_RoleId",
                table: "InvitationLinks",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvitationLinks_AspNetRoles_RoleId",
                table: "InvitationLinks");

            migrationBuilder.DropIndex(
                name: "IX_InvitationLinks_RoleId",
                table: "InvitationLinks");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "InvitationLinks");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "InvitationLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
