using System.Globalization;
using System.Text.RegularExpressions;

namespace KomponentSistemi.Services;

// Bir direnç datasheet'inin metninden seri seviyesi özellikleri çıkarır.
// Kurallar üretici-bağımsız kelimelere dayanır (kompozisyon adları, ppm/°C, W, %, °C),
// belirli bir üreticiye/şablona bağlı değil. Belirli parçanın değeri buraya girmez;
// o MPN'den ya da kullanıcıdan gelir.
public class ResistorDatasheetSpecs
{
    public string? Composition { get; set; }
    public List<double> PowerOptionsW { get; } = new();
    public List<int> TcrPpmOptions { get; } = new();
    public List<double> ToleranceOptions { get; } = new();
    public int? OperatingTempMin { get; set; }
    public int? OperatingTempMax { get; set; }
    public string? Mounting { get; set; }

    public double? MaxPowerW => PowerOptionsW.Count > 0 ? PowerOptionsW.Max() : null;
    public double? TightestToleranceP => ToleranceOptions.Count > 0 ? ToleranceOptions.Min() : null;
    public int? TightestTcr => TcrPpmOptions.Count > 0 ? TcrPpmOptions.Min() : null;
}

public static class ResistorDatasheetExtractor
{
    private static readonly (string key, string val)[] Compositions =
    {
        ("metal film", "Metal Film"), ("carbon film", "Carbon Film"), ("thick film", "Thick Film"),
        ("thin film", "Thin Film"), ("metal oxide", "Metal Oxide"), ("wirewound", "Wirewound"),
        ("wire wound", "Wirewound"), ("metal element", "Metal Element"), ("metal foil", "Metal Foil"),
    };

    // ppm/V (gerilim katsayısı) gibi başka ppm'leri elemek için /°C veya /K şart.
    private static readonly Regex PpmRx = new(@"(\d{1,4})\s*ppm\s*/\s*°?\s*[CK]\b", RegexOptions.IgnoreCase);
    private static readonly Regex PowerRx = new(@"(\d+(?:\.\d+)?)\s*W\b");
    private static readonly Regex PctRx = new(@"(\d+(?:\.\d+)?)\s*%");
    private static readonly Regex TempRx = new(@"([+-]?\d{1,3})\s*°?\s*C\b");

    // Tolerans/sıcaklık ancak doğru bağlamda anlamlı; lehim/test satırlarını dışarıda bırakır.
    private static readonly Regex TolCtx = new(@"toler|precision", RegexOptions.IgnoreCase);
    private static readonly Regex TempCtx = new(@"operating|environmental|climatic|category|storage", RegexOptions.IgnoreCase);

    public static ResistorDatasheetSpecs Extract(string? text)
    {
        var s = new ResistorDatasheetSpecs();
        text ??= "";
        string low = text.ToLowerInvariant();

        foreach (var (k, v) in Compositions)
            if (low.Contains(k)) { s.Composition = v; break; }

        foreach (Match m in PpmRx.Matches(text))
            AddInt(s.TcrPpmOptions, int.Parse(m.Groups[1].Value));

        foreach (Match m in PowerRx.Matches(text))
            AddDouble(s.PowerOptionsW, ParseNum(m.Groups[1].Value));

        foreach (var line in text.Split('\n'))
            if (TolCtx.IsMatch(line))
                foreach (Match m in PctRx.Matches(line))
                    AddDouble(s.ToleranceOptions, ParseNum(m.Groups[1].Value));

        ExtractTemp(text, s);

        if (low.Contains("through hole") || low.Contains("axial") || low.Contains("leaded"))
            s.Mounting = "Through Hole";

        s.TcrPpmOptions.Sort();
        s.PowerOptionsW.Sort();
        s.ToleranceOptions.Sort();
        return s;
    }

    private static void ExtractTemp(string text, ResistorDatasheetSpecs s)
    {
        int? min = null, max = null;

        foreach (var line in text.Split('\n'))
        {
            if (!TempCtx.IsMatch(line)) continue;

            var temps = new List<int>();
            foreach (Match m in TempRx.Matches(line))
            {
                int v = int.Parse(m.Groups[1].Value);
                if (v is >= -100 and <= 250) temps.Add(v);
            }

            // Tek sıcaklık bir aralık değil (ör. "125 °C'de yük ömrü") — çift arıyoruz.
            if (temps.Count < 2) continue;

            int lo = temps.Min(), hi = temps.Max();
            min = min is null ? lo : Math.Min(min.Value, lo);
            max = max is null ? hi : Math.Max(max.Value, hi);
        }

        s.OperatingTempMin = min;
        s.OperatingTempMax = max;
    }

    private static double ParseNum(string s) => double.Parse(s, CultureInfo.InvariantCulture);
    private static void AddInt(List<int> list, int v) { if (!list.Contains(v)) list.Add(v); }
    private static void AddDouble(List<double> list, double v) { if (!list.Contains(v)) list.Add(v); }
}
