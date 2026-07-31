namespace KomponentSistemi.Services;

// Bir tipin tek bir parametresinin form tanımı; dinamik Komponent Ekle formu
// alanları bundan üretilir.
public class ParameterFieldDto
{
    public string Key { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Unit { get; init; }
    public string DataType { get; init; } = "text";   // "numeric" | "text"
    public string? HotColumn { get; init; }            // "primary" | "secondary" | null
}
