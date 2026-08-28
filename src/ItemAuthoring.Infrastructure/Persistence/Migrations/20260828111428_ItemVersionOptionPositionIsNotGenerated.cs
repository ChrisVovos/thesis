using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemAuthoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ItemVersionOptionPositionIsNotGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot drop the IDENTITY property in place, so the column is rebuilt. The
            // existing values are identity numbers rather than per-version ordinals, and are
            // renumbered from zero within each version on the way across.
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] ADD [Ordinal] int NOT NULL "
                + "CONSTRAINT [DF_ItemVersionOptions_Ordinal] DEFAULT 0;");

            migrationBuilder.Sql(
                """
                UPDATE [o]
                SET [o].[Ordinal] = [n].[Ordinal]
                FROM [authoring].[ItemVersionOptions] AS [o]
                INNER JOIN (
                    SELECT
                        [ItemVersionId],
                        [Position],
                        ROW_NUMBER() OVER (PARTITION BY [ItemVersionId] ORDER BY [Position]) - 1 AS [Ordinal]
                    FROM [authoring].[ItemVersionOptions]
                ) AS [n] ON [n].[ItemVersionId] = [o].[ItemVersionId] AND [n].[Position] = [o].[Position];
                """);

            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] DROP CONSTRAINT [PK_ItemVersionOptions];");
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] DROP COLUMN [Position];");
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] "
                + "DROP CONSTRAINT [DF_ItemVersionOptions_Ordinal];");
            migrationBuilder.Sql(
                "EXEC sp_rename N'[authoring].[ItemVersionOptions].[Ordinal]', N'Position', N'COLUMN';");
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] ADD CONSTRAINT [PK_ItemVersionOptions] "
                + "PRIMARY KEY CLUSTERED ([ItemVersionId], [Position]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] DROP CONSTRAINT [PK_ItemVersionOptions];");
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] DROP COLUMN [Position];");
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] ADD [Position] int NOT NULL IDENTITY(1, 1);");
            migrationBuilder.Sql(
                "ALTER TABLE [authoring].[ItemVersionOptions] ADD CONSTRAINT [PK_ItemVersionOptions] "
                + "PRIMARY KEY CLUSTERED ([ItemVersionId], [Position]);");
        }
    }
}
