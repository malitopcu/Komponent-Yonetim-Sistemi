using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// Parametrik arama motoru: SearchCriteria alır, eşleşen komponentleri DTO olarak döndürür.
public class SearchService
{
    private readonly AppDbContext _db;

    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ComponentSummaryDto>> SearchAsync(SearchCriteria criteria)
    {
        // Tarif defterini aç: henüz DB'ye GİDİLMEDİ, sadece sorgu kuruluyor
        IQueryable<Component> query = _db.Components;

        // Her filtre, kullanıcı doldurmuşsa tarife bir satır ekler:

        if (criteria.ComponentTypeId.HasValue)
            query = query.Where(c => c.ComponentTypeId == criteria.ComponentTypeId.Value);

        // Metin arama: iki taraf da büyük harfe çevrilerek harf duyarlılığı kaldırılır
        var text = criteria.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            var upper = text.ToUpperInvariant();
            query = query.Where(c => c.Mpn.ToUpper().Contains(upper)
                                  || c.Manufacturer.ToUpper().Contains(upper));
        }

        if (criteria.MinPrimary.HasValue)
            query = query.Where(c => c.PrimaryValueSi >= criteria.MinPrimary.Value);

        if (criteria.MaxPrimary.HasValue)
            query = query.Where(c => c.PrimaryValueSi <= criteria.MaxPrimary.Value);

        if (criteria.MinSecondary.HasValue)
            query = query.Where(c => c.SecondaryValueSi >= criteria.MinSecondary.Value);

        if (criteria.MaxSecondary.HasValue)
            query = query.Where(c => c.SecondaryValueSi <= criteria.MaxSecondary.Value);

        // Kategorik filtreler: her biri JSON içinde json_extract ile aranır
        foreach (var (key, value) in criteria.CategoricalFilters)
        {
            var path = $"$.{key}";   // "dielectric" → "$.dielectric"
            var wanted = value;
            query = query.Where(c => SqlJson.Extract(c.ParamsJson, path) == wanted);
        }

        // Tarif tamam → ŞİMDİ tek SQL üretilir, DB'ye gidilir, DTO'lara dökülür
        return await query
            .Select(c => new ComponentSummaryDto
            {
                Id = c.Id,
                Mpn = c.Mpn,
                Manufacturer = c.Manufacturer,
                TypeName = c.ComponentType!.Name,
                PrimaryValueSi = c.PrimaryValueSi,
                SecondaryValueSi = c.SecondaryValueSi,
                OfferCount = c.Offers.Count
            })
            .ToListAsync();
    }
}