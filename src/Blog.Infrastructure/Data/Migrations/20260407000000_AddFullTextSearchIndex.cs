using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Creates the SQL Server Full-Text Search catalog and index on the Articles table.
    /// Covers Title, Abstract, and Body columns with CHANGE_TRACKING AUTO so that the
    /// index is kept up to date automatically as articles are inserted or modified.
    ///
    /// Requires Azure SQL Database Standard (S1+) or Premium tier — Full-Text Search
    /// is not available on the Basic tier.
    /// </summary>
    public partial class AddFullTextSearchIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'BlogSearchCatalog'
                )
                BEGIN
                    CREATE FULLTEXT CATALOG BlogSearchCatalog AS DEFAULT;
                END
            ", suppressTransaction: true);

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Articles')
                )
                BEGIN
                    CREATE FULLTEXT INDEX ON Articles(Title, Abstract, Body)
                        KEY INDEX PK_Articles
                        ON BlogSearchCatalog
                        WITH CHANGE_TRACKING AUTO;
                END
            ", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Articles')
                )
                BEGIN
                    DROP FULLTEXT INDEX ON Articles;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'BlogSearchCatalog'
                )
                BEGIN
                    DROP FULLTEXT CATALOG BlogSearchCatalog;
                END
            ");
        }
    }
}
