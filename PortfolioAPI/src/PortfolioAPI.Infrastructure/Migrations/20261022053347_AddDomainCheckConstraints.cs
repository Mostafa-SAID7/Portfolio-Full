using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Testimonials_Rating",
                table: "Testimonials",
                sql: "[Rating] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Skills_Proficiency",
                table: "Skills",
                sql: "[Proficiency] BETWEEN 0 AND 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Testimonials_Rating",
                table: "Testimonials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Skills_Proficiency",
                table: "Skills");
        }
    }
}
