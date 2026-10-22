using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropDeadCategoryColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The pre-bilingual legacy [Category] column: MakeProjectsBilingual
            // renamed it (Category → ClientEn), so no migrated database has it
            // — except databases where DbInitializer's broken schema checks
            // (comparing TABLE_NAME against the literal '@p0') re-added it via
            // ALTER TABLE on every startup. Guarded so it is a no-op on any
            // database where the column does not exist.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE object_id = OBJECT_ID('dbo.Projects')
                             AND name = 'Category')
                BEGIN
                    ALTER TABLE dbo.Projects DROP COLUMN [Category];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns
                               WHERE object_id = OBJECT_ID('dbo.Projects')
                                 AND name = 'Category')
                BEGIN
                    ALTER TABLE dbo.Projects ADD [Category] nvarchar(100) NULL;
                END
            ");
        }
    }
}
