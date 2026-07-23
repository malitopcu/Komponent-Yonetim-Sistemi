using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMpnProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "Id", "Name", "PrefixesJson", "SizeLen", "TcrMapJson", "ToleranceMapJson", "ValueEncoding", "ValueLen" },
                values: new object[] { 1, "Vishay Sfernice RCM", "[\"RCMS\",\"RCMT\",\"RCMA\"]", 2, "{\"H\":50,\"E\":25,\"D\":15}", "{\"F\":1.0,\"B\":0.1,\"A\":0.2,\"D\":0.5}", "sig4-zeros1-R", 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MpnProfiles");
        }
    }
}
