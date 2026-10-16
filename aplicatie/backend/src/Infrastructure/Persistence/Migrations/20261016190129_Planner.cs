using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Planner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMeals_FoodItems_FoodItemId",
                table: "PlannedMeals");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMeals_Recipes_RecipeId",
                table: "PlannedMeals");

            migrationBuilder.DropColumn(
                name: "IsTransferredToDiary",
                table: "PlannedMeals");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityGrams",
                table: "PlannedMeals",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<Guid>(
                name: "DiaryEntryId",
                table: "PlannedMeals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Servings",
                table: "PlannedMeals",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMeals_DiaryEntryId",
                table: "PlannedMeals",
                column: "DiaryEntryId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlannedMeals_FoodXorRecipe",
                table: "PlannedMeals",
                sql: "(\"FoodItemId\" IS NULL) <> (\"RecipeId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlannedMeals_QuantityByKind",
                table: "PlannedMeals",
                sql: "(\"FoodItemId\" IS NOT NULL AND \"QuantityGrams\" IS NOT NULL) OR (\"RecipeId\" IS NOT NULL AND \"QuantityGrams\" IS NULL AND \"Servings\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMeals_DiaryEntries_DiaryEntryId",
                table: "PlannedMeals",
                column: "DiaryEntryId",
                principalTable: "DiaryEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMeals_FoodItems_FoodItemId",
                table: "PlannedMeals",
                column: "FoodItemId",
                principalTable: "FoodItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMeals_Recipes_RecipeId",
                table: "PlannedMeals",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMeals_DiaryEntries_DiaryEntryId",
                table: "PlannedMeals");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMeals_FoodItems_FoodItemId",
                table: "PlannedMeals");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMeals_Recipes_RecipeId",
                table: "PlannedMeals");

            migrationBuilder.DropIndex(
                name: "IX_PlannedMeals_DiaryEntryId",
                table: "PlannedMeals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlannedMeals_FoodXorRecipe",
                table: "PlannedMeals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlannedMeals_QuantityByKind",
                table: "PlannedMeals");

            migrationBuilder.DropColumn(
                name: "DiaryEntryId",
                table: "PlannedMeals");

            migrationBuilder.DropColumn(
                name: "Servings",
                table: "PlannedMeals");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityGrams",
                table: "PlannedMeals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTransferredToDiary",
                table: "PlannedMeals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMeals_FoodItems_FoodItemId",
                table: "PlannedMeals",
                column: "FoodItemId",
                principalTable: "FoodItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMeals_Recipes_RecipeId",
                table: "PlannedMeals",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
