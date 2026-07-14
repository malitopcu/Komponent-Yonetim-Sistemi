namespace KomponentSistemi.Data;

// Bir içe aktarma "partisi" — geri alabilmek için o içe aktarmanın kimliği ve özeti.
public class ImportBatch
{
    public int Id { get; set; }

    public string FileName { get; set; } = "";
    public string Source { get; set; } = "";
    public string TypeName { get; set; } = "";
    public DateTime ImportedAt { get; set; }

    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }

    // Kullanıcının bu içe aktarmaya düştüğü kısa not (isteğe bağlı).
    public string Note { get; set; } = "";

    // Bu partide yapılan tekil değişiklikler (geri alma buradan okur).
    public List<ImportChange> Changes { get; set; } = new();
}
