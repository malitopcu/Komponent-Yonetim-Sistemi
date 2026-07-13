namespace KomponentSistemi.Data;

// Bir "proje" / malzeme listesi (BOM = Bill of Materials).
// Örn: "Güç Kaynağı Kartı v2". İçinde birden çok satır (BomItem) barındırır.
// Analoji: alışveriş listesinin başlığı; satırlar aşağıda.
public class BomList
{
    public int Id { get; set; }

    // Projenin adı — kullanıcı verir.
    public string Name { get; set; } = "";

    // Ne zaman oluşturuldu (snapshot mantığı: o anki tarih donar).
    public DateTime CreatedAt { get; set; }

    // Bu listedeki satırlar (her satır = bir komponent + adet + referans).
    public List<BomItem> Items { get; set; } = new();
}
