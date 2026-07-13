using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KomponentSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_ComponentId",
                table: "Offers");

            // İkiz teklifleri temizle: her (ComponentId, Source) için EN YENİ (en büyük Id) kalır.
            // Benzersiz indeksten önce şart — aynı dosyanın 2 kez yüklenmesinden kalan kayıtlar.
            migrationBuilder.Sql(
                "DELETE FROM Offers WHERE Id NOT IN (SELECT MAX(Id) FROM Offers GROUP BY ComponentId, Source);");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ComponentId_Source",
                table: "Offers",
                columns: new[] { "ComponentId", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_ComponentId_Source",
                table: "Offers");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ComponentId",
                table: "Offers",
                column: "ComponentId");
        }
    }
}
