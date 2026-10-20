using Microsoft.EntityFrameworkCore.Migrations;
using PortfolioAPI.Core.Entities;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoriesArabicData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing categories with their Arabic names using EF Core
            var categories = new[]
            {
                new Category { Id = 1, NameEn = "Clothing", NameAr = "ملابس" },
                new Category { Id = 2, NameEn = "Beauty", NameAr = "جمال" },
                new Category { Id = 3, NameEn = "Jewelry", NameAr = "مجوهرات" },
                new Category { Id = 4, NameEn = "Furniture", NameAr = "أثاث" },
                new Category { Id = 5, NameEn = "Dashboard", NameAr = "لوحة تحكم" },
                new Category { Id = 6, NameEn = "Web3", NameAr = "ويب 3" },
                new Category { Id = 7, NameEn = "Sports", NameAr = "رياضة" },
                new Category { Id = 8, NameEn = "Clothing", NameAr = "ملابس" }, // duplicate Clothing
                new Category { Id = 9, NameEn = "Tech Accessories", NameAr = "إكسسوارات تقنية" },
                new Category { Id = 10, NameEn = "Backend", NameAr = "باك إند" },
                new Category { Id = 11, NameEn = "Other", NameAr = "أخرى" },
            };

            foreach (var category in categories)
            {
                migrationBuilder.Sql($"UPDATE [Categories] SET [NameAr] = N'{category.NameAr}' WHERE [Id] = {category.Id}");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [Categories] SET [NameAr] = '' WHERE [Id] IN (1,2,3,4,5,6,7,8,9,10,11);
            ");
        }
    }
}