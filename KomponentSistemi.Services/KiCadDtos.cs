namespace KomponentSistemi.Services;

// KiCad şemasına YERLEŞTİRİLMİŞ tek bir sembol (C1, R2, U1, #PWR03...).
// Dikkat: bu, dosyanın başındaki (lib_symbols ...) içindeki kütüphane TANIMI değil;
// tahtaya konmuş gerçek bir örnektir (bir "instance").
//
// Analoji: (lib_symbols) = tarif defteri ("Direnç nasıl çizilir"), yerleştirilmiş
// sembol = o tarifle pişmiş gerçek tabak ("R1, 240 ohm, şu köşede").
public class KiCadSymbol
{
    public string Reference { get; init; } = "";   // "C1", "U1", "#PWR03"
    public string LibId { get; init; } = "";        // "Device:C", "Regulator_Linear:LM317_TO-220"
    public string Value { get; init; } = "";        // "100n", "LM317_TO-220"
    public string Footprint { get; init; } = "";    // "" (boş) ya da "Package_...:TO-220-3_Vertical"
    public string Uuid { get; init; } = "";         // dosyadaki benzersiz kimlik; Adım 5'te hedefli yazmaya yarar

    // KiCad kuralı: referansı '#' ile başlayan semboller SANALDIR (güç bayrağı, etiket).
    // BOM'a girmez, footprint atanmaz. Örn: #PWR01 = bir GND sembolü.
    // Not: eşleştirmede (Adım 4) bunları elerken bu bayrağı kullanacağız.
    public bool IsPowerOrVirtual => Reference.StartsWith('#');

    // Footprint zaten atanmış mı? (boş = biz atayacağız)
    public bool HasFootprint => !string.IsNullOrWhiteSpace(Footprint);

    // Kütüphane ailesi (namespace): "Device:C" → "Device",
    // "Regulator_Linear:LM317_TO-220" → "Regulator_Linear".
    public string LibNamespace
    {
        get
        {
            int idx = LibId.IndexOf(':');
            return idx >= 0 ? LibId.Substring(0, idx) : LibId;
        }
    }

    // Terminal/log için okunaklı tek satır.
    public override string ToString() =>
        $"{Reference,-7} {LibId,-34} {Value,-14} fp={(HasFootprint ? Footprint : "<boş>")}";
}
