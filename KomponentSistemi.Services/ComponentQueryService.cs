using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// Okuma tarafının servisi: DB'den ekranlık DTO'lar üretir.
// (ImportService yazar, bu okur — sorumluluklar ayrı.)
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
                OfferCount = c.Offers.Count
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
}