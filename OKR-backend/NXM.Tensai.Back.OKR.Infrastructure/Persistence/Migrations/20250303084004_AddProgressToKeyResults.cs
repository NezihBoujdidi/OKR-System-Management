using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class AddProgressToKeyResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "KeyResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Progress",
                table: "KeyResults");
        }
    }
}
