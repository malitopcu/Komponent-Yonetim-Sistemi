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
        // LCSC
        ["pricing($)"]             = "Price",
        ["lcsc part#"]             = "SourcePartNo",
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
        // Pot/trimpot (Direnç alt türü)
        ["resistance (ohms)"]       = "resistance",
        ["taper"]                   = "taper",
        ["number of turns"]         = "turns",
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
        // Osilatörde gerçek boyut burada (footprint için). Sadece "size" tanımı olan tip
        // (osilatör) saklar; diğer tiplerde IgnoreSet fallback'i sessizce atar.
        ["size / dimension"]        = "size",

        // --- LCSC başlıkları ---
        // Transistör
        ["collector - emitter voltage vceo"] = "voltage",
        ["drain to source voltage"]          = "voltage",
        ["current - collector(ic)"]          = "current",
        ["current - continuous drain(id)"]   = "current",
        ["pd - power dissipation"]           = "power",
        ["type"]                             = "subtype",   // NPN/PNP, N/P-Channel, TVS...
        // Regülatör
        ["output voltage"]                   = "output_voltage",
        ["output current"]                   = "output_current",
        ["output type"]                      = "subtype",   // Fixed/Adjustable
        // Direnç
        ["power(watts)"]                     = "power",
        // Diyot
        ["current - rectified"]              = "forward_current",
        ["voltage - forward(vf@if)"]         = "forward_voltage",
        ["voltage - forward(vf)"]            = "forward_voltage",
        ["forward current"]                  = "forward_current",
        ["reverse stand-off voltage (vrwm)"] = "reverse_voltage",
        // Paket (kasa) — "Tape & Reel" gibi paketleme değerleri ImportService'te elenir
        ["package"]                          = "package",

        // Konnektör (DigiKey wire-to-board)
        ["positions per level"]              = "positions",
        ["current"]                          = "current",
        ["voltage"]                          = "voltage",
        ["pitch"]                            = "pitch",
        ["mounting type"]                    = "mounting",
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
        "lead spacing", "lead style", "url", "ürün",
        "uzunluk", "genişlik", "kasa kodu - mm",
        // Konnektör meta (parametre değil)
        "number of levels", "mating orientation", "wire gauge", "wire termination", "color",
        // Pot/trimpot mekanik meta (parametre değil)
        "adjustment type", "resistive material", "termination style", "built in switch",
        "number of gangs", "rotation", "actuator type", "actuator length", "actuator diameter",
        "bushing thread", "mounting type",
        // LCSC meta / şemada olmayan sütunlar
        "availability", "minimum", "multiples", "product detail", "packaging", "number",
        "configuration", "number of outputs", "output configuration", "operating voltage",
        "supply current (iq)", "polarity", "clamping voltage", "dc current gain",
        "vce saturation(vce(sat))", "current - collector cutoff", "emitter-base voltage vebo",
        "transition frequency(ft)", "gate threshold voltage (vgs(th))", "rds(on)", "gate charge(qg)",
        "input capacitance(ciss)", "reverse transfer capacitance (crss@vds)", "output capacitance(coss)",
        "vgs", "reverse leakage current (ir)", "peak pulse power dissipation (ppp)",
        "voltage - breakdown", "peak pulse current (ipp)", "non-repetitive peak forward surge current",
        "operating junction temperature range", "diode configuration", "lamp holder type",
        "viewing angle", "peak wavelength", "illumination color", "lens color", "installation method",
        "color temperature", "luminous intensity", "features", "temperature coefficient",
    };

    public static string? ResolveIdentity(string header) => Lookup(IdentityMap, header);
    public static string? ResolveOffer(string header)    => Lookup(OfferMap, header);
    public static string? ResolveParameter(string header) => Lookup(ParameterMap, header);

    // Başlık bilinen bir meta/atlanabilir sütun mu? (rapordaki gürültüyü keser)
    public static bool IsIgnorable(string header)
        => IgnoreSet.Contains(header.Trim().Replace('İ', 'i').ToLowerInvariant());

    // Başlık RoHS sütunu mu?
    public static bool IsRohsHeader(string header)
    {
        var h = header.Trim().Replace('İ', 'i').ToLowerInvariant();
        return h == "rohs" || h == "rohs status" || h == "rohs uyumlu";
    }

    // RoHS hücre değeri → "Uyumlu" / "Uyumsuz" / "" (bilinmiyor).
    public static string NormalizeRohs(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var v = value.Trim().Replace('İ', 'i').ToLowerInvariant();
        if (v.Contains("uyumsuz") || v.Contains("non-compliant") || v.Contains("noncompliant")
            || v.Contains("not compliant") || v.Contains("değil"))
            return "Uyumsuz";
        if (v.Contains("uyumlu") || v.Contains("compliant") || v.Contains("rohs"))
            return "Uyumlu";
        return "";
    }

    // "Tape & Reel", "Bag-packed", "Bulk" gibi PAKETLEME değeri mi? (kasa/paket adı değil)
    // DigiKey "Package" sütunu paketleme, LCSC "Package" sütunu kasa olduğu için ayırt ederiz.
    public static bool IsPackagingValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.ToLowerInvariant();
        return v.Contains("tape") || v.Contains("reel") || v.Contains("cut")
            || v.Contains("bulk") || v.Contains("tube") || v.Contains("tray")
            || v.Contains("bag") || v.Contains("box");
    }

    private static string? Lookup(Dictionary<string, string> map, string header)
    {
        string key = header.Trim()
            .Replace('İ', 'i')        // U+0130: macOS/ICU'da ToLowerInvariant bunu küçültmüyor!
            .ToLowerInvariant()
            .Replace("\u0307", "");   // olası birleşen-nokta artıkları (platform farkına karşı)
        return map.TryGetValue(key, out var value) ? value : null;
    }
}