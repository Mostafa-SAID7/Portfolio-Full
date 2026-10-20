using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoriesArabicData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing categories with their Arabic names using N prefix for Unicode
            migrationBuilder.Sql(@"
                UPDATE [Categories] SET [NameAr] = N'ملابس' WHERE [NameEn] = 'Clothing';
                UPDATE [Categories] SET [NameAr] = N'جمال' WHERE [NameEn] = 'Beauty';
                UPDATE [Categories] SET [NameAr] = N'مجوهرات' WHERE [NameEn] = 'Jewelry';
                UPDATE [Categories] SET [NameAr] = N'أثاث' WHERE [NameEn] = 'Furniture';
                UPDATE [Categories] SET [NameAr] = N'لوحة تحكم' WHERE [NameEn] = 'Dashboard';
                UPDATE [Categories] SET [NameAr] = N'ويب 3' WHERE [NameEn] = 'Web3';
                UPDATE [Categories] SET [NameAr] = N'رياضة' WHERE [NameEn] = 'Sports';
                UPDATE [Categories] SET [NameAr] = N'ملابس 2' WHERE [NameEn] = 'Clothing 2';
                UPDATE [Categories] SET [NameAr] = N'إكسسوارات تقنية' WHERE [NameEn] = 'Tech Accessories';
                UPDATE [Categories] SET [NameAr] = N'باك إند' WHERE [NameEn] = 'Backend';
                UPDATE [Categories] SET [NameAr] = N'طعام' WHERE [NameEn] = 'Food';
                UPDATE [Categories] SET [NameAr] = N'قهوة' WHERE [NameEn] = 'Coffe';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [Categories] SET [NameAr] = '' WHERE [NameEn] IN ('Clothing', 'Beauty', 'Jewelry', 'Furniture', 'Dashboard', 'Web3', 'Sports', 'Clothing 2', 'Tech Accessories', 'Backend', 'Food', 'Coffe');
            ");
        }
    }
}