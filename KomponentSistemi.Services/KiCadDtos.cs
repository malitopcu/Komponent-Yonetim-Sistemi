namespace KomponentSistemi.Services;

// Şemaya yerleştirilmiş tek bir sembol (C1, R2, U1, #PWR03...).
// Dosyanın başındaki (lib_symbols ...) altındaki kütüphane tanımı değil.
public class KiCadSymbol
{
    public string Reference { get; init; } = "";   // "C1", "U1", "#PWR03"
    public string LibId { get; init; } = "";        // "Device:C", "Regulator_Linear:LM317_TO-220"
    public string Value { get; init; } = "";        // "100n", "LM317_TO-220"
    public string Footprint { get; init; } = "";    // "" (boş) ya da "Package_...:TO-220-3_Vertical"
    public string Uuid { get; init; } = "";         // dosyadaki benzersiz kimlik

    // '#' ile başlayan referanslar KiCad'de sanaldır (güç bayrağı, etiket).
    // BOM'a girmez, footprint atanmaz.
    public bool IsPowerOrVirtual => Reference.StartsWith('#');

    // Boşsa biz atayacağız.
    public bool HasFootprint => !string.IsNullOrWhiteSpace(Footprint);

    // "Regulator_Linear:LM317_TO-220" → "Regulator_Linear"
    public string LibNamespace
    {
        get
        {
            int idx = LibId.IndexOf(':');
            return idx >= 0 ? LibId.Substring(0, idx) : LibId;
        }
    }

    public override string ToString() =>
        $"{Reference,-7} {LibId,-34} {Value,-14} fp={(HasFootprint ? Footprint : "<boş>")}";
}
