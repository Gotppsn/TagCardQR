using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardTagManager.Migrations
{
    /// <inheritdoc />
    public partial class AddIconColorToTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconColor",
                table: "Templates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconColor",
                table: "Templates");
        }
    }
}
