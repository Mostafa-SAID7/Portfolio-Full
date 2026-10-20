using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeExperienceBilingual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Experiences_Company",
                table: "Experiences");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "Experiences",
                newName: "PositionEn");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Experiences",
                newName: "LocationEn");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Experiences",
                newName: "DescriptionEn");

            migrationBuilder.RenameColumn(
                name: "Company",
                table: "Experiences",
                newName: "CompanyEn");

            migrationBuilder.AddColumn<string>(
                name: "CompanyAr",
                table: "Experiences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Experiences",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LocationAr",
                table: "Experiences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PositionAr",
                table: "Experiences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_CompanyEn",
                table: "Experiences",
                column: "CompanyEn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Experiences_CompanyEn",
                table: "Experiences");

            migrationBuilder.DropColumn(
                name: "CompanyAr",
                table: "Experiences");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Experiences");

            migrationBuilder.DropColumn(
                name: "LocationAr",
                table: "Experiences");

            migrationBuilder.DropColumn(
                name: "PositionAr",
                table: "Experiences");

            migrationBuilder.RenameColumn(
                name: "PositionEn",
                table: "Experiences",
                newName: "Position");

            migrationBuilder.RenameColumn(
                name: "CompanyEn",
                table: "Experiences",
                newName: "Company");

            migrationBuilder.RenameColumn(
                name: "LocationEn",
                table: "Experiences",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "DescriptionEn",
                table: "Experiences",
                newName: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_Company",
                table: "Experiences",
                column: "Company");
        }
    }
}
