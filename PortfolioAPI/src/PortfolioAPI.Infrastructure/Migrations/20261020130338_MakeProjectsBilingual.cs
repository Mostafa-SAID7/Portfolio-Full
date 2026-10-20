using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeProjectsBilingual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index IX_Projects_Category may not exist - handle gracefully
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Projects_Category' AND object_id = OBJECT_ID('Projects'))
                BEGIN
                    DROP INDEX IX_Projects_Category ON Projects;
                END");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Projects",
                newName: "TypeEn");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Projects",
                newName: "TitleEn");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Projects",
                newName: "TypeAr");

            migrationBuilder.RenameColumn(
                name: "Metrics",
                table: "Projects",
                newName: "MetricsEn");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Projects",
                newName: "DescriptionEn");

            migrationBuilder.RenameColumn(
                name: "Database",
                table: "Projects",
                newName: "DatabaseEn");

            migrationBuilder.RenameColumn(
                name: "Client",
                table: "Projects",
                newName: "DatabaseAr");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Projects",
                newName: "ClientEn");

            migrationBuilder.RenameColumn(
                name: "Badges",
                table: "Projects",
                newName: "BadgesEn");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_Title",
                table: "Projects",
                newName: "IX_Projects_TitleEn");

            migrationBuilder.AddColumn<string>(
                name: "BadgesAr",
                table: "Projects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryAr",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryEn",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientAr",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Projects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetricsAr",
                table: "Projects",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusAr",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusEn",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleAr",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CategoryEn",
                table: "Projects",
                column: "CategoryEn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_CategoryEn",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BadgesAr",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CategoryAr",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CategoryEn",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ClientAr",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "MetricsAr",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StatusAr",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StatusEn",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TitleAr",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "TypeEn",
                table: "Projects",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "TypeAr",
                table: "Projects",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "TitleEn",
                table: "Projects",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "MetricsEn",
                table: "Projects",
                newName: "Metrics");

            migrationBuilder.RenameColumn(
                name: "DescriptionEn",
                table: "Projects",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DatabaseEn",
                table: "Projects",
                newName: "Database");

            migrationBuilder.RenameColumn(
                name: "DatabaseAr",
                table: "Projects",
                newName: "Client");

            migrationBuilder.RenameColumn(
                name: "ClientEn",
                table: "Projects",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "BadgesEn",
                table: "Projects",
                newName: "Badges");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_TitleEn",
                table: "Projects",
                newName: "IX_Projects_Title");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Category",
                table: "Projects",
                column: "Category");
        }
    }
}
