using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

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
                    CapacitanceF = table.Column<double>(type: "REAL", nullable: true),
                    VoltageV = table.Column<double>(type: "REAL", nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_Components_CapacitanceF",
                table: "Components",
                column: "CapacitanceF");

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
                name: "IX_Components_VoltageV",
                table: "Components",
                column: "VoltageV");

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
