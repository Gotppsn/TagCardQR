using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardTagManager.Migrations
{
    /// <inheritdoc />
    public partial class AddScanresults2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IssueDetected",
                table: "ScanResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RelatedIssueId",
                table: "ScanResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanCategory",
                table: "ScanResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScanContext",
                table: "ScanResults",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_RelatedIssueId",
                table: "ScanResults",
                column: "RelatedIssueId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScanResults_IssueReports_RelatedIssueId",
                table: "ScanResults",
                column: "RelatedIssueId",
                principalTable: "IssueReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScanResults_IssueReports_RelatedIssueId",
                table: "ScanResults");

            migrationBuilder.DropIndex(
                name: "IX_ScanResults_RelatedIssueId",
                table: "ScanResults");

            migrationBuilder.DropColumn(
                name: "IssueDetected",
                table: "ScanResults");

            migrationBuilder.DropColumn(
                name: "RelatedIssueId",
                table: "ScanResults");

            migrationBuilder.DropColumn(
                name: "ScanCategory",
                table: "ScanResults");

            migrationBuilder.DropColumn(
                name: "ScanContext",
                table: "ScanResults");
        }
    }
}
