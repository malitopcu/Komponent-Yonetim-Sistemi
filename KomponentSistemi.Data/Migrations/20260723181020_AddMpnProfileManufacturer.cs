using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMpnProfileManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "MpnProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Manufacturer",
                value: "Vishay Sfernice");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Manufacturer",
                value: "Vishay Sfernice");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Manufacturer",
                value: "Ohmite");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 4,
                column: "Manufacturer",
                value: "Ohmite");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 5,
                column: "Manufacturer",
                value: "Vishay Sfernice");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 6,
                column: "Manufacturer",
                value: "Vishay Sfernice");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 7,
                column: "Manufacturer",
                value: "Yageo");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 8,
                column: "Manufacturer",
                value: "KOA Speer");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 9,
                column: "Manufacturer",
                value: "Panasonic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "MpnProfiles");
        }
    }
}
