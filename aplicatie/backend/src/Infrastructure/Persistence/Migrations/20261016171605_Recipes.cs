using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Recipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Recipes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Recipes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrepTimeMinutes",
                table: "Recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "Recipes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityGrams",
                table: "DiaryEntries",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<Guid>(
                name: "FoodItemId",
                table: "DiaryEntries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "RecipeId",
                table: "DiaryEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_SearchText",
                table: "Recipes",
                column: "SearchText");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId_FoodItemId",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "FoodItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_RecipeId",
                table: "DiaryEntries",
                column: "RecipeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DiaryEntries_FoodXorRecipe",
                table: "DiaryEntries",
                sql: "(\"FoodItemId\" IS NULL) <> (\"RecipeId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DiaryEntries_QuantityByKind",
                table: "DiaryEntries",
                sql: "(\"FoodItemId\" IS NOT NULL AND \"QuantityGrams\" IS NOT NULL) OR (\"RecipeId\" IS NOT NULL AND \"QuantityGrams\" IS NULL AND \"Servings\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_DiaryEntries_Recipes_RecipeId",
                table: "DiaryEntries",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiaryEntries_Recipes_RecipeId",
                table: "DiaryEntries");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_SearchText",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_RecipeId_FoodItemId",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_DiaryEntries_RecipeId",
                table: "DiaryEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DiaryEntries_FoodXorRecipe",
                table: "DiaryEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DiaryEntries_QuantityByKind",
                table: "DiaryEntries");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "PrepTimeMinutes",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "DiaryEntries");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Recipes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "QuantityGrams",
                table: "DiaryEntries",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FoodItemId",
                table: "DiaryEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");
        }
    }
}
