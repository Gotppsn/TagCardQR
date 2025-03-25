using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardTagManager.Migrations
{
    /// <inheritdoc />
    public partial class Addbyid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "User_Code",
                table: "Cards");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByID",
                table: "Cards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByID",
                table: "Cards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByID",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "UpdatedByID",
                table: "Cards");

            migrationBuilder.AddColumn<string>(
                name: "User_Code",
                table: "Cards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
