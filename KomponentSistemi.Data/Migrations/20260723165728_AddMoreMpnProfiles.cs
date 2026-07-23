using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreMpnProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "PatternRegex" },
                values: new object[] { "Vishay Sfernice RCMS", "^RCMS\\d{2}(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])" });

            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "Name", "PatternRegex", "PowerMapJson", "TcrMapJson", "ToleranceMapJson", "ValueEncoding" },
                values: new object[,]
                {
                    { 5, "Vishay Sfernice RCMT", "^RCMT(?<power>\\d{2})(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])", "{\"01\":0.063,\"02\":0.125,\"05\":0.25,\"08\":0.5,\"10\":1.0,\"20\":2.0,\"40\":4.0}", "{\"H\":50,\"E\":25,\"D\":15}", "{\"F\":1.0,\"B\":0.1,\"A\":0.2,\"D\":0.5}", "sig-zeros-R" },
                    { 6, "Vishay Sfernice RCMA", "^RCMA(?<power>\\d{2})(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])", "{\"02\":0.125,\"05\":0.25,\"08\":0.5,\"10\":0.75,\"20\":1.0,\"40\":2.0}", "{\"H\":50,\"E\":25,\"D\":15}", "{\"F\":1.0,\"B\":0.1,\"A\":0.2,\"D\":0.5}", "sig-zeros-R" },
                    { 7, "Yageo RC", "^RC(?<size>\\d{4})(?<tol>[BDFJ])[RKS]-(?:07|10|13|7W|7D|7N|3W)(?<value>\\d+[RKM]\\d*|R\\d+)[A-Z]?$", "{}", "{}", "{\"B\":0.1,\"D\":0.5,\"F\":1.0,\"J\":5.0}", "rkm" },
                    { 8, "KOA RK73H", "^RK73H(?<size>W3A2|W2H|W3A|1[FHEJ]|2[ABEH]|3A)A?[TGL](?:TX|TBL|TCM|TPL|TP|TD|TE)(?<value>[0-9R]{4})(?<tol>[DF])$", "{}", "{}", "{\"D\":0.5,\"F\":1.0}", "sig-zeros-R" },
                    { 9, "Panasonic ERJ", "^ERJ-?[A-Z0-9]{2,4}(?<tol>[DFGJ])(?<value>[0-9R]{4})[A-Z]?$", "{}", "{}", "{\"D\":0.5,\"F\":1.0,\"G\":2.0,\"J\":5.0}", "sig-zeros-R" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.UpdateData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "PatternRegex" },
                values: new object[] { "Vishay Sfernice RCM", "^(?:RCMS|RCMT|RCMA)\\d{2}(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])" });
        }
    }
}
