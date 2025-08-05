using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class AddOKRSessionTeamEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KeyResults_AspNetUsers_UserId",
                table: "KeyResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Objectives_AspNetUsers_UserId",
                table: "Objectives");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRSessions_Teams_TeamId",
                table: "OKRSessions");

            migrationBuilder.DropIndex(
                name: "IX_OKRSessions_TeamId",
                table: "OKRSessions");

            migrationBuilder.DropIndex(
                name: "IX_Objectives_UserId",
                table: "Objectives");

            migrationBuilder.DropIndex(
                name: "IX_KeyResults_UserId",
                table: "KeyResults");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "OKRSessions");

            migrationBuilder.RenameColumn(
                name: "Progression",
                table: "KeyResultTasks",
                newName: "Progress");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Teams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamManagerId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "OKRSessions",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "OKRSessions",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "Objectives",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "Objectives",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleTeamId",
                table: "Objectives",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "KeyResultTasks",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "KeyResultTasks",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "KeyResults",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "KeyResults",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OKRSessionTeams",
                columns: table => new
                {
                    OKRSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKRSessionTeams", x => new { x.OKRSessionId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_OKRSessionTeams_OKRSessions_OKRSessionId",
                        column: x => x.OKRSessionId,
                        principalTable: "OKRSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OKRSessionTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TeamManagerId",
                table: "Teams",
                column: "TeamManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_ResponsibleTeamId",
                table: "Objectives",
                column: "ResponsibleTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRSessionTeams_TeamId",
                table: "OKRSessionTeams",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Objectives_Teams_ResponsibleTeamId",
                table: "Objectives",
                column: "ResponsibleTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_AspNetUsers_TeamManagerId",
                table: "Teams",
                column: "TeamManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Objectives_Teams_ResponsibleTeamId",
                table: "Objectives");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_AspNetUsers_TeamManagerId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "OKRSessionTeams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TeamManagerId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Objectives_ResponsibleTeamId",
                table: "Objectives");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TeamManagerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ResponsibleTeamId",
                table: "Objectives");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Progress",
                table: "KeyResultTasks",
                newName: "Progression");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "OKRSessions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "OKRSessions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "OKRSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "Objectives",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "Objectives",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "KeyResultTasks",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "KeyResultTasks",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "KeyResults",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                table: "KeyResults",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OKRSessions_TeamId",
                table: "OKRSessions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_UserId",
                table: "Objectives",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_UserId",
                table: "KeyResults",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KeyResults_AspNetUsers_UserId",
                table: "KeyResults",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Objectives_AspNetUsers_UserId",
                table: "Objectives",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRSessions_Teams_TeamId",
                table: "OKRSessions",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
