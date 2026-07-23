using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

public enum KiCadMatchStatus { FootprintFound, NoFootprint, NoBomMatch, SkippedVirtual }

public class KiCadMatchRow
{
    public string Reference { get; init; } = "";
    public string Value { get; init; } = "";
    public string Mpn { get; init; } = "";
    public string Footprint { get; init; } = "";
    public KiCadMatchStatus Status { get; init; }

    public string StatusText => Status switch
    {
        KiCadMatchStatus.FootprintFound => "✓ footprint",
        KiCadMatchStatus.NoFootprint    => "eşleşti, footprint yok",
        KiCadMatchStatus.NoBomMatch     => "BOM'da yok",
        KiCadMatchStatus.SkippedVirtual => "güç/sanal (atlandı)",
        _ => ""
    };
}

// Şemadaki sembolleri BOM satırlarıyla referans üzerinden eşler (C1 ↔ C1).
// Sadece okur; yazma işi KiCadWriteService'te.
public class KiCadMatchService
{
    public class MatchResult
    {
        public List<KiCadMatchRow> Rows { get; } = new();
        public List<string> Errors { get; } = new();
    }

    // bomListId verilmezse ilk proje kullanılır.
    public MatchResult Match(string schematicPath, int? bomListId = null)
    {
        var result = new MatchResult();

        var read = new KiCadSchematicReader().Read(schematicPath);
        result.Errors.AddRange(read.Errors);

        using var db = new AppDbContext();

        int? listId = bomListId ?? db.BomLists.OrderBy(l => l.Id).Select(l => (int?)l.Id).FirstOrDefault();
        if (listId is null)
        {
            result.Errors.Add("Hiç BOM projesi yok — önce uygulamada bir proje kur.");
            return result;
        }

        var items = db.BomItems.AsNoTracking()
            .Where(i => i.BomListId == listId)
            .Select(i => new
            {
                i.References,
                i.Component.Mpn,
                i.Component.ComponentTypeId,
                i.Component.ParamsJson,
                i.Component.PrimaryValueSi,
                i.Component.SecondaryValueSi
            })
            .ToList();

        foreach (var sym in read.Symbols)
        {
            if (sym.IsPowerOrVirtual)
            {
                result.Rows.Add(new KiCadMatchRow
                {
                    Reference = sym.Reference,
                    Value = sym.Value,
                    Status = KiCadMatchStatus.SkippedVirtual
                });
                continue;
            }

            var hit = items.FirstOrDefault(it => RefContains(it.References, sym.Reference));
            if (hit is null)
            {
                result.Rows.Add(new KiCadMatchRow
                {
                    Reference = sym.Reference,
                    Value = sym.Value,
                    Status = KiCadMatchStatus.NoBomMatch
                });
                continue;
            }

            string? fp = FootprintMapper.Resolve(hit.ComponentTypeId, hit.ParamsJson, hit.PrimaryValueSi, hit.SecondaryValueSi);

            result.Rows.Add(new KiCadMatchRow
            {
                Reference = sym.Reference,
                Value = sym.Value,
                Mpn = hit.Mpn,
                Footprint = fp ?? "",
                Status = fp is not null ? KiCadMatchStatus.FootprintFound : KiCadMatchStatus.NoFootprint
            });
        }

        return result;
    }

    // Referans alanı "C1, C2, C5" gibi serbest metin tutuyor.
    private static bool RefContains(string references, string reference)
    {
        foreach (var part in references.Split(','))
            if (string.Equals(part.Trim(), reference.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
