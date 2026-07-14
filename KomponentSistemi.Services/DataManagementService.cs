using System.Globalization;
using System.Text.Json;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// Yönetim ekranı: içe aktarma geri alma + tekil silme.
public class DataManagementService
{
    private readonly AppDbContext _db;

    public DataManagementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ImportBatchDto>> GetBatchesAsync()
    {
        return await _db.ImportBatches
            .OrderByDescending(b => b.Id)
            .Select(b => new ImportBatchDto
            {
                Id = b.Id,
                FileName = b.FileName,
                Source = b.Source,
                TypeName = b.TypeName,
                ImportedAt = b.ImportedAt,
                AddedCount = b.AddedCount,
                UpdatedCount = b.UpdatedCount,
                Note = b.Note
            })
            .ToListAsync();
    }

    public async Task<List<ManagedOfferDto>> GetOffersAsync(int componentId)
    {
        return await _db.Offers
            .Where(o => o.ComponentId == componentId)
            .OrderBy(o => o.Source)
            .Select(o => new ManagedOfferDto
            {
                OfferId = o.Id,
                Source = o.Source,
                PriceDisplay = o.Price != null
                    ? o.Price.Value.ToString("0.####", CultureInfo.InvariantCulture) + " " + o.Currency
                    : "-"
            })
            .ToListAsync();
    }

    public async Task DeleteOfferAsync(int offerId)
    {
        var o = await _db.Offers.FindAsync(offerId);
        if (o == null) return;
        _db.Offers.Remove(o);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteComponentAsync(int componentId)
    {
        var c = await _db.Components.Include(x => x.Offers).FirstOrDefaultAsync(x => x.Id == componentId);
        if (c == null) return;
        _db.Components.Remove(c);   // teklifler + BOM satırları cascade ile gider
        await _db.SaveChangesAsync();
    }

    // İçe aktarmayı geri sar: eklenenleri sil, güncellenenleri eski haline döndür, öncekilere dokunma.
    public async Task UndoBatchAsync(int batchId)
    {
        var batch = await _db.ImportBatches
            .Include(b => b.Changes)
            .FirstOrDefaultAsync(b => b.Id == batchId);
        if (batch == null) return;

        // 1) Güncellenenleri eski değerlerine döndür.
        foreach (var ch in batch.Changes.Where(c => c.Operation == "Updated" && c.PrevJson != null))
        {
            if (ch.EntityType == "Offer")
            {
                var o = await _db.Offers.FindAsync(ch.EntityId);
                var p = JsonSerializer.Deserialize<OfferPrev>(ch.PrevJson!);
                if (o != null && p != null)
                {
                    o.Price = p.Price;
                    o.Currency = p.Currency;
                    o.SourcePartNo = p.SourcePartNo;
                    o.PriceUpdatedAt = p.PriceUpdatedAt;
                }
            }
            else if (ch.EntityType == "Component")
            {
                var c = await _db.Components.FindAsync(ch.EntityId);
                var p = JsonSerializer.Deserialize<ComponentPrev>(ch.PrevJson!);
                if (c != null && p != null)
                {
                    c.PrimaryValueSi = p.PrimaryValueSi;
                    c.SecondaryValueSi = p.SecondaryValueSi;
                    c.ParamsJson = p.ParamsJson;
                    c.Rohs = p.Rohs ?? "";
                }
            }
        }

        // 2) Eklenen teklifleri sil.
        foreach (var ch in batch.Changes.Where(c => c.Operation == "Added" && c.EntityType == "Offer"))
        {
            var o = await _db.Offers.FindAsync(ch.EntityId);
            if (o != null) _db.Offers.Remove(o);
        }

        // 3) Eklenen komponentleri sil (kalan teklifleri/BOM satırları cascade).
        foreach (var ch in batch.Changes.Where(c => c.Operation == "Added" && c.EntityType == "Component"))
        {
            var c = await _db.Components.FindAsync(ch.EntityId);
            if (c != null) _db.Components.Remove(c);
        }

        // 4) Bu içe aktarma kaydını sil (değişiklikler cascade).
        _db.ImportBatches.Remove(batch);

        await _db.SaveChangesAsync();
    }
}
