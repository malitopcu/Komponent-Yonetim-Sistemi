using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacitorProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComponentTypeId",
                table: "MpnProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DielectricMapJson",
                table: "MpnProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoltageMapJson",
                table: "MpnProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "ComponentTypeId", "DielectricMapJson", "VoltageMapJson" },
                values: new object[] { 2, "{}", "{}" });

            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "ComponentTypeId", "DielectricMapJson", "Manufacturer", "Name", "PackageMapJson", "PatternRegex", "PowerMapJson", "TcrMapJson", "ToleranceMapJson", "ValueEncoding", "VoltageMapJson" },
                values: new object[,]
                {
                    { 17, 1, "{}", "TDK", "TDK C", "{\"1005\":\"0402\",\"1608\":\"0603\",\"2012\":\"0805\",\"3216\":\"1206\",\"3225\":\"1210\",\"4532\":\"1812\"}", "^C(?<size>\\d{4})(?<diel>C0G|X7R|X5R|X6S|X7S|NP0|Y5V|X8R)(?<volt>[0-9][A-Z])(?<value>[0-9R]{3})(?<tol>[A-Z])[A-Z0-9]*$", "{}", "{}", "{\"F\":1.0,\"G\":2.0,\"J\":5.0,\"K\":10.0,\"M\":20.0}", "sig-zeros-R", "{}" },
                    { 18, 1, "{\"R7\":\"X7R\",\"R6\":\"X5R\",\"5C\":\"C0G\",\"R1\":\"X8R\"}", "Murata", "Murata GRM", "{\"03\":\"0201\",\"15\":\"0402\",\"18\":\"0603\",\"21\":\"0805\",\"31\":\"1206\",\"32\":\"1210\",\"43\":\"1812\",\"55\":\"2220\"}", "^GRM(?<size>\\d{2})[0-9A-Z](?<diel>R7|R6|5C|R1)(?<volt>[0-9][A-Z])(?<value>[0-9R]{3})(?<tol>[A-Z])[A-Z0-9]*$", "{}", "{}", "{\"F\":1.0,\"G\":2.0,\"J\":5.0,\"K\":10.0,\"M\":20.0}", "sig-zeros-R", "{}" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DropColumn(
                name: "ComponentTypeId",
                table: "MpnProfiles");

            migrationBuilder.DropColumn(
                name: "DielectricMapJson",
                table: "MpnProfiles");

            migrationBuilder.DropColumn(
                name: "VoltageMapJson",
                table: "MpnProfiles");
        }
    }
}
