using System.Globalization;

namespace KomponentSistemi.Services;

// BOM satırındaki tek bir teklif seçeneği (tıklanabilir fiyat çipi).
public class OfferOptionDto
{
    public int BomItemId { get; init; }   // hangi satır (tıkla-seç komutu için)
    public int OfferId { get; init; }
    public string Source { get; init; } = "";
    public string SourcePartNo { get; init; } = "";
    public double Price { get; init; }
    public string Currency { get; init; } = "";
    public bool IsActive { get; init; }

    // Aktifse başında işaret: "✓ 0.14 USD", değilse "0.14 USD"
    public string Display =>
        (IsActive ? "✓ " : "") + Price.ToString("0.####", CultureInfo.InvariantCulture) + " " + Currency;
}

// Proje seçici için özet: "Güç Kaynağı (5)".
public class BomListDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int ItemCount { get; init; }
    public string Display => ItemCount > 0 ? $"{Name} ({ItemCount})" : Name;
}

// BOM ekranındaki tek satır: bir komponent + adet + referans + aktif fiyat + tüm teklifler.
public class BomRowDto
{
    public int BomItemId { get; init; }
    public int ComponentId { get; init; }
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string TypeName { get; init; } = "";
    public int Quantity { get; init; }
    public string References { get; init; } = "";

    // Aktif teklife göre (tek fiyat) — satır toplamı ve CSV bunu kullanır.
    public string UnitPriceDisplay { get; init; } = "—";
    public string LinePriceDisplay { get; init; } = "—";

    // Aktif teklifi veren distribütör (Source).
    public string DistributorName { get; init; } = "";

    // Bu parçanın tüm teklifleri (tıklanabilir çipler); aktif olan IsActive=true.
    public List<OfferOptionDto> Offers { get; } = new();

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
