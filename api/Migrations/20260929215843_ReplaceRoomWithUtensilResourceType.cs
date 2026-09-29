using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRoomWithUtensilResourceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Resources\" SET \"TypeId\" = '3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f04' WHERE \"TypeId\" = '3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f01';");

            migrationBuilder.DeleteData(
                table: "ResourceTypes",
                keyColumn: "Id",
                keyValue: new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f01"));

            migrationBuilder.InsertData(
                table: "ResourceTypes",
                columns: new[] { "Id", "Name", "OrganizationId" },
                values: new object[] { new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f05"), "Utensil", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Resources\" SET \"TypeId\" = '3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f04' WHERE \"TypeId\" = '3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f05';");

            migrationBuilder.DeleteData(
                table: "ResourceTypes",
                keyColumn: "Id",
                keyValue: new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f05"));

            migrationBuilder.InsertData(
                table: "ResourceTypes",
                columns: new[] { "Id", "Name", "OrganizationId" },
                values: new object[] { new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f01"), "Room", null });
        }
    }
}
