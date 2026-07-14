namespace KomponentSistemi.Services;

// Yönetim ekranı: içe aktarma partisi özeti.
public class ImportBatchDto
{
    public int Id { get; init; }
    public string FileName { get; init; } = "";
    public string Source { get; init; } = "";
    public string TypeName { get; init; } = "";
    public DateTime ImportedAt { get; init; }
    public int AddedCount { get; init; }
    public int UpdatedCount { get; init; }
    public string Note { get; init; } = "";

    public string DateDisplay => ImportedAt.ToString("dd.MM.yyyy HH:mm");
    public string Summary => $"+{AddedCount} eklendi · ~{UpdatedCount} güncellendi";
}

// Yönetim ekranı: bir komponentin tek teklifi (silmek için Id ile).
public class ManagedOfferDto
{
    public int OfferId { get; init; }
    public string Source { get; init; } = "";
    public string PriceDisplay { get; init; } = "";
}
