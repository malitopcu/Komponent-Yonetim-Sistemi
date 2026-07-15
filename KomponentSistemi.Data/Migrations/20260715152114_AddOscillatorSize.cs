using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOscillatorSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ParameterDefinitions",
                columns: new[] { "Id", "ComponentTypeId", "DataType", "DisplayName", "HotColumn", "IsSearchable", "Key", "Unit" },
                values: new object[] { 505, 5, "text", "Boyut", null, false, "size", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ParameterDefinitions",
                keyColumn: "Id",
                keyValue: 505);
        }
    }
}
