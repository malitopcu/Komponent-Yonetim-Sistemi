using System.Globalization;

namespace KomponentSistemi.Services;

// BOM satırındaki tıklanabilir fiyat çipi.
public class OfferOptionDto
{
    public int BomItemId { get; init; }
    public int OfferId { get; init; }
    public string Source { get; init; } = "";
    public string SourcePartNo { get; init; } = "";
    public double Price { get; init; }
    public string Currency { get; init; } = "";
    public bool IsActive { get; init; }

    public string Display =>
        (IsActive ? "✓ " : "") + Price.ToString("0.####", CultureInfo.InvariantCulture) + " " + Currency;
}

public class BomListDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int ItemCount { get; init; }
    public string Display => ItemCount > 0 ? $"{Name} ({ItemCount})" : Name;
}

public class BomRowDto
{
    public int BomItemId { get; init; }
    public int ComponentId { get; init; }
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string TypeName { get; init; } = "";
    public int Quantity { get; init; }
    public string References { get; init; } = "";

    // Sepette kısa özet göstermek için.
    public double? PrimaryValueSi { get; init; }
    public string? PrimaryUnit { get; init; }
    public string ValueDisplay => ValueNormalizer.FormatSi(PrimaryValueSi, PrimaryUnit);

    public string UnitPriceDisplay { get; init; } = "—";
    public string LinePriceDisplay { get; init; } = "—";

    public string DistributorName { get; init; } = "";

    public List<OfferOptionDto> Offers { get; } = new();

    public bool HasPrice { get; init; }
}

// Kur çevirmediğimiz için EUR ve USD ayrı toplanır.
public class BomTotalDto
{
    public string Currency { get; init; } = "";
    public double Amount { get; init; }
    public string Display => Amount.ToString("0.####", CultureInfo.InvariantCulture) + " " + Currency;
}

public class BomDetailDto
{
    public int BomListId { get; init; }
    public string Name { get; init; } = "";
    public List<BomRowDto> Rows { get; } = new();
    public List<BomTotalDto> Totals { get; } = new();
    public int PricelessCount { get; set; }

    // "12.4 EUR   ·   9.75 USD"
    public string TotalsDisplay =>
        Totals.Count == 0 ? "—" : string.Join("   ·   ", Totals.Select(t => t.Display));

    // Toplamın dışında kalan parça sayısı.
    public string PricelessNote =>
        PricelessCount == 0 ? "" : $"{PricelessCount} parçanın fiyatı yok (toplama katılmadı).";
}
