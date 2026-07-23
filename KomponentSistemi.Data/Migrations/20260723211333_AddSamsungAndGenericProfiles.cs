using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSamsungAndGenericProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "Manufacturer", "Name", "PackageMapJson", "PatternRegex", "PowerMapJson", "TcrMapJson", "ToleranceMapJson", "ValueEncoding" },
                values: new object[,]
                {
                    { 15, "Samsung Electro-Mechanics", "Samsung RC", "{\"0402\":\"01005\",\"0603\":\"0201\",\"1005\":\"0402\",\"1608\":\"0603\",\"2012\":\"0805\",\"3216\":\"1206\",\"3225\":\"1210\",\"5025\":\"2010\",\"6432\":\"2512\"}", "^RC(?<size>\\d{4})(?<tol>[DFGJ])(?<value>[0-9R]{3,4})(?:CS|ES|AS)$", "{}", "{}", "{\"D\":0.5,\"F\":1.0,\"G\":2.0,\"J\":5.0}", "sig-zeros-R" },
                    { 16, "", "Standart boy-W kodu", "{\"0402\":\"0402\",\"0603\":\"0603\",\"0805\":\"0805\",\"1206\":\"1206\",\"1210\":\"1210\",\"2010\":\"2010\",\"2512\":\"2512\"}", "^(?<size>0201|0402|0603|0805|1206|1210|2010|2512)W[0-9A-Z](?<tol>[FGJD])(?<value>[0-9R]{3,4})T[0-9A-Z]*$", "{}", "{}", "{\"F\":1.0,\"G\":2.0,\"J\":5.0,\"D\":0.5}", "sig-zeros-R" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}
