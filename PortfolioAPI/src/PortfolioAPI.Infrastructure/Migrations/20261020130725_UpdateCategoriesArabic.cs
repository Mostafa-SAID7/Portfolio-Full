using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoriesArabic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing categories with their Arabic names
            migrationBuilder.Sql(@"
                UPDATE [Categories] SET [NameAr] = N'ملابس' WHERE [NameEn] = 'Clothing';
                UPDATE [Categories] SET [NameAr] = N'جمال' WHERE [NameEn] = 'Beauty';
                UPDATE [Categories] SET [NameAr] = N'مجوهرات' WHERE [NameEn] = 'Jewelry';
                UPDATE [Categories] SET [NameAr] = N'أثاث' WHERE [NameEn] = 'Furniture';
                UPDATE [Categories] SET [NameAr] = N'لوحة تحكم' WHERE [NameEn] = 'Dashboard';
                UPDATE [Categories] SET [NameAr] = N'ويب 3' WHERE [NameEn] = 'Web3';
                UPDATE [Categories] SET [NameAr] = N'رياضة' WHERE [NameEn] = 'Sports';
                UPDATE [Categories] SET [NameAr] = N'ملابس' WHERE [NameEn] = 'Clothing';
                UPDATE [Categories] SET [NameAr] = N'إكسسوارات تقنية' WHERE [NameEn] = 'Tech Accessories';
                UPDATE [Categories] SET [NameAr] = N'باك إند' WHERE [NameEn] = 'Backend';
                UPDATE [Categories] SET [NameAr] = N'أخرى' WHERE [NameEn] = 'Other';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Arabic names to empty
            migrationBuilder.Sql(@"
                UPDATE [Categories] SET [NameAr] = '' WHERE [NameEn] IN ('Clothing', 'Beauty', 'Jewelry', 'Furniture', 'Dashboard', 'Web3', 'Sports', 'Tech Accessories', 'Backend', 'Other');
            ");
        }
    }
}
