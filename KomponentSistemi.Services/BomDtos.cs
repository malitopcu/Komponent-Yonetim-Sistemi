using System.Globalization;

namespace KomponentSistemi.Services;

// BOM ekranındaki tek satır: bir komponent + adet + referans + fiyat gösterimi.
public class BomRowDto
{
    public int BomItemId { get; init; }      // silme/güncelleme için kimlik
    public int ComponentId { get; init; }
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string TypeName { get; init; } = "";
    public int Quantity { get; init; }
    public string References { get; init; } = "";

    // Para birimi başına gösterim: "0.1 EUR  ·  0.11 USD" (yoksa "—")
    public string UnitPriceDisplay { get; init; } = "—";
    public string LinePriceDisplay { get; init; } = "—";

    // Fiyatı olan bir teklif var mı? (yoksa satır "fiyatsız")
    public bool HasPrice { get; init; }
}

// Para birimi başına toplam: EUR ayrı, USD ayrı. Asla toplanmaz.
public class BomTotalDto
{
    public string Currency { get; init; } = "";
    public double Amount { get; init; }
    public string Display => Amount.ToString("0.####", CultureInfo.InvariantCulture) + " " + Currency;
}

// BOM ekranının tüm veri paketi.
public class BomDetailDto
{
    public int BomListId { get; init; }
    public string Name { get; init; } = "";
    public List<BomRowDto> Rows { get; } = new();
    public List<BomTotalDto> Totals { get; } = new();
    public int PricelessCount { get; set; }

    // "12.4 EUR   ·   9.75 USD" — tek satırlık dürüst özet (kör toplama yok).
    public string TotalsDisplay =>
        Totals.Count == 0 ? "—" : string.Join("   ·   ", Totals.Select(t => t.Display));

    // "2 parçanın fiyatı yok" — toplamdan neyin dışarıda kaldığını açıkça söyler.
    public string PricelessNote =>
        PricelessCount == 0 ? "" : $"{PricelessCount} parçanın fiyatı yok (toplama katılmadı).";
}
