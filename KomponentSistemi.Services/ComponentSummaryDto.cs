namespace KomponentSistemi.Services;

// Ekranın "komponent listesi" satırı için ihtiyacı olan bilgi paketi.
public class ComponentSummaryDto
{
    public int RowNo { get; set; }   // ekrandaki sıra numarası (1, 2, 3...)

    public int Id { get; init; }
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string TypeName { get; init; } = "";
    public double? PrimaryValueSi { get; init; }
    public double? SecondaryValueSi { get; init; }
    public int OfferCount { get; init; }
    public string? Package { get; init; }         // JSON'dan: 0402, SOD-123...

    // Tipin sıcak sütun birimleri (ParameterDefinition.Unit: F, Ω, Hz, V, W...)
    public string? PrimaryUnit { get; init; }
    public string? SecondaryUnit { get; init; }

    // Ekranda gösterilecek insanca metinler
    public string PrimaryDisplay => ValueNormalizer.FormatSi(PrimaryValueSi, PrimaryUnit);
    public string SecondaryDisplay => ValueNormalizer.FormatSi(SecondaryValueSi, SecondaryUnit);
}