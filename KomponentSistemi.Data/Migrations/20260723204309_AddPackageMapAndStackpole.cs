using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageMapAndStackpole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackageMapJson",
                table: "MpnProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "PackageMapJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 2,
                column: "PackageMapJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 3,
                column: "PackageMapJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 4,
                column: "PackageMapJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 5,
                column: "PackageMapJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 6,
                column: "PackageMapJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 7,
                column: "PackageMapJson",
                value: "{\"0075\":\"0075\",\"0100\":\"0100\",\"0201\":\"0201\",\"0402\":\"0402\",\"0603\":\"0603\",\"0805\":\"0805\",\"1206\":\"1206\",\"1210\":\"1210\",\"1218\":\"1218\",\"2010\":\"2010\",\"2512\":\"2512\"}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 8,
                column: "PackageMapJson",
                value: "{\"1F\":\"01005\",\"1H\":\"0201\",\"1E\":\"0402\",\"1J\":\"0603\",\"2A\":\"0805\",\"2B\":\"1206\",\"2E\":\"1210\",\"2H\":\"2010\",\"W2H\":\"2010\",\"3A\":\"2512\",\"W3A\":\"2512\",\"W3A2\":\"2512\"}");

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 9,
                column: "PackageMapJson",
                value: "{}");

            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "Manufacturer", "Name", "PackageMapJson", "PatternRegex", "PowerMapJson", "TcrMapJson", "ToleranceMapJson", "ValueEncoding" },
                values: new object[] { 10, "Stackpole Electronics", "Stackpole SP", "{}", "^SP3A(?<tol>[J])T(?<value>[0-9R]{4})$", "{}", "{}", "{\"J\":5.0}", "rkm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "PackageMapJson",
                table: "MpnProfiles");
        }
    }
}
