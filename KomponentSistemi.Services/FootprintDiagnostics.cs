using System.Text;
using System.Text.Json;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// Adım 3 doğrulaması: FootprintMapper'ı TÜM veritabanına karşı çalıştırıp
// tip bazında kapsam raporu üretir (kaç paket eşleşti / eşleşmedi).
// Salt-okuma; sadece raporlar. UI'a geçici kanca ile bağlanır (Adım 6'da kalkar).
public static class FootprintDiagnostics
{
    public static string CoverageReport()
    {
        using var db = new AppDbContext();

        var comps = db.Components
            .AsNoTracking()
            .Select(c => new { c.ComponentTypeId, c.ParamsJson })
            .ToList();
        var typeNames = db.ComponentTypes.AsNoTracking().ToDictionary(t => t.Id, t => t.Name);

        var sb = new StringBuilder();
        int grandOk = 0, grandTot = 0;

        foreach (var tid in typeNames.Keys.OrderBy(x => x))
        {
            int tot = 0, ok = 0;
            var miss = new Dictionary<string, int>();

            foreach (var c in comps.Where(c => c.ComponentTypeId == tid))
            {
                tot++;
                var (pkg, sub, size) = ReadParams(c.ParamsJson);
                var fp = FootprintMapper.Resolve(tid, pkg, sub, size);
                if (fp != null)
                {
                    ok++;
                }
                else
                {
                    string key = string.IsNullOrWhiteSpace(pkg) ? "<yok>" : pkg!;
                    miss[key] = miss.GetValueOrDefault(key) + 1;
                }
            }

            grandOk += ok; grandTot += tot;
            int pct = tot == 0 ? 0 : 100 * ok / tot;
            sb.AppendLine($"\n=== {typeNames[tid]}: {ok}/{tot} eşleşti (%{pct}) ===");
            foreach (var kv in miss.OrderByDescending(k => k.Value))
                sb.AppendLine($"   {kv.Value,4}  eşleşmedi: {kv.Key}");
        }

        int gpct = grandTot == 0 ? 0 : 100 * grandOk / grandTot;
        sb.AppendLine($"\n######## GENEL: {grandOk}/{grandTot} eşleşti (%{gpct}) ########");
        sb.AppendLine("(Osilatör 'boyut yok' ve TVS 'CASE-1' gibi belirsizler dürüst null.)");
        return sb.ToString();
    }

    // ParamsJson'dan package + subtype + size çek (yoksa/bozuksa null).
    private static (string? package, string? subtype, string? size) ReadParams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (null, null, null);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string? pkg = root.TryGetProperty("package", out var p) ? p.GetString() : null;
            string? sub = root.TryGetProperty("subtype", out var s) ? s.GetString() : null;
            string? size = root.TryGetProperty("size", out var z) ? z.GetString() : null;
            return (pkg, sub, size);
        }
        catch
        {
            return (null, null, null);
        }
    }
}
