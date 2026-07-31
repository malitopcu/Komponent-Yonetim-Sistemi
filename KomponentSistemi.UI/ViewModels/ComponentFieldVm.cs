using CommunityToolkit.Mvvm.ComponentModel;

namespace KomponentSistemi.UI.ViewModels;

// Dinamik Komponent Ekle formundaki tek bir giriş alanı. Alanlar seçili tipin
// ParameterDefinitions'ından üretilir; Value kullanıcının yazdığı ham metin.
public partial class ComponentFieldVm : ObservableObject
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";     // "Direnç (Ω)" gibi
    public string? Unit { get; init; }
    public bool IsNumeric { get; init; }
    public string? HotColumn { get; init; }        // "primary" | "secondary" | null

    [ObservableProperty] private string? _value;
}
