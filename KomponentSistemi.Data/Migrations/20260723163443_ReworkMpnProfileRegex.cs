using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReworkMpnProfileRegex : Migration
    {
        // EF'in ürettiği sütun-düşürme planı SQLite'ta "tabloyu sonda yeniden inşa et"
        // anlamına gelir; tohum INSERT'leri inşadan önce koşup NOT NULL'a takılıyordu.
        // Tablo yalnızca tohum verisi taşıdığı için düşürüp yeni şemayla kurmak
        // hem güvenli hem belirlenimci.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MpnProfiles");

            migrationBuilder.CreateTable(
                name: "MpnProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PatternRegex = table.Column<string>(type: "TEXT", nullable: false),
                    ValueEncoding = table.Column<string>(type: "TEXT", nullable: false),
                    ToleranceMapJson = table.Column<string>(type: "TEXT", nullable: false),
                    TcrMapJson = table.Column<string>(type: "TEXT", nullable: false),
                    PowerMapJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MpnProfiles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "Name", "PatternRegex", "ValueEncoding", "ToleranceMapJson", "TcrMapJson", "PowerMapJson" },
                values: new object[,]
                {
                    { 1, "Vishay Sfernice RCM", "^(?:RCMS|RCMT|RCMA)\\d{2}(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])", "sig-zeros-R", "{\"F\":1.0,\"B\":0.1,\"A\":0.2,\"D\":0.5}", "{\"H\":50,\"E\":25,\"D\":15}", "{}" },
                    { 2, "Vishay Sfernice RLP", "^RLP\\d{2}(?<value>[0-9R]{5})(?<tol>[A-Z])", "sig-zeros-R", "{\"F\":1.0,\"J\":5.0,\"D\":0.5}", "{}", "{}" },
                    { 3, "Ohmite 40 Serisi", "^4(?<power>[123570])N?(?<tol>[FJ])(?<value>\\d+[RKM]\\d*|R\\d+|\\d+)(?:E)?(?:-T)?$", "rkm", "{\"F\":1.0,\"J\":5.0}", "{}", "{\"1\":1.0,\"2\":2.0,\"3\":3.0,\"5\":5.0,\"7\":7.0,\"0\":10.0}" },
                    { 4, "Ohmite HSX", "^HSX-2[WZ](?<value>\\d{4})(?<tol>[A-Z])E$", "sig-zeros-R", "{\"F\":1.0,\"J\":5.0}", "{}", "{}" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MpnProfiles");

            migrationBuilder.CreateTable(
                name: "MpnProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PrefixesJson = table.Column<string>(type: "TEXT", nullable: false),
                    SizeLen = table.Column<int>(type: "INTEGER", nullable: false),
                    ValueLen = table.Column<int>(type: "INTEGER", nullable: false),
                    ValueEncoding = table.Column<string>(type: "TEXT", nullable: false),
                    ToleranceMapJson = table.Column<string>(type: "TEXT", nullable: false),
                    TcrMapJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MpnProfiles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MpnProfiles",
                columns: new[] { "Id", "Name", "PrefixesJson", "SizeLen", "ValueLen", "ValueEncoding", "ToleranceMapJson", "TcrMapJson" },
                values: new object[] { 1, "Vishay Sfernice RCM", "[\"RCMS\",\"RCMT\",\"RCMA\"]", 2, 5, "sig4-zeros1-R", "{\"F\":1.0,\"B\":0.1,\"A\":0.2,\"D\":0.5}", "{\"H\":50,\"E\":25,\"D\":15}" });
        }
    }
}
