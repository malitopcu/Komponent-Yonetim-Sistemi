using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComponentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Delimiter = table.Column<string>(type: "TEXT", nullable: false),
                    Encoding = table.Column<string>(type: "TEXT", nullable: false),
                    DecimalSeparator = table.Column<string>(type: "TEXT", nullable: false),
                    MappingsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mpn = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    ComponentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryValueSi = table.Column<double>(type: "REAL", nullable: true),
                    SecondaryValueSi = table.Column<double>(type: "REAL", nullable: true),
                    ParamsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Components_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    DataType = table.Column<string>(type: "TEXT", nullable: false),
                    IsSearchable = table.Column<bool>(type: "INTEGER", nullable: false),
                    HotColumn = table.Column<string>(type: "TEXT", nullable: true),
                    ComponentTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterDefinitions_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    SourcePartNo = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true),
                    PriceUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ComponentTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Kondansatör" },
                    { 2, "Direnç" },
                    { 3, "Diyot" },
                    { 4, "Transistör" },
                    { 5, "Osilatör" },
                    { 6, "Regülatör" }
                });

            migrationBuilder.InsertData(
                table: "ParameterDefinitions",
                columns: new[] { "Id", "ComponentTypeId", "DataType", "DisplayName", "HotColumn", "IsSearchable", "Key", "Unit" },
                values: new object[,]
                {
                    { 101, 1, "numeric", "Kapasitans", "primary", true, "capacitance", "F" },
                    { 102, 1, "numeric", "Anma Gerilimi", "secondary", true, "voltage", "V" },
                    { 103, 1, "text", "Dielektrik", null, true, "dielectric", null },
                    { 104, 1, "text", "Tolerans", null, false, "tolerance", null },
                    { 105, 1, "text", "Paket", null, true, "package", null },
                    { 201, 2, "numeric", "Direnç", "primary", true, "resistance", "Ω" },
                    { 202, 2, "numeric", "Güç", "secondary", true, "power", "W" },
                    { 203, 2, "text", "Tolerans", null, false, "tolerance", null },
                    { 204, 2, "text", "Paket", null, true, "package", null },
                    { 301, 3, "numeric", "Ters Gerilim (Vr)", "primary", true, "reverse_voltage", "V" },
                    { 302, 3, "numeric", "İleri Akım (If)", "secondary", true, "forward_current", "A" },
                    { 303, 3, "numeric", "İleri Gerilim (Vf)", null, false, "forward_voltage", "V" },
                    { 304, 3, "text", "Alt Tür", null, true, "subtype", null },
                    { 305, 3, "text", "Paket", null, true, "package", null },
                    { 401, 4, "numeric", "Gerilim (Vds/Vce)", "primary", true, "voltage", "V" },
                    { 402, 4, "numeric", "Akım", "secondary", true, "current", "A" },
                    { 403, 4, "numeric", "Güç", null, false, "power", "W" },
                    { 404, 4, "text", "Alt Tür", null, true, "subtype", null },
                    { 405, 4, "text", "Paket", null, true, "package", null },
                    { 501, 5, "numeric", "Frekans", "primary", true, "frequency", "Hz" },
                    { 502, 5, "numeric", "Besleme Gerilimi", "secondary", true, "supply_voltage", "V" },
                    { 503, 5, "numeric", "Frekans Toleransı", null, false, "frequency_tolerance", "ppm" },
                    { 504, 5, "text", "Paket", null, true, "package", null },
                    { 601, 6, "numeric", "Çıkış Gerilimi", "primary", true, "output_voltage", "V" },
                    { 602, 6, "numeric", "Çıkış Akımı", "secondary", true, "output_current", "A" },
                    { 603, 6, "text", "Alt Tür", null, true, "subtype", null },
                    { 604, 6, "text", "Paket", null, true, "package", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_ComponentTypeId",
                table: "Components",
                column: "ComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_Mpn_Manufacturer",
                table: "Components",
                columns: new[] { "Mpn", "Manufacturer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Components_PrimaryValueSi",
                table: "Components",
                column: "PrimaryValueSi");

            migrationBuilder.CreateIndex(
                name: "IX_Components_SecondaryValueSi",
                table: "Components",
                column: "SecondaryValueSi");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ComponentId",
                table: "Offers",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterDefinitions_ComponentTypeId",
                table: "ParameterDefinitions",
                column: "ComponentTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportProfiles");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "ParameterDefinitions");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "ComponentTypes");
        }
    }
}
