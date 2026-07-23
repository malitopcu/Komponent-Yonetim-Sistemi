namespace KomponentSistemi.Data;

public class Offer
{
    public int Id { get; set; }

    // Bu teklif hangi kaynaktan? ("DigiKey", "Mouser"...)
    public string Source { get; set; } = "";

    // Kaynağın kendi parça numarası
    public string SourcePartNo { get; set; } = "";

    public double? Price { get; set; }
    public string? Currency { get; set; }

    // Fiyatın hangi tarihteki değer olduğu
    public DateTime? PriceUpdatedAt { get; set; }

    // Bu teklif hangi komponente ait?
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;
}