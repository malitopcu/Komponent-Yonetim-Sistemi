namespace KomponentSistemi.Data;

public class Component
{
    public int Id { get; set; }

    public string Mpn { get; set; } = "";
    public string Manufacturer { get; set; } = "";

    public int ComponentTypeId { get; set; }
    public ComponentType ComponentType { get; set; } = null!;

    // Aralık sorguları buradan gidiyor. Anlamları tipe göre değişir,
    // hangi parametreye karşılık geldiğini ParameterDefinitions söyler.
    public double? PrimaryValueSi { get; set; }
    public double? SecondaryValueSi { get; set; }

    public string ParamsJson { get; set; } = "{}";

    // Her tipte olan bir özellik, o yüzden ParamsJson'da değil kendi sütununda.
    public string Rohs { get; set; } = "";

    public List<Offer> Offers { get; set; } = new();
}