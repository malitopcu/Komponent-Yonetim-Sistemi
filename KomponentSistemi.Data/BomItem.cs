namespace KomponentSistemi.Data;

// BOM listesindeki tek bir satır: "5 adet şu direnç, referanslar R1, R2, R3".
// Fiyat burada TUTULMAZ; toplam, komponentin Offer'larından CANLI hesaplanır
// (fiyat kilitleme/snapshot ileride backlog işi).
public class BomItem
{
    public int Id { get; set; }

    // Hangi projeye ait? (BomList 1 — * BomItem)
    public int BomListId { get; set; }
    public BomList BomList { get; set; } = null!;

    // Hangi komponent? (Component 1 — * BomItem)
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    // Kaç adet lazım.
    public int Quantity { get; set; } = 1;

    // Şemadaki referans etiketleri: "C1, C2, C5" (serbest metin; yoksa boş).
    public string References { get; set; } = "";
}
