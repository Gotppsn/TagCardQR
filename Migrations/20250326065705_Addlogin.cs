using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardTagManager.Migrations
{
    /// <inheritdoc />
    public partial class Addlogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail_TH_FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail_TH_LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail_EN_FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail_EN_LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    User_Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Plant_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    User_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstLoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
