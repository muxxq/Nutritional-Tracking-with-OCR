using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FoodCatalogConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodItems_Users_CreatedByUserId",
                table: "FoodItems");

            migrationBuilder.AlterColumn<string>(
                name: "SearchText",
                table: "FoodItems",
                type: "character varying(401)",
                maxLength: 401,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodItems_Users_CreatedByUserId",
                table: "FoodItems",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodItems_Users_CreatedByUserId",
                table: "FoodItems");

            migrationBuilder.AlterColumn<string>(
                name: "SearchText",
                table: "FoodItems",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(401)",
                oldMaxLength: 401);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodItems_Users_CreatedByUserId",
                table: "FoodItems",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
