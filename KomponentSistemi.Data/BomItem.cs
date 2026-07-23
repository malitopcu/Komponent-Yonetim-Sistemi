namespace KomponentSistemi.Data;

// BOM'un tek bir satırı. Fiyat burada tutulmuyor; toplam, komponentin
// tekliflerinden canlı hesaplanıyor.
public class BomItem
{
    public int Id { get; set; }

    public int BomListId { get; set; }
    public BomList BomList { get; set; } = null!;

    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    // "C1, C2, C5" — serbest metin, boş da olabilir.
    public string References { get; set; } = "";

    // Elle seçilen teklif. null ise para birimi kuralı karar verir.
    public int? SelectedOfferId { get; set; }
}
