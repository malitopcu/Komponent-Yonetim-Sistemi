namespace KomponentSistemi.Data;

// Bir proje / malzeme listesi (BOM). Örn: "Güç Kaynağı Kartı v2".
public class BomList
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public List<BomItem> Items { get; set; } = new();
}
