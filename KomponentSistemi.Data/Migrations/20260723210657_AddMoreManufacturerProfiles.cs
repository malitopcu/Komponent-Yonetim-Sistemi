using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreManufacturerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 9,
                column: "PatternRegex",
                value: "^ERJ-?[A-Z0-9]{2,4}(?<tol>[DFGJ])(?<value>[0-9R]{3,4})[A-Z]?$");

            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "Manufacturer", "Name", "PackageMapJson", "PatternRegex", "PowerMapJson", "TcrMapJson", "ToleranceMapJson", "ValueEncoding" },
                values: new object[,]
                {
                    { 11, "Vishay", "Vishay Dale CRCW", "{\"0201\":\"0201\",\"0402\":\"0402\",\"0603\":\"0603\",\"0805\":\"0805\",\"1206\":\"1206\",\"1210\":\"1210\",\"2010\":\"2010\",\"2512\":\"2512\"}", "^CRCW(?<size>\\d{4})(?<value>[0-9RKM]{4})(?<tol>[FJDZ])(?<tcr>[KNH])[A-Z0-9]*$", "{}", "{\"K\":100,\"N\":200,\"H\":50}", "{\"F\":1.0,\"D\":0.5,\"J\":5.0}", "rkm" },
                    { 12, "Bourns", "Bourns CR", "{\"0402\":\"0402\",\"0603\":\"0603\",\"0805\":\"0805\",\"1206\":\"1206\",\"1210\":\"1210\",\"2010\":\"2010\",\"2512\":\"2512\"}", "^CR(?<size>\\d{4})-(?<tol>[FGJ])[A-Z]-(?<value>[0-9R]{3,4})E[A-Z]*$", "{}", "{}", "{\"F\":1.0,\"G\":2.0,\"J\":5.0}", "sig-zeros-R" },
                    { 13, "ROHM", "Rohm MCR", "{}", "^MCR(?<size>\\d{2,3})[A-Z]{3}(?<tol>[FJD])X?(?<value>[0-9R]{3,4})[A-Z]?$", "{}", "{}", "{\"F\":1.0,\"J\":5.0,\"D\":0.5}", "sig-zeros-R" },
                    { 14, "Walsin", "Walsin WR", "{\"10\":\"1210\",\"12\":\"1206\",\"08\":\"0805\",\"06\":\"0603\",\"04\":\"0402\"}", "^WR(?<size>10|12|08|06|04)[XW](?<value>[0-9R]{3,4})(?<tol>[FJ])[TQGHBDA]L?$", "{}", "{}", "{\"F\":1.0,\"J\":5.0}", "sig-zeros-R" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 9,
                column: "PatternRegex",
                value: "^ERJ-?[A-Z0-9]{2,4}(?<tol>[DFGJ])(?<value>[0-9R]{4})[A-Z]?$");
        }
    }
}
