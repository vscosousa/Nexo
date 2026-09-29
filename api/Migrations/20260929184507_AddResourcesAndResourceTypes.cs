using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nexo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResourcesAndResourceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCustomResourceTypes",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ResourceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resources_ResourceTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("6f1c2b1e-7a52-4d0a-9a53-1f3e5c0b8d01"),
                column: "HasCustomResourceTypes",
                value: false);

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("8b2d4f3a-9c61-4e2b-8a64-2f4e6c0b9d02"),
                column: "HasCustomResourceTypes",
                value: true);

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("a1e5c7d4-1b83-4f0c-9b75-3a5f7d1c0e03"),
                column: "HasCustomResourceTypes",
                value: true);

            migrationBuilder.InsertData(
                table: "ResourceTypes",
                columns: new[] { "Id", "Name", "OrganizationId" },
                values: new object[,]
                {
                    { new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f01"), "Room", null },
                    { new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f02"), "Equipment", null },
                    { new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f03"), "Vehicle", null },
                    { new Guid("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f04"), "Other", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_OrganizationId",
                table: "Resources",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TypeId",
                table: "Resources",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_Name",
                table: "ResourceTypes",
                column: "Name",
                unique: true,
                filter: "\"OrganizationId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_OrganizationId_Name",
                table: "ResourceTypes",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"OrganizationId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "ResourceTypes");

            migrationBuilder.DropColumn(
                name: "HasCustomResourceTypes",
                table: "Plans");
        }
    }
}
