using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProfileTargetOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TargetCarbsGrams",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TargetFatsGrams",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TargetProteinsGrams",
                table: "Users");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "BirthDate",
                table: "Users",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaloriesOverride",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CarbsGramsOverride",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FatsGramsOverride",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProteinsGramsOverride",
                table: "Users",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaloriesOverride",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CarbsGramsOverride",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FatsGramsOverride",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProteinsGramsOverride",
                table: "Users");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetCalories",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TargetCarbsGrams",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TargetFatsGrams",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TargetProteinsGrams",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
