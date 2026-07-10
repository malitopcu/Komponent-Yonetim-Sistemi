namespace KomponentSistemi.Data;

public class Component
{
    public int Id { get; set; }

    // Ortak kimlik alanları (her komponentte var)
    public string Mpn { get; set; } = "";
    public string Manufacturer { get; set; } = "";

    // Tip ilişkisi
    public int ComponentTypeId { get; set; }
    public ComponentType ComponentType { get; set; } = null!;

    // Sıcak arama sütunları (indekslenecek, aralık sorgusu buradan)
    public double? CapacitanceF { get; set; }
    public double? VoltageV { get; set; }

    // Tipe özel parametreler 
    public string ParamsJson { get; set; } = "{}";

    // İlişki: bu parçayı satan kaynaklar
    public List<Offer> Offers { get; set; } = new();
}