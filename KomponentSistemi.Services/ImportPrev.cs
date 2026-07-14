namespace KomponentSistemi.Services;

// İçe aktarma geri-alması için "eski değer" anlık görüntüleri (JSON olarak saklanır).
public record OfferPrev(double? Price, string? Currency, string SourcePartNo, DateTime? PriceUpdatedAt);
public record ComponentPrev(double? PrimaryValueSi, double? SecondaryValueSi, string ParamsJson, string? Rohs = null);
