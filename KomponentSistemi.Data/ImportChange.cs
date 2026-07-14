namespace KomponentSistemi.Data;

// İçe aktarmada yapılan TEK bir değişiklik (geri almak için yeterli bilgi).
public class ImportChange
{
    public int Id { get; set; }

    public int ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    public string EntityType { get; set; } = "";   // "Component" | "Offer"
    public int EntityId { get; set; }
    public string Operation { get; set; } = "";     // "Added" | "Updated"

    // "Updated" için eski değerlerin JSON'u (geri döndürmek için); "Added" için null.
    public string? PrevJson { get; set; }
}
