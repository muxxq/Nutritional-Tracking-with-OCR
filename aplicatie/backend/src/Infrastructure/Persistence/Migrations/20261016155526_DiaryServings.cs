using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DiaryServings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Servings",
                table: "DiaryEntries",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Servings",
                table: "DiaryEntries");
        }
    }
}
