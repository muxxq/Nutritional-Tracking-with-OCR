using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FoodCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Not a rename: the old column holds sodium in mg, the new one salt in g (EU labels declare salt).
            migrationBuilder.AddColumn<double>(
                name: "SaltGrams",
                table: "FoodItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"FoodItems\" SET \"SaltGrams\" = \"Sodium\" * 2.5 / 1000 WHERE \"Sodium\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "Sodium",
                table: "FoodItems");

            migrationBuilder.AlterColumn<double>(
                name: "ServingSizeGrams",
                table: "FoodItems",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "FoodItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "FoodItems",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            // Backfill existing rows the same way TextNormalizer.ForSearch does for Romanian diacritics.
            migrationBuilder.Sql(
                "UPDATE \"FoodItems\" SET \"SearchText\" = " +
                "trim(regexp_replace(translate(lower(\"Name\" || ' ' || coalesce(\"Brand\", '')), " +
                "'ăâîșşțţ', 'aaissstt'), '\\s+', ' ', 'g'));");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_SearchText",
                table: "FoodItems",
                column: "SearchText");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodItems_SearchText",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "FoodItems");

            migrationBuilder.Sql("UPDATE \"FoodItems\" SET \"ServingSizeGrams\" = 100 WHERE \"ServingSizeGrams\" IS NULL;");

            migrationBuilder.AlterColumn<double>(
                name: "ServingSizeGrams",
                table: "FoodItems",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Sodium",
                table: "FoodItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"FoodItems\" SET \"Sodium\" = \"SaltGrams\" * 1000 / 2.5 WHERE \"SaltGrams\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "SaltGrams",
                table: "FoodItems");
        }
    }
}
