using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegulatorOscillatorConnectorProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "ComponentTypeId", "DielectricMapJson", "Manufacturer", "Name", "PackageMapJson", "PatternRegex", "PowerMapJson", "TcrMapJson", "ToleranceMapJson", "ValueEncoding", "VoltageMapJson" },
                values: new object[,]
                {
                    { 19, 6, "{}", "", "Regülatör 78xx", "{}", "^(?:LM|L|MC|UA|KA|NJM|TS|ST|HT|AZ)?78(?<value>05|06|08|09|10|12|15|18|24)[A-Z0-9/\\-]*$", "{}", "{}", "{}", "literal", "{}" },
                    { 20, 6, "{}", "", "Regülatör AMS1117/LM1117", "{}", "^(?:AMS|LM|LD)1117[A-Z]*-(?<value>\\d(?:\\.\\d+)?)[A-Z0-9/]*$", "{}", "{}", "{}", "literal", "{}" },
                    { 21, 5, "{}", "", "Osilatör (literal frekans)", "{}", "^.*?(?<value>\\d+(?:\\.\\d+)?[MK]HZ).*$", "{}", "{}", "{}", "literal", "{}" },
                    { 22, 7, "{}", "JST", "JST konnektör (housing)", "{}", "^(?:XHP|PHR|ZHR|EHR|SMR)-(?<value>\\d+)[A-Z0-9\\-]*$", "{}", "{}", "{}", "literal", "{}" },
                    { 23, 7, "{}", "JST", "JST konnektör (header)", "{}", "^[BS](?<value>\\d+)B-(?:XH|PH|ZH|EH)[A-Z0-9\\-]*$", "{}", "{}", "{}", "literal", "{}" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "MpnProfiles",
                keyColumn: "Id",
                keyValue: 23);
        }
    }
}
