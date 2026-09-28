using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SplitAccountNameIntoFirstAndLast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Accounts");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Accounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Accounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Accounts");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
