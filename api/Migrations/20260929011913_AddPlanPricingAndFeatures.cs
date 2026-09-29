using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanPricingAndFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAiInsights",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDecisionHistory",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasExpenseTracking",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasIncidentTracking",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPrioritySupport",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "Plans",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("6f1c2b1e-7a52-4d0a-9a53-1f3e5c0b8d01"),
                columns: new[] { "HasAiInsights", "HasDecisionHistory", "HasExpenseTracking", "HasIncidentTracking", "HasPrioritySupport", "MonthlyPrice" },
                values: new object[] { false, false, false, false, false, 0m });

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("8b2d4f3a-9c61-4e2b-8a64-2f4e6c0b9d02"),
                columns: new[] { "HasAiInsights", "HasDecisionHistory", "HasExpenseTracking", "HasIncidentTracking", "HasPrioritySupport", "MonthlyPrice" },
                values: new object[] { false, true, true, true, false, 29m });

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("a1e5c7d4-1b83-4f0c-9b75-3a5f7d1c0e03"),
                columns: new[] { "HasAiInsights", "HasDecisionHistory", "HasExpenseTracking", "HasIncidentTracking", "HasPrioritySupport", "MonthlyPrice" },
                values: new object[] { true, true, true, true, true, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAiInsights",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "HasDecisionHistory",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "HasExpenseTracking",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "HasIncidentTracking",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "HasPrioritySupport",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "Plans");
        }
    }
}
