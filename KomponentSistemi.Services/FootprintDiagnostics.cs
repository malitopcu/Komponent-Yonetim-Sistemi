using System.Text;
using System.Text.Json;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// FootprintMapper'ı tüm veritabanına karşı çalıştırıp tip bazında kapsam raporu çıkarır.
// Yeni bir paket ailesi eklerken nereyi kaçırdığımızı görmek için.
public static class FootprintDiagnostics
{
    public static string CoverageReport()
    {
        using var db = new AppDbContext();

        var comps = db.Components
            .AsNoTracking()
            .Select(c => new { c.ComponentTypeId, c.ParamsJson, c.PrimaryValueSi, c.SecondaryValueSi })
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

                var fp = FootprintMapper.Resolve(tid, c.ParamsJson, c.PrimaryValueSi, c.SecondaryValueSi);
                if (fp != null)
                {
                    ok++;
                }
                else
                {
                    string key = PackageOf(c.ParamsJson) is { Length: > 0 } pkg ? pkg : "<yok>";
                    miss[key] = miss.GetValueOrDefault(key) + 1;
                }
            }

            grandOk += ok;
            grandTot += tot;

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

    // Sadece "eşleşmedi" satırını etiketlemek için.
    private static string? PackageOf(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("package", out var p) ? p.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
