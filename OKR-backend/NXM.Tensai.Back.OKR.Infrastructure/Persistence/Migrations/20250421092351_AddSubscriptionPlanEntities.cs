using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NXM.Tensai.Back.OKR.Infrastructure
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlanEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Interval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlanType = table.Column<byte>(type: "smallint", nullable: false),
                    StripeProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StripePriceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanFeature",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanFeature", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanFeature_SubscriptionPlanEntity_Subscription~",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlanEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanFeature_SubscriptionPlanId",
                table: "SubscriptionPlanFeature",
                column: "SubscriptionPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlanFeature");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanEntity");
        }
    }
}
