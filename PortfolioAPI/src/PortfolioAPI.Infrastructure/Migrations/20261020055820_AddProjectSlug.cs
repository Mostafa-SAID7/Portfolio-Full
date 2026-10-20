using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Backfill the existing rows: the slug is derived from the title
            // once and then never changes again, so the public links of the
            // projects that already exist stay stable from here on.
            migrationBuilder.Sql(@"
                -- 1) title -> slug (lowercase, separators become dashes)
                UPDATE [Projects]
                SET [Slug] = LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                        LTRIM(RTRIM([Title])),
                        ' ', '-'), '_', '-'), '.', '-'), ',', '-'), '/', '-'), '\', '-'), '&', '-'), '  ', '-'));

                -- 2) drop every character that is not allowed in a slug
                --    (only the characters that actually occur in the titles,
                --     so the loop is short and bounded)
                DECLARE @ch NVARCHAR(2);
                DECLARE @bad TABLE (c NVARCHAR(2) NOT NULL PRIMARY KEY);

                INSERT INTO @bad(c)
                SELECT DISTINCT SUBSTRING(s.[Slug], n.n, 1)
                FROM [Projects] s
                CROSS JOIN (
                    SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
                    FROM sys.all_objects) n
                WHERE n.n <= LEN(s.[Slug])
                  AND SUBSTRING(s.[Slug], n.n, 1) NOT IN (
                        'a','b','c','d','e','f','g','h','i','j','k','l','m',
                        'n','o','p','q','r','s','t','u','v','w','x','y','z',
                        '0','1','2','3','4','5','6','7','8','9','-');

                WHILE EXISTS (SELECT 1 FROM @bad)
                BEGIN
                    SELECT TOP (1) @ch = c FROM @bad;
                    UPDATE [Projects] SET [Slug] = REPLACE([Slug], @ch, '');
                    DELETE FROM @bad WHERE c = @ch;
                END;

                -- 3) collapse repeated dashes and trim them
                UPDATE [Projects]
                SET [Slug] = LTRIM(RTRIM(REPLACE([Slug], '--', '-'), '-'));
                WHILE EXISTS (SELECT 1 FROM [Projects] WHERE [Slug] LIKE '%--%')
                BEGIN
                    UPDATE [Projects] SET [Slug] = REPLACE([Slug], '--', '-');
                END;
                UPDATE [Projects]
                SET [Slug] = LTRIM(RTRIM([Slug], '-'), '-');

                -- 4) a title without any usable character still needs a slug
                UPDATE [Projects] SET [Slug] = 'project' WHERE [Slug] = '';

                -- 5) make the slugs unique (the id disambiguates collisions)
                UPDATE p
                SET p.[Slug] = d.[Slug] + '-' + CAST(p.[Id] AS NVARCHAR(10))
                FROM [Projects] p
                INNER JOIN (
                    SELECT [Slug] FROM [Projects] GROUP BY [Slug] HAVING COUNT(*) > 1) d
                    ON d.[Slug] = p.[Slug];
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Slug",
                table: "Projects",
                column: "Slug",
                unique: true,
                filter: "[Slug] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_Slug",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Projects");
        }
    }
}
