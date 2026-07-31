using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using KomponentSistemi.Services;

namespace KomponentSistemi.UI;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // === GEÇİCİ TEST KANCALARI (Adım 2-3 doğrulaması) — Adım 6'da (KiCad UI) kaldırılacak ===
        // Kullanım:  dotnet run --project KomponentSistemi.UI -- <yol>/KomponentSistemi_devre.kicad_sch
        // Bir .kicad_sch yolu verilirse UI AÇILMAZ; sadece okuma servisi çalışıp çıktı basar.
        if (args.Length > 0 && args[0].EndsWith(".kicad_sch", StringComparison.OrdinalIgnoreCase))
        {
            RunKiCadReaderSmokeTest(args[0]);
            return;
        }

        // Adım 3: footprint eşleme kapsam raporu (tüm DB üzerinde).
        // Kullanım:  dotnet run --project KomponentSistemi.UI -- --footprints
        if (args.Length > 0 && args[0] == "--footprints")
        {
            Console.WriteLine("Footprint eşleme kapsamı (paket → KiCad footprint):");
            Console.WriteLine(FootprintDiagnostics.CoverageReport());
            return;
        }

        // Adım 4: şema ↔ BOM eşleştirme önizlemesi.
        // Kullanım:  dotnet run --project KomponentSistemi.UI -- --match <yol>/x.kicad_sch
        if (args.Length >= 2 && args[0] == "--match")
        {
            RunKiCadMatch(args[1]);
            return;
        }

        // Şemadan BOM Adım 1: her sembolü tip + değer olarak yorumla (DB'ye yazmaz).
        // Kullanım:  dotnet run --project KomponentSistemi.UI -- --interpret <yol>/x.kicad_sch
        if (args.Length >= 2 && args[0] == "--interpret")
        {
            RunSchematicInterpret(args[1]);
            return;
        }

        // Şemadan BOM Adım 2: her sembol için veritabanından aday komponent öner.
        // Kullanım:  dotnet run --project KomponentSistemi.UI -- --suggest <yol>/x.kicad_sch
        if (args.Length >= 2 && args[0] == "--suggest")
        {
            RunSchematicSuggest(args[1]);
            return;
        }

        // MPN'den değer çözme (DB'deki MpnProfile verisiyle).
        // Kullanım:  dotnet run --project KomponentSistemi.UI -- --decode <MPN> [<MPN> ...]
        if (args.Length >= 2 && args[0] == "--decode")
        {
            RunMpnDecode(args.Skip(1));
            return;
        }

        // Adım 5: şemaya footprint yaz (yedekli).
        // Kullanım:  dotnet run --project KomponentSistemi.UI -- --write <yol>/x.kicad_sch
        if (args.Length >= 2 && args[0] == "--write")
        {
            var w = new KiCadWriteService().Write(args[1]);
            foreach (var e in w.Errors) Console.WriteLine("! " + e);
            if (!string.IsNullOrEmpty(w.BackupPath)) Console.WriteLine($"Yedek: {w.BackupPath}");
            foreach (var d in w.Details) Console.WriteLine("  " + d);
            Console.WriteLine($"Yazılan footprint: {w.Written}");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // GEÇİCİ yardımcı: okuma servisini çalıştırıp sonucu terminale basar.
    private static void RunKiCadReaderSmokeTest(string path)
    {
        Console.WriteLine($"KiCad okuma testi (SALT-OKUMA): {path}\n");
        var result = new KiCadSchematicReader().Read(path);

        if (result.Errors.Count > 0)
        {
            Console.WriteLine("Hata/Uyarı:");
            foreach (var e in result.Errors) Console.WriteLine("  - " + e);
            Console.WriteLine();
        }

        int real = result.Symbols.Count(s => !s.IsPowerOrVirtual);
        Console.WriteLine($"Toplam yerleştirilmiş sembol : {result.Symbols.Count}");
        Console.WriteLine($"  Gerçek parça (güç/sanal hariç): {real}");
        Console.WriteLine($"  Güç/sanal (#...)             : {result.Symbols.Count - real}\n");

        Console.WriteLine($"{"Ref",-7} {"lib_id",-34} {"Value",-14} Footprint");
        Console.WriteLine(new string('-', 90));
        foreach (var s in result.Symbols.OrderBy(s => s.Reference, StringComparer.Ordinal))
        {
            string fp = s.HasFootprint ? s.Footprint : "<boş>";
            string tag = s.IsPowerOrVirtual ? "   (sanal)" : "";
            Console.WriteLine($"{s.Reference,-7} {s.LibId,-34} {s.Value,-14} {fp}{tag}");
        }
    }

    // GEÇİCİ: şema ↔ BOM eşleştirme önizlemesi (Adım 4).
    private static void RunKiCadMatch(string path)
    {
        var res = new KiCadMatchService().Match(path);
        foreach (var e in res.Errors) Console.WriteLine("! " + e);
        Console.WriteLine($"{"Ref",-6} {"Değer",-8} {"Durum",-22} MPN → Footprint");
        Console.WriteLine(new string('-', 90));
        foreach (var r in res.Rows.OrderBy(r => r.Reference, StringComparer.Ordinal))
        {
            string tail = r.Status == KiCadMatchStatus.FootprintFound ? $"{r.Mpn} → {r.Footprint}"
                        : r.Status == KiCadMatchStatus.NoFootprint    ? $"{r.Mpn} → (footprint yok)"
                        : "";
            Console.WriteLine($"{r.Reference,-6} {r.Value,-8} {r.StatusText,-22} {tail}");
        }
    }

    // GEÇİCİ: şema sembollerini tip + değer olarak yorumla (Şemadan BOM, Adım 1).
    private static void RunSchematicInterpret(string path)
    {
        var rows = SchematicInterpreter.InterpretFile(path, out var errors);
        foreach (var e in errors) Console.WriteLine("! " + e);

        using var db = new KomponentSistemi.Data.AppDbContext();
        var typeNames = db.ComponentTypes.ToDictionary(t => t.Id, t => t.Name);

        Console.WriteLine($"{"Ref",-6} {"lib_id",-34} {"Tip",-12} {"Alt tür",-9} Değer");
        Console.WriteLine(new string('-', 96));
        foreach (var r in rows.OrderBy(r => r.Reference, StringComparer.Ordinal))
        {
            string type = r.ComponentTypeId is int id ? typeNames.GetValueOrDefault(id, "?") : "BİLİNMİYOR";
            string val = r.ValueKind switch
            {
                SchematicValueKind.Numeric => $"sayı: {r.NumericSi}",
                SchematicValueKind.PartNumber => $"MPN: {r.PartText}",
                _ => "—"
            };
            Console.WriteLine($"{r.Reference,-6} {r.LibId,-34} {type,-12} {(r.Subtype ?? "-"),-9} {val}");
        }
    }

    // GEÇİCİ: her sembol için aday komponent önerisi (Şemadan BOM, Adım 2).
    private static void RunSchematicSuggest(string path)
    {
        var rows = new SchematicBomMatcher().Suggest(path, out var errors);
        foreach (var e in errors) Console.WriteLine("! " + e);

        foreach (var r in rows.OrderBy(r => r.Reference, StringComparer.Ordinal))
        {
            string head = r.ValueKind switch
            {
                SchematicValueKind.Numeric => $"sayı {r.TargetSi}",
                SchematicValueKind.PartNumber => $"MPN '{r.PartText}'",
                _ => "değersiz"
            };
            Console.WriteLine($"\n{r.Reference}  (tip {r.ComponentTypeId?.ToString() ?? "?"}" +
                              $"{(r.Subtype is null ? "" : ", " + r.Subtype)})  {head}  [{r.Status}]");

            if (r.Status == SchematicSuggestStatus.NoValueManual)
            {
                Console.WriteLine($"     değer yok — tipte {r.TypePoolCount} aday, elle seçilecek");
                continue;
            }
            if (r.Candidates.Count == 0)
            {
                Console.WriteLine("     (eşleşme yok)");
                continue;
            }

            Console.WriteLine($"     ({r.Candidates.Count} aday)");
            foreach (var c in r.Candidates)
            {
                string tag = r.ValueKind == SchematicValueKind.Numeric
                    ? (c.IsExact ? "TAM " : $"%{c.RelError * 100:F1} ")
                    : "";
                Console.WriteLine($"     {tag}{c.Display}");
            }
        }
    }

    // GEÇİCİ: MPN'leri DB'deki profillerle çözüp basar.
    private static void RunMpnDecode(IEnumerable<string> mpns)
    {
        using var db = new KomponentSistemi.Data.AppDbContext();
        var profiles = db.MpnProfiles.ToList();
        if (profiles.Count == 0) { Console.WriteLine("DB'de MpnProfile yok (migration çalıştı mı?)"); return; }

        foreach (var mpn in mpns)
        {
            var d = MpnDecoder.TryDecode(mpn, profiles);
            if (d is null)
            {
                var sug = MpnDecoder.SuggestValues(mpn);
                Console.WriteLine(sug.Count > 0
                    ? $"{mpn}: profil yok, ÖNERİ: " + string.Join(", ", sug.Select(x => $"{x.Ohms} Ω ({x.Reason})"))
                    : $"{mpn}: çözülemedi (profil eşleşmedi, öneri de yok)");
                continue;
            }
            // Generic çıktı (tip-bağımsız); birincil/ikincil ham SI olarak basılır.
            var extras = new List<string>();
            if (d.TolerancePercent is double t) extras.Add($"±{t}%");
            if (d.TcrPpm is int tc) extras.Add($"{tc}ppm/°C");
            if (d.Dielectric is string di) extras.Add(di);
            if (d.Package is string pk) extras.Add(pk);
            Console.WriteLine(
                $"{mpn}: tip={d.ComponentTypeId} birincil={d.PrimaryValueSi} ikincil={d.SecondaryValueSi?.ToString() ?? "-"} " +
                $"{string.Join(" ", extras)}  [{d.ProfileName} · {d.Manufacturer}]");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}