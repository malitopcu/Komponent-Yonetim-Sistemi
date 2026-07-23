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
        IQueryable<Component> query = _db.Components;

        if (criteria.ComponentTypeId.HasValue)
            query = query.Where(c => c.ComponentTypeId == criteria.ComponentTypeId.Value);

        var text = criteria.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            var upper = text.ToUpperInvariant();
            query = query.Where(c => c.Mpn.ToUpper().Contains(upper)
                                  || c.Manufacturer.ToUpper().Contains(upper)
                                  || EF.Functions.Like(SqlJson.Extract(c.ParamsJson, "$.subtype"), "%" + text + "%"));
        }

        if (criteria.MinPrimary.HasValue)
            query = query.Where(c => c.PrimaryValueSi >= criteria.MinPrimary.Value);

        if (criteria.MaxPrimary.HasValue)
            query = query.Where(c => c.PrimaryValueSi <= criteria.MaxPrimary.Value);

        if (criteria.MinSecondary.HasValue)
            query = query.Where(c => c.SecondaryValueSi >= criteria.MinSecondary.Value);

        if (criteria.MaxSecondary.HasValue)
            query = query.Where(c => c.SecondaryValueSi <= criteria.MaxSecondary.Value);

        foreach (var (key, value) in criteria.CategoricalFilters)
        {
            var path = $"$.{key}";
            var wanted = value;
            query = query.Where(c => SqlJson.Extract(c.ParamsJson, path) == wanted);
        }

        return await query
            .Select(c => new ComponentSummaryDto
            {
                Id = c.Id,
                Mpn = c.Mpn,
                Manufacturer = c.Manufacturer,
                TypeName = c.ComponentType!.Name,
                PrimaryValueSi = c.PrimaryValueSi,
                SecondaryValueSi = c.SecondaryValueSi,
                OfferCount = c.Offers.Count,
                Package = SqlJson.Extract(c.ParamsJson, "$.package"),
                Subtype = SqlJson.Extract(c.ParamsJson, "$.subtype"),
                Tolerance = SqlJson.Extract(c.ParamsJson, "$.tolerance"),
                Rohs = c.Rohs,
                PrimaryUnit = _db.ParameterDefinitions
                    .Where(p => p.ComponentTypeId == c.ComponentTypeId && p.HotColumn == "primary")
                    .Select(p => p.Unit).FirstOrDefault(),
                SecondaryUnit = _db.ParameterDefinitions
                    .Where(p => p.ComponentTypeId == c.ComponentTypeId && p.HotColumn == "secondary")
                    .Select(p => p.Unit).FirstOrDefault()
            })
            .ToListAsync();
    }
}