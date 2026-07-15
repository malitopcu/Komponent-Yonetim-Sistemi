using Avalonia;
using System;
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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}