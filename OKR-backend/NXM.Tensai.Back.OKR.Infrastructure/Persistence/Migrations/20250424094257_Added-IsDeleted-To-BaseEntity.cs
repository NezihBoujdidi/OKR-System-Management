using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class AddedIsDeletedToBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SubscriptionPlanFeature",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SubscriptionPlanEntity",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SubscriptionPlanFeature");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SubscriptionPlanEntity");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Organizations");
        }
    }
}
