namespace KomponentSistemi.Services;

// Detay panelindeki tek parametre satırı: "Dielektrik: C0G/NP0"
public class ParameterRowDto
{
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";
}

// Detay panelindeki tek teklif satırı: "Mouser | 0.12 EUR | 11.07.2026"
public class OfferRowDto
{
    public string Source { get; init; } = "";
    public string Price { get; init; } = "";
    public string SourcePartNo { get; init; } = "";
    public string Updated { get; init; } = "";
}

// Panelin tamamının veri paketi
public class ComponentDetailDto
{
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string TypeName { get; init; } = "";
    public List<ParameterRowDto> Parameters { get; } = new();
    public List<OfferRowDto> Offers { get; } = new();
}