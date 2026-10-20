using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeSkillsBilingual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Skills",
                newName: "NameEn");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Skills",
                newName: "DescriptionEn");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Skills",
                newName: "CategoryEn");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                newName: "IX_Skills_NameEn");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_Category",
                table: "Skills",
                newName: "IX_Skills_CategoryEn");

            migrationBuilder.AddColumn<string>(
                name: "CategoryAr",
                table: "Skills",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Skills",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Skills",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryAr",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Skills");

            migrationBuilder.RenameColumn(
                name: "NameEn",
                table: "Skills",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "DescriptionEn",
                table: "Skills",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "CategoryEn",
                table: "Skills",
                newName: "Category");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_NameEn",
                table: "Skills",
                newName: "IX_Skills_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_CategoryEn",
                table: "Skills",
                newName: "IX_Skills_Category");
        }
    }
}
