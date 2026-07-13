namespace KomponentSistemi.Services;

// Tip seçilince sıcak sütun başlıkları: "Direnç (Ω)", "Güç (W)" gibi.
public class TypeHeaderDto
{
    public int TypeId { get; init; }
    public string PrimaryHeader { get; init; } = "Değer";
    public string SecondaryHeader { get; init; } = "2. Değer";
}
