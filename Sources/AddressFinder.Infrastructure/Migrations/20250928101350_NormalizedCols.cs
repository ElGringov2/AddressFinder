using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressFinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizedCols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Addresses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 28, 12, 13, 49, 846, DateTimeKind.Local).AddTicks(953),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValue: new DateTime(2025, 9, 27, 23, 24, 14, 3, DateTimeKind.Local).AddTicks(8836));

            migrationBuilder.AddColumn<string>(
                name: "CityNorm",
                table: "Addresses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryNorm",
                table: "Addresses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetNorm",
                table: "Addresses",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityNorm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "CountryNorm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "StreetNorm",
                table: "Addresses");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Addresses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 27, 23, 24, 14, 3, DateTimeKind.Local).AddTicks(8836),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValue: new DateTime(2025, 9, 28, 12, 13, 49, 846, DateTimeKind.Local).AddTicks(953));
        }
    }
}
