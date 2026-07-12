using System.Text.Json;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// Okuma tarafının servisi: DB'den ekranlık DTO'lar üretir.
public class ComponentQueryService
{
    private readonly AppDbContext _db;

    public ComponentQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ComponentSummaryDto>> GetAllAsync()
    {
        return await _db.Components
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
                PrimaryUnit = _db.ParameterDefinitions
                    .Where(p => p.ComponentTypeId == c.ComponentTypeId && p.HotColumn == "primary")
                    .Select(p => p.Unit).FirstOrDefault(),
                SecondaryUnit = _db.ParameterDefinitions
                    .Where(p => p.ComponentTypeId == c.ComponentTypeId && p.HotColumn == "secondary")
                    .Select(p => p.Unit).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<List<ComponentTypeDto>> GetTypesAsync()
    {
        return await _db.ComponentTypes
            .OrderBy(t => t.Id)
            .Select(t => new ComponentTypeDto { Id = t.Id, Name = t.Name })
            .ToListAsync();
    }

    // Tek bir komponentin tüm detayı: parametreler (sıcak sütunlar + JSON) ve teklifler.
    public async Task<ComponentDetailDto?> GetDetailAsync(int componentId)
    {
        var comp = await _db.Components
            .Include(c => c.ComponentType)
            .Include(c => c.Offers)
            .FirstOrDefaultAsync(c => c.Id == componentId);

        if (comp == null) return null;

        var defs = await _db.ParameterDefinitions
            .Where(p => p.ComponentTypeId == comp.ComponentTypeId)
            .ToListAsync();

        var detail = new ComponentDetailDto
        {
            Mpn = comp.Mpn,
            Manufacturer = comp.Manufacturer,
            TypeName = comp.ComponentType?.Name ?? ""
        };

        // 1) Sıcak sütunlar (primary/secondary) — tanımdaki adı ve birimiyle
        var primaryDef = defs.FirstOrDefault(p => p.HotColumn == "primary");
        if (primaryDef != null && comp.PrimaryValueSi.HasValue)
            detail.Parameters.Add(new ParameterRowDto
            {
                Name = primaryDef.DisplayName,
                Value = ValueNormalizer.FormatSi(comp.PrimaryValueSi, primaryDef.Unit)
            });

        var secondaryDef = defs.FirstOrDefault(p => p.HotColumn == "secondary");
        if (secondaryDef != null && comp.SecondaryValueSi.HasValue)
            detail.Parameters.Add(new ParameterRowDto
            {
                Name = secondaryDef.DisplayName,
                Value = ValueNormalizer.FormatSi(comp.SecondaryValueSi, secondaryDef.Unit)
            });

        // 2) JSON'daki parametreler — anahtarı tanıma çevir, sayısalı birimle formatla
        using var doc = JsonDocument.Parse(comp.ParamsJson);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var def = defs.FirstOrDefault(p => p.Key == prop.Name);
            string name = def?.DisplayName ?? prop.Name;   // tanım yoksa ham anahtar

            string value = prop.Value.ValueKind == JsonValueKind.Number
                ? ValueNormalizer.FormatSi(prop.Value.GetDouble(), def?.Unit)
                : prop.Value.ToString();

            detail.Parameters.Add(new ParameterRowDto { Name = name, Value = value });
        }

        // 3) Teklifler — fiyat + ISO para birimi + snapshot tarihi
        foreach (var o in comp.Offers)
        {
            detail.Offers.Add(new OfferRowDto
            {
                Source = o.Source,
                Price = o.Price.HasValue
                    ? $"{o.Price.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)} {o.Currency}"
                    : "-",
                SourcePartNo = o.SourcePartNo,
                Updated = o.PriceUpdatedAt?.ToString("dd.MM.yyyy") ?? ""
            });
        }

        return detail;
    }
}