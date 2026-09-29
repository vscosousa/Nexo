using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nexo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanResourceLimitAndTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResourceLimit",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("6f1c2b1e-7a52-4d0a-9a53-1f3e5c0b8d01"),
                column: "ResourceLimit",
                value: 10);

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "MemberLimit", "Name", "ResourceLimit" },
                values: new object[,]
                {
                    { new Guid("8b2d4f3a-9c61-4e2b-8a64-2f4e6c0b9d02"), 100, "Team", 100 },
                    { new Guid("a1e5c7d4-1b83-4f0c-9b75-3a5f7d1c0e03"), 2147483647, "Enterprise", 2147483647 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("8b2d4f3a-9c61-4e2b-8a64-2f4e6c0b9d02"));

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("a1e5c7d4-1b83-4f0c-9b75-3a5f7d1c0e03"));

            migrationBuilder.DropColumn(
                name: "ResourceLimit",
                table: "Plans");
        }
    }
}
