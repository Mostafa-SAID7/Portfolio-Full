using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairProjectCategoryData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── Repair for MakeProjectsBilingual's rename chain ───────────────
            //
            // The bilingual migration renamed the pre-bilingual columns in a
            // chain the model never intended:
            //     Category → ClientEn
            // which silently moved every project's CATEGORY NAME into the
            // client fields (verified live: all 14 projects had CategoryEn
            // NULL while ClientEn/ClientAr held exactly "Clothing", "Coffe",
            // "Food", "Jewelry", "Furniture", "Beauty", "Dashboard"...).
            //
            // 1) Restore the category into its real column.
            // 2) Clear the misplaced text from the client fields — but ONLY
            //    where it exactly equals a managed category name, so a
            //    genuine client name (if one ever existed) survives.
            migrationBuilder.Sql(@"
                UPDATE dbo.Projects
                SET CategoryEn = ClientEn
                WHERE (CategoryEn IS NULL OR LTRIM(RTRIM(CategoryEn)) = '')
                  AND ClientEn IS NOT NULL
                  AND LTRIM(RTRIM(ClientEn)) <> '';

                UPDATE dbo.Projects
                SET ClientEn = NULL,
                    ClientAr = NULL
                WHERE EXISTS (
                        SELECT 1
                        FROM dbo.Categories c
                        WHERE c.NameEn = dbo.Projects.ClientEn
                          AND dbo.Projects.ClientEn IS NOT NULL)
                   OR EXISTS (
                        SELECT 1
                        FROM dbo.Categories c
                        WHERE c.NameAr = dbo.Projects.ClientAr
                          AND dbo.Projects.ClientAr IS NOT NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort inverse: move the restored category back into the
            // empty client fields. CategoryAr cleared by step 2 cannot be
            // reconstructed (it held no distinct data), which is acceptable —
            // Down here exists for rollback symmetry, not data resurrection.
            migrationBuilder.Sql(@"
                UPDATE dbo.Projects
                SET ClientEn = CategoryEn
                WHERE CategoryEn IS NOT NULL
                  AND ClientEn IS NULL;

                UPDATE dbo.Projects
                SET CategoryEn = NULL
                WHERE CategoryEn IS NOT NULL
                  AND ClientEn = CategoryEn;
            ");
        }
    }
}
