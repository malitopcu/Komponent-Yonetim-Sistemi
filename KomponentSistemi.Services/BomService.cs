using System.Globalization;
using System.Text;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// BOM (proje / malzeme listesi) tarafının servisi.
// Yazma + okuma bir arada; UI Data'yı göremediği için burada yaşar.
public class BomService
{
    private readonly AppDbContext _db;

    public BomService(AppDbContext db)
    {
        _db = db;
    }

    // --- Aktif (varsayılan) proje ---
    // Gün 10: tek proje ile çalışıyoruz. Yoksa oluştur, varsa ilkini kullan.
    public async Task<int> GetOrCreateDefaultListAsync()
    {
        var existing = await _db.BomLists.OrderBy(b => b.Id).FirstOrDefaultAsync();
        if (existing != null) return existing.Id;

        var list = new BomList { Name = "Proje 1", CreatedAt = DateTime.Now };
        _db.BomLists.Add(list);
        await _db.SaveChangesAsync();
        return list.Id;
    }

    // --- Satır ekleme (upsert) ---
    // Aynı komponent listede varsa: adet artar, referanslar birleşir. Yoksa yeni satır.
    // (Bu davranış (BomListId, ComponentId) benzersiz indeksiyle DB güvencesine de bağlı.)
    public async Task AddItemAsync(int bomListId, int componentId, int quantity, string? references)
    {
        if (quantity < 1) quantity = 1;
        references = references?.Trim() ?? "";

        var existing = await _db.BomItems
            .FirstOrDefaultAsync(i => i.BomListId == bomListId && i.ComponentId == componentId);

        if (existing == null)
        {
            _db.BomItems.Add(new BomItem
            {
                BomListId = bomListId,
                ComponentId = componentId,
                Quantity = quantity,
                References = references
            });
        }
        else
        {
            existing.Quantity += quantity;
            existing.References = MergeReferences(existing.References, references);
        }

        await _db.SaveChangesAsync();
    }

    // İki referans listesini birleştir: tekrarları at, sırayı koru.
    // "R1, R2" + "R2, R5" → "R1, R2, R5"
    private static string MergeReferences(string a, string b)
    {
        var parts = (a + "," + b)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var seen = new List<string>();
        foreach (var p in parts)
            if (!seen.Contains(p, StringComparer.OrdinalIgnoreCase))
                seen.Add(p);

        return string.Join(", ", seen);
    }

    // --- Satır güncelleme (adet + referans elle düzenleme) ---
    public async Task UpdateItemAsync(int bomItemId, int quantity, string? references)
    {
        var item = await _db.BomItems.FindAsync(bomItemId);
        if (item == null) return;

        item.Quantity = quantity < 1 ? 1 : quantity;
        item.References = references?.Trim() ?? "";
        await _db.SaveChangesAsync();
    }

    // --- Satır silme ---
    public async Task RemoveItemAsync(int bomItemId)
    {
        var item = await _db.BomItems.FindAsync(bomItemId);
        if (item == null) return;

        _db.BomItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    // --- Ekran verisi: satırlar + para birimi başına toplam ---
    public async Task<BomDetailDto> GetDetailAsync(int bomListId)
    {
        var list = await _db.BomLists
            .Include(b => b.Items).ThenInclude(i => i.Component).ThenInclude(c => c.ComponentType)
            .Include(b => b.Items).ThenInclude(i => i.Component).ThenInclude(c => c.Offers)
            .FirstOrDefaultAsync(b => b.Id == bomListId);

        var detail = new BomDetailDto
        {
            BomListId = bomListId,
            Name = list?.Name ?? ""
        };

        if (list == null) return detail;

        // Para birimi başına koşan toplam. EUR kendi sepetinde, USD kendi sepetinde.
        var totals = new Dictionary<string, double>();

        foreach (var item in list.Items.OrderBy(i => i.Id))
        {
            // Komponentin tekliflerini para birimine göre grupla; her para biriminde EN UCUZ.
            // Yalnızca hem fiyatı hem para birimi olan teklifler sayılır (dürüstlük: birimsiz fiyat karışmaz).
            var perCurrency = item.Component.Offers
                .Where(o => o.Price.HasValue && !string.IsNullOrWhiteSpace(o.Currency))
                .GroupBy(o => o.Currency!)
                .ToDictionary(g => g.Key, g => g.Min(o => o.Price!.Value));

            bool hasPrice = perCurrency.Count > 0;
            if (!hasPrice) detail.PricelessCount++;

            // Bu satırın her para birimindeki katkısını genel toplama ekle.
            foreach (var kv in perCurrency)
            {
                double line = kv.Value * item.Quantity;
                totals.TryGetValue(kv.Key, out var acc);
                totals[kv.Key] = acc + line;
            }

            detail.Rows.Add(new BomRowDto
            {
                BomItemId = item.Id,
                ComponentId = item.ComponentId,
                Mpn = item.Component.Mpn,
                Manufacturer = item.Component.Manufacturer,
                TypeName = item.Component.ComponentType?.Name ?? "",
                Quantity = item.Quantity,
                References = item.References,
                UnitPriceDisplay = FormatPerCurrency(perCurrency, 1),
                LinePriceDisplay = FormatPerCurrency(perCurrency, item.Quantity),
                HasPrice = hasPrice
            });
        }

        foreach (var kv in totals.OrderBy(k => k.Key))
            detail.Totals.Add(new BomTotalDto { Currency = kv.Key, Amount = kv.Value });

        return detail;
    }

    // {EUR:0.10, USD:0.11} + çarpan → "0.1 EUR  ·  0.11 USD"
    private static string FormatPerCurrency(Dictionary<string, double> perCurrency, int multiplier)
    {
        if (perCurrency.Count == 0) return "—";
        return string.Join("  ·  ", perCurrency
            .OrderBy(kv => kv.Key)
            .Select(kv => (kv.Value * multiplier).ToString("0.####", CultureInfo.InvariantCulture) + " " + kv.Key));
    }

    // --- CSV dışa aktarma (Adım 5'te butonla bağlanacak) ---
    public async Task<string> ExportCsvAsync(int bomListId)
    {
        var detail = await GetDetailAsync(bomListId);

        var sb = new StringBuilder();
        sb.AppendLine("MPN,Üretici,Tip,Adet,Referanslar,Birim Fiyat,Satır Fiyat");

        foreach (var r in detail.Rows)
            sb.AppendLine(string.Join(",",
                Csv(r.Mpn), Csv(r.Manufacturer), Csv(r.TypeName),
                r.Quantity.ToString(CultureInfo.InvariantCulture),
                Csv(r.References), Csv(r.UnitPriceDisplay), Csv(r.LinePriceDisplay)));

        // Toplam bölümü — para birimi başına ayrı satır (kör toplama yok).
        sb.AppendLine();
        foreach (var t in detail.Totals)
            sb.AppendLine(string.Join(",", "TOPLAM", "", "", "", "", "", Csv(t.Display)));
        if (detail.PricelessCount > 0)
            sb.AppendLine(string.Join(",", "Fiyatsız parça", "", "", "", "", "",
                detail.PricelessCount.ToString(CultureInfo.InvariantCulture)));

        return sb.ToString();
    }

    // CSV hücresini kaçır: virgül/tırnak/yeni satır varsa çift tırnakla sarar.
    private static string Csv(string? value)
    {
        value ??= "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
