using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecipeIngredientPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "RecipeIngredients");
        }
    }
}
