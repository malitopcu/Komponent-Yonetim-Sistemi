using System.Globalization;
using System.Text;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// Proje (BOM) listelerinin okuma ve yazma işleri.
// UI, Data katmanını doğrudan görmediği için hepsi buradan geçiyor.
public class BomService
{
    private readonly AppDbContext _db;

    public BomService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetOrCreateDefaultListAsync()
    {
        var existing = await _db.BomLists.OrderBy(b => b.Id).FirstOrDefaultAsync();
        if (existing != null) return existing.Id;

        var list = new BomList { Name = "Proje 1", CreatedAt = DateTime.Now };
        _db.BomLists.Add(list);
        await _db.SaveChangesAsync();
        return list.Id;
    }

    public async Task<List<BomListDto>> GetListsAsync()
    {
        return await _db.BomLists
            .OrderBy(b => b.Id)
            .Select(b => new BomListDto { Id = b.Id, Name = b.Name, ItemCount = b.Items.Count })
            .ToListAsync();
    }

    public async Task<int> CreateListAsync(string name)
    {
        var list = new BomList
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Yeni Proje" : name.Trim(),
            CreatedAt = DateTime.Now
        };
        _db.BomLists.Add(list);
        await _db.SaveChangesAsync();
        return list.Id;
    }

    public async Task RenameListAsync(int bomListId, string name)
    {
        var list = await _db.BomLists.FindAsync(bomListId);
        if (list == null || string.IsNullOrWhiteSpace(name)) return;
        list.Name = name.Trim();
        await _db.SaveChangesAsync();
    }

    public async Task DeleteListAsync(int bomListId)
    {
        var list = await _db.BomLists.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == bomListId);
        if (list == null) return;
        _db.BomLists.Remove(list);
        await _db.SaveChangesAsync();
    }

    // Aynı komponent listede varsa adet artar ve referanslar birleşir.
    // (BomListId, ComponentId) benzersiz indeksi de bunu zorunlu kılıyor.
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

    public async Task UpdateItemAsync(int bomItemId, int quantity, string? references)
    {
        var item = await _db.BomItems.FindAsync(bomItemId);
        if (item == null) return;

        item.Quantity = quantity < 1 ? 1 : quantity;
        item.References = references?.Trim() ?? "";
        await _db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int bomItemId)
    {
        var item = await _db.BomItems.FindAsync(bomItemId);
        if (item == null) return;

        _db.BomItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    // En çok teklifi olan para birimi başta gelsin ki varsayılan seçim mantıklı olsun.
    public async Task<List<string>> GetCurrenciesAsync()
    {
        return await _db.Offers
            .Where(o => o.Currency != null && o.Currency != "")
            .GroupBy(o => o.Currency!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToListAsync();
    }

    public async Task<BomDetailDto> GetDetailAsync(int bomListId, string? preferredCurrency)
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

        string? baseCurrency = string.IsNullOrWhiteSpace(preferredCurrency)
            ? MajorityCurrency(list)
            : preferredCurrency;

        var totals = new Dictionary<string, double>();

        // Kondansatör → F, Direnç → Ω ...
        var primaryUnits = await _db.ParameterDefinitions
            .Where(p => p.HotColumn == "primary")
            .ToDictionaryAsync(p => p.ComponentTypeId, p => p.Unit);

        foreach (var item in list.Items.OrderBy(i => i.Id))
        {
            // Para birimi olmayan fiyat işimize yaramaz, toplamı bozar.
            var priced = item.Component.Offers
                .Where(o => o.Price.HasValue && !string.IsNullOrWhiteSpace(o.Currency))
                .ToList();

            // Kullanıcının elle seçtiği teklif varsa o kazanır.
            Offer? active = null;
            if (item.SelectedOfferId is int selId)
                active = priced.FirstOrDefault(o => o.Id == selId);
            active ??= PickOffer(priced, baseCurrency);

            var row = new BomRowDto
            {
                BomItemId = item.Id,
                ComponentId = item.ComponentId,
                Mpn = item.Component.Mpn,
                Manufacturer = item.Component.Manufacturer,
                TypeName = item.Component.ComponentType?.Name ?? "",
                Quantity = item.Quantity,
                References = item.References,
                PrimaryValueSi = item.Component.PrimaryValueSi,
                PrimaryUnit = primaryUnits.GetValueOrDefault(item.Component.ComponentTypeId),
                HasPrice = active != null,
                UnitPriceDisplay = active == null ? "—" : Money(active.Price!.Value, active.Currency!),
                LinePriceDisplay = active == null ? "—" : Money(active.Price!.Value * item.Quantity, active.Currency!),
                DistributorName = active?.Source ?? ""
            };

            foreach (var o in priced.OrderBy(o => o.Currency).ThenBy(o => o.Price))
                row.Offers.Add(new OfferOptionDto
                {
                    BomItemId = item.Id,
                    OfferId = o.Id,
                    Source = o.Source,
                    SourcePartNo = o.SourcePartNo,
                    Price = o.Price!.Value,
                    Currency = o.Currency!,
                    IsActive = active != null && o.Id == active.Id
                });

            if (active == null)
            {
                detail.PricelessCount++;
            }
            else
            {
                double line = active.Price!.Value * item.Quantity;
                totals.TryGetValue(active.Currency!, out var acc);
                totals[active.Currency!] = acc + line;
            }

            detail.Rows.Add(row);
        }

        foreach (var kv in totals.OrderBy(k => k.Key))
            detail.Totals.Add(new BomTotalDto { Currency = kv.Key, Amount = kv.Value });

        return detail;
    }

    // Her komponent bir para birimini bir kez sayar, yoksa 20 teklifli tek parça
    // bütün listenin para birimini belirlerdi.
    private static string? MajorityCurrency(BomList list)
    {
        return list.Items
            .Select(i => i.Component)
            .SelectMany(c => c.Offers
                .Where(o => o.Price.HasValue && !string.IsNullOrWhiteSpace(o.Currency))
                .Select(o => o.Currency!)
                .Distinct())
            .GroupBy(cur => cur)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefault();
    }

    public async Task SetSelectedOfferAsync(int bomItemId, int offerId)
    {
        var item = await _db.BomItems.FindAsync(bomItemId);
        if (item == null) return;

        item.SelectedOfferId = offerId;
        await _db.SaveChangesAsync();
    }

    // Tercih edilen para biriminde varsa oradaki en ucuz, yoksa genel en ucuz.
    private static Offer? PickOffer(List<Offer> priced, string? preferredCurrency)
    {
        if (priced.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(preferredCurrency))
        {
            var inPref = priced
                .Where(o => string.Equals(o.Currency, preferredCurrency, StringComparison.OrdinalIgnoreCase))
                .OrderBy(o => o.Price!.Value)
                .ToList();
            if (inPref.Count > 0) return inPref[0];
        }

        return priced.OrderBy(o => o.Price!.Value).First();
    }

    private static string Money(double amount, string currency)
        => amount.ToString("0.####", CultureInfo.InvariantCulture) + " " + currency;

    public async Task<string> ExportCsvAsync(int bomListId, string? preferredCurrency)
    {
        var detail = await GetDetailAsync(bomListId, preferredCurrency);

        var sb = new StringBuilder();
        sb.AppendLine("MPN,Üretici,Tip,Adet,Referanslar,Birim Fiyat,Satır Fiyat,Distribütör");

        foreach (var r in detail.Rows)
            sb.AppendLine(string.Join(",",
                Csv(r.Mpn), Csv(r.Manufacturer), Csv(r.TypeName),
                r.Quantity.ToString(CultureInfo.InvariantCulture),
                Csv(r.References), Csv(r.UnitPriceDisplay), Csv(r.LinePriceDisplay),
                Csv(r.DistributorName)));

        // Kur çevirmiyoruz, o yüzden her para birimi kendi satırında.
        sb.AppendLine();
        foreach (var t in detail.Totals)
            sb.AppendLine(string.Join(",", "TOPLAM", "", "", "", "", "", Csv(t.Display), ""));
        if (detail.PricelessCount > 0)
            sb.AppendLine(string.Join(",", "Fiyatsız parça", "", "", "", "", "",
                detail.PricelessCount.ToString(CultureInfo.InvariantCulture), ""));

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        value ??= "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
