namespace KomponentSistemi.Services;

// Başlık → parametre anahtarı eşlemesi.
// Hangi sütuna/JSON'a gideceği ImportService'te ParameterDefinition'dan belirlenir.
public static class SynonymDictionary
{
    // Kimlik alanları (parametre değil, doğrudan Component sütunu)
    private static readonly Dictionary<string, string> IdentityMap = new()
    {
        ["mpn"]                       = "Mpn",
        ["mfr part #"]                = "Mpn",
        ["manufacturer part number"]  = "Mpn",
        ["ürt. parça numarası"]       = "Mpn",
        ["manufacturer"]              = "Manufacturer",
        ["mfr"]                       = "Manufacturer",
        ["ürt."]                      = "Manufacturer",
    };

    // Offer alanları
    private static readonly Dictionary<string, string> OfferMap = new()
    {
        ["price"]                  = "Price",
        ["fiyatlandırma"]          = "Price",
        ["dk part #"]              = "SourcePartNo",
        ["mouser parça numarası"]  = "SourcePartNo",
    };

    // Parametre alanları: başlık → parametre "Key"i (ParameterDefinition.Key ile eşleşir)
    private static readonly Dictionary<string, string> ParameterMap = new()
    {
        // Kondansatör
        ["capacitance"]             = "capacitance",
        ["kapasitans"]              = "capacitance",
        ["voltage - rated"]         = "voltage",
        ["rated voltage"]           = "voltage",
        ["voltaj değer dc"]         = "voltage",
        ["dielectric"]              = "dielectric",
        ["dielektrik"]              = "dielectric",
        ["temperature coefficient"] = "dielectric",
        ["tolerance"]               = "tolerance",
        ["tolerans"]                = "tolerance",
        ["package / case"]          = "package",
        ["kasa kodu - cm"]          = "package",
        // Direnç
        ["resistance"]              = "resistance",
        ["direnç"]                  = "resistance",
        ["power"]                   = "power",
        ["güç"]                     = "power",
        // Osilatör
        ["frequency"]               = "frequency",
        ["frekans"]                 = "frequency",
    };

    public static string? ResolveIdentity(string header) => Lookup(IdentityMap, header);
    public static string? ResolveOffer(string header)    => Lookup(OfferMap, header);
    public static string? ResolveParameter(string header) => Lookup(ParameterMap, header);

    private static string? Lookup(Dictionary<string, string> map, string header)
    {
        string key = header.Trim().ToLowerInvariant();
        return map.TryGetValue(key, out var value) ? value : null;
    }
}