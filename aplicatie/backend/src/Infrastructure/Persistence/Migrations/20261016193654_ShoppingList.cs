using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShoppingList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_UserId",
                table: "ShoppingListItems");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityGrams",
                table: "ShoppingListItems",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "ShoppingListItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FoodItemId",
                table: "ShoppingListItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuantityText",
                table: "ShoppingListItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Existing foods start as Other (6); the global seed gets its aisles (FruitsVegetables 0, MeatFish 1,
            // Dairy 2, Grains 3, Pantry 4). New databases get the same values from ApplicationDbContextSeed.
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "FoodItems",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.Sql("""
                UPDATE "FoodItems" SET "Category" = 1 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Piept de pui fără piele (crud)';
                UPDATE "FoodItems" SET "Category" = 3 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Fulgi de ovăz integrali';
                UPDATE "FoodItems" SET "Category" = 3 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Orez alb basmati (crud)';
                UPDATE "FoodItems" SET "Category" = 2 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Ou de găină întreg';
                UPDATE "FoodItems" SET "Category" = 0 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Banană proaspătă';
                UPDATE "FoodItems" SET "Category" = 1 WHERE "CreatedByUserId" IS NULL AND "Name" = 'File de somon proaspăt';
                UPDATE "FoodItems" SET "Category" = 0 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Broccoli crud';
                UPDATE "FoodItems" SET "Category" = 4 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Ulei de măsline extravirgin';
                UPDATE "FoodItems" SET "Category" = 2 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Iaurt grecesc 2% grăsime';
                UPDATE "FoodItems" SET "Category" = 4 WHERE "CreatedByUserId" IS NULL AND "Name" = 'Migdale crude';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_FoodItemId",
                table: "ShoppingListItems",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_UserId_Category",
                table: "ShoppingListItems",
                columns: new[] { "UserId", "Category" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShoppingListItems_GeneratedOrManual",
                table: "ShoppingListItems",
                sql: "(\"FoodItemId\" IS NOT NULL AND \"QuantityGrams\" IS NOT NULL AND \"QuantityText\" IS NULL) OR (\"FoodItemId\" IS NULL AND \"QuantityGrams\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_FoodItems_FoodItemId",
                table: "ShoppingListItems",
                column: "FoodItemId",
                principalTable: "FoodItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_FoodItems_FoodItemId",
                table: "ShoppingListItems");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_FoodItemId",
                table: "ShoppingListItems");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_UserId_Category",
                table: "ShoppingListItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShoppingListItems_GeneratedOrManual",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "FoodItemId",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "QuantityText",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "FoodItems");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityGrams",
                table: "ShoppingListItems",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_UserId",
                table: "ShoppingListItems",
                column: "UserId");
        }
    }
}
