namespace KomponentSistemi.Services;

// Başlık → parametre anahtarı eşlemesi.
public static class SynonymDictionary
{
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

    private static readonly Dictionary<string, string> OfferMap = new()
    {
        ["price"]                  = "Price",
        ["fiyatlandırma"]          = "Price",
        ["dk part #"]              = "SourcePartNo",
        ["mouser parça numarası"]  = "SourcePartNo",
    };

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
        ["paket / kasa"]            = "package",
        // Direnç
        ["resistance"]              = "resistance",
        ["direnç"]                  = "resistance",
        ["power"]                   = "power",
        ["power (watts)"]           = "power",
        ["güç"]                     = "power",
        // Diyot (DigiKey EN)
        ["voltage - dc reverse (vr) (max)"]   = "reverse_voltage",
        ["voltage - zener (nom) (vz)"]        = "reverse_voltage",
        ["current - average rectified (io)"]  = "forward_current",
        ["voltage - forward (vf) (max) @ if"] = "forward_voltage",
        ["technology"]                        = "subtype",
        // Diyot (Mouser TR)
        ["vz - zener voltaj"]       = "reverse_voltage",
        ["tepe ters voltaj"]        = "reverse_voltage",
        ["if - ileri akım"]         = "forward_current",
        ["vf - ileri voltaj"]       = "forward_voltage",
        // Osilatör
        ["frequency"]               = "frequency",
        ["frekans"]                 = "frequency",
        ["voltage - supply"]        = "supply_voltage",
        ["frequency stability"]     = "frequency_tolerance",
    };

    // Bilinen meta/lojistik/medya başlıkları — komponent parametresi değil, sessizce atlanır.
    private static readonly HashSet<string> IgnoreSet = new()
    {
        "datasheet", "veri sayfası", "image", "resim", "supplier", "description", "açıklama",
        "stock", "stok", "stok durumu", "@ qty", "min qty", "package", "paketleme",
        "series", "seri", "product status", "yaşam döngüsü", "ürün detayı", "product detail",
        "operating temperature", "minimum çalışma sıcaklığı", "maksimum çalışma sıcaklığı",
        "features", "ratings", "vasıf", "applications", "failure rate",
        "mounting type", "sonlandırma stili", "sonlandırma",
        "size / dimension", "height - seated (max)", "yükseklik", "thickness (max)",
        "lead spacing", "lead style", "url", "rohs", "ürün",
        "uzunluk", "genişlik", "kasa kodu - mm",
    };

    public static string? ResolveIdentity(string header) => Lookup(IdentityMap, header);
    public static string? ResolveOffer(string header)    => Lookup(OfferMap, header);
    public static string? ResolveParameter(string header) => Lookup(ParameterMap, header);

    // Başlık bilinen bir meta/atlanabilir sütun mu? (rapordaki gürültüyü keser)
    public static bool IsIgnorable(string header)
        => IgnoreSet.Contains(header.Trim().Replace('İ', 'i').ToLowerInvariant());

    private static string? Lookup(Dictionary<string, string> map, string header)
    {
        string key = header.Trim()
            .Replace('İ', 'i')        // U+0130: macOS/ICU'da ToLowerInvariant bunu küçültmüyor!
            .ToLowerInvariant()
            .Replace("\u0307", "");   // olası birleşen-nokta artıkları (platform farkına karşı)
        return map.TryGetValue(key, out var value) ? value : null;
    }
}