namespace KomponentSistemi.Services;

// Bir parametrik aramanın tarifi. null = "bu filtre kapalı".
public class SearchCriteria
{
    // Hangi komponent tipi? (1=Kondansatör vb.) null = tüm tipler
    public int? ComponentTypeId { get; set; }

    // MPN/üretici içinde metin arama ("GCM" yazınca içerenler). null/boş = kapalı
    public string? Text { get; set; }

    // Sıcak sütun aralıkları (SI cinsinden!) — kondansatörde Farad, dirençte Ohm
    public double? MinPrimary { get; set; }
    public double? MaxPrimary { get; set; }
    public double? MinSecondary { get; set; }
    public double? MaxSecondary { get; set; }

    // Kategorik filtreler: Key → aranan kanonik değer
    // örn: ["dielectric"] = "C0G/NP0"
    public Dictionary<string, string> CategoricalFilters { get; } = new();
}