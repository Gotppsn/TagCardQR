using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CardTagManager.Migrations
{
    /// <inheritdoc />
    public partial class AddNewRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Manager with limited administrative access, Department Access menu and Scan results");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Standard user with departmental access, can't access Department Access menu");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "CreatedAt", "Description", "Name", "NormalizedName", "UpdatedAt" },
                values: new object[,]
                {
                    { 4, "5a92f40b-7c2e-4ec9-a637-d7d3c8e9f8a0", new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Can access Scan results and edit departmental data with authorized access", "Edit", "EDIT", null },
                    { 5, "7b81d6c1-3e7f-48d3-9c2b-1b7ba4582c9d", new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Limited access to view QR codes only", "View", "VIEW", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Manager with limited administrative access");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Standard user with basic access");
        }
    }
}
