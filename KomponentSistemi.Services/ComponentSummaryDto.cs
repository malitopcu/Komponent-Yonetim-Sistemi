namespace KomponentSistemi.Services;

// Ekranın "komponent listesi" satırı için ihtiyacı olan bilgi paketi.
public class ComponentSummaryDto
{
    public int RowNo { get; set; }

    public int Id { get; init; }
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string TypeName { get; init; } = "";
    public double? PrimaryValueSi { get; init; }
    public double? SecondaryValueSi { get; init; }
    public int OfferCount { get; init; }
    public string? Package { get; init; }
    public string? Subtype { get; init; }        // JSON'dan: ZENER, SCHOTTKY...
    public string? Tolerance { get; init; }      // JSON'dan: ±1%, ±0.25PF...
    public string Rohs { get; init; } = "";      // Uyumlu / Uyumsuz / "" (bilinmiyor)

    public string? PrimaryUnit { get; init; }
    public string? SecondaryUnit { get; init; }

    // Görüntü özellikleri
    public string PrimaryDisplay => ValueNormalizer.FormatSi(PrimaryValueSi, PrimaryUnit);
    public string SecondaryDisplay => ValueNormalizer.FormatSi(SecondaryValueSi, SecondaryUnit);
    public string TypeDisplay => Subtype == null ? TypeName : $"{TypeName} ({Subtype})";
}