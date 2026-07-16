using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectorAndPots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ComponentTypes",
                columns: new[] { "Id", "Name" },
                values: new object[] { 7, "Konnektör" });

            migrationBuilder.InsertData(
                table: "ParameterDefinitions",
                columns: new[] { "Id", "ComponentTypeId", "DataType", "DisplayName", "HotColumn", "IsSearchable", "Key", "Unit" },
                values: new object[,]
                {
                    { 205, 2, "text", "Alt Tür", null, true, "subtype", null },
                    { 206, 2, "text", "Taper (Eğri)", null, false, "taper", null },
                    { 207, 2, "text", "Tur Sayısı", null, false, "turns", null },
                    { 701, 7, "numeric", "Pozisyon", "primary", true, "positions", null },
                    { 702, 7, "numeric", "Akım", "secondary", true, "current", "A" },
                    { 703, 7, "numeric", "Gerilim", null, false, "voltage", "V" },
                    { 704, 7, "text", "Aralık (Pitch)", null, false, "pitch", null },
                    { 705, 7, "text", "Montaj", null, false, "mounting", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 701);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 702);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 703);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 704);

            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 705);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
