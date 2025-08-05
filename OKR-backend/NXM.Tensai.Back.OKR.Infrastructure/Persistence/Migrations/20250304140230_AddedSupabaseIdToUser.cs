using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class AddedSupabaseIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupabaseId",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupabaseId",
                table: "AspNetUsers");
        }
    }
}
