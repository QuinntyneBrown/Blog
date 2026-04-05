using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Corrective migration that aligns the DigitalAssets table with the EF Core entity model
    /// and the design specification (Feature 04, Section 4.1):
    ///
    /// 1. Alters Width and Height from INT NULL to INT NOT NULL with a default of 0 so that
    ///    any legacy NULL rows are coerced to 0 before the NOT NULL constraint is applied.
    ///    The entity (DigitalAsset.cs) was changed to non-nullable int in a prior conformance
    ///    fix but the migration was never updated.
    ///
    /// 2. Creates the IX_DigitalAssets_StoredFileName unique index specified in
    ///    DigitalAssetConfiguration.cs and required by the design's "StoredFileName: Required,
    ///    unique, max 256 chars" constraint. The index was added to the EF Core configuration
    ///    but the initial migration pre-dates that fix.
    /// </summary>
    public partial class CorrectDigitalAssetSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Set any existing NULL values to 0 before applying NOT NULL constraint.
            // On a fresh development database there will be no rows; this guards against
            // existing data on any deployed instance.
            migrationBuilder.Sql(
                "UPDATE [DigitalAssets] SET [Width] = 0 WHERE [Width] IS NULL");
            migrationBuilder.Sql(
                "UPDATE [DigitalAssets] SET [Height] = 0 WHERE [Height] IS NULL");

            // Step 2: Alter Width column — INT NULL → INT NOT NULL DEFAULT 0
            migrationBuilder.AlterColumn<int>(
                name: "Width",
                table: "DigitalAssets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "int",
                oldNullable: true);

            // Step 3: Alter Height column — INT NULL → INT NOT NULL DEFAULT 0
            migrationBuilder.AlterColumn<int>(
                name: "Height",
                table: "DigitalAssets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "int",
                oldNullable: true);

            // Step 4: Create the unique index on StoredFileName that was specified in
            // DigitalAssetConfiguration but was absent from the initial migration.
            migrationBuilder.CreateIndex(
                name: "IX_DigitalAssets_StoredFileName",
                table: "DigitalAssets",
                column: "StoredFileName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index first so the column can be made nullable again.
            migrationBuilder.DropIndex(
                name: "IX_DigitalAssets_StoredFileName",
                table: "DigitalAssets");

            // Revert Width back to INT NULL.
            migrationBuilder.AlterColumn<int?>(
                name: "Width",
                table: "DigitalAssets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false,
                oldDefaultValue: 0);

            // Revert Height back to INT NULL.
            migrationBuilder.AlterColumn<int?>(
                name: "Height",
                table: "DigitalAssets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false,
                oldDefaultValue: 0);
        }
    }
}
