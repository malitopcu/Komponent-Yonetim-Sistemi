namespace KomponentSistemi.Services;

// Ekranın "komponent listesi" satırı için ihtiyacı olan bilgi paketi.
// EF entity'si değil; veritabanını ve JSON'u bilmez, sadece taşır.
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
}