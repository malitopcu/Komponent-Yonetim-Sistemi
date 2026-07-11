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
    // Anlamları tipe göre değişir; ParameterDefinitions söyler.
    public double? PrimaryValueSi { get; set; }
    public double? SecondaryValueSi { get; set; }
    
    // Tipe özel parametreler 
    public string ParamsJson { get; set; } = "{}";

    // İlişki: bu parçayı satan kaynaklar
    public List<Offer> Offers { get; set; } = new();
}