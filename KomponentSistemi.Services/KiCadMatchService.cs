using System.Text.Json;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// Şema sembolleri ↔ BOM satırları eşleştirme (referansla: C1↔C1). SALT-OKUMA (Adım 4).
public enum KiCadMatchStatus { FootprintFound, NoFootprint, NoBomMatch, SkippedVirtual }

public class KiCadMatchRow
{
    public string Reference { get; init; } = "";
    public string Value { get; init; } = "";       // şemadaki değer (100n, 240...)
    public string Mpn { get; init; } = "";          // eşleşen komponent ("" yoksa)
    public string Footprint { get; init; } = "";    // çözülen footprint ("" yoksa)
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

public class KiCadMatchService
{
    public class MatchResult
    {
        public List<KiCadMatchRow> Rows { get; } = new();
        public List<string> Errors { get; } = new();
    }

    // schematicPath: .kicad_sch yolu. bomListId null → ilk (varsayılan) proje.
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
            .Select(i => new { i.References, i.Component.Mpn, i.Component.ComponentTypeId,
                               i.Component.ParamsJson, i.Component.PrimaryValueSi, i.Component.SecondaryValueSi })
            .ToList();

        foreach (var sym in read.Symbols)
        {
            if (sym.IsPowerOrVirtual)
            {
                result.Rows.Add(new KiCadMatchRow
                { Reference = sym.Reference, Value = sym.Value, Status = KiCadMatchStatus.SkippedVirtual });
                continue;
            }

            var hit = items.FirstOrDefault(it => RefContains(it.References, sym.Reference));
            if (hit is null)
            {
                result.Rows.Add(new KiCadMatchRow
                { Reference = sym.Reference, Value = sym.Value, Status = KiCadMatchStatus.NoBomMatch });
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

    // "C1, C2, C5" listesinde reference geçiyor mu? (trim + büyük/küçük harf duyarsız)
    private static bool RefContains(string references, string reference)
    {
        foreach (var part in references.Split(','))
            if (string.Equals(part.Trim(), reference.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static (string? package, string? subtype, string? size) ReadParams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (null, null, null);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            return (
                r.TryGetProperty("package", out var p) ? p.GetString() : null,
                r.TryGetProperty("subtype", out var s) ? s.GetString() : null,
                r.TryGetProperty("size", out var z) ? z.GetString() : null);
        }
        catch { return (null, null, null); }
    }
}
