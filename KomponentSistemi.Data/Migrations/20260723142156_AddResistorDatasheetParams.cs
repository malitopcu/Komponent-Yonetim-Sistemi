using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResistorDatasheetParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ParameterDefinitions",
                columns: new[] { "Id", "ComponentTypeId", "DataType", "DisplayName", "HotColumn", "IsSearchable", "Key", "Unit" },
                values: new object[,]
                {
                    { 208, 2, "numeric", "TCR", null, false, "tcr", "ppm/°C" },
                    { 209, 2, "text", "Kompozisyon", null, true, "composition", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 208);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 209);
        }
    }
}
