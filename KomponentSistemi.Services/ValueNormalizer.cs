using System.Globalization;
using System.Text.RegularExpressions;

namespace KomponentSistemi.Services;

public class ValueNormalizer
{
    // SI ön ekleri. µ için üç ayrı karakter dolaşımda, üçünü de tanıyoruz.
    private static readonly Dictionary<string, double> SiPrefixes = new()
    {
        ["p"] = 1e-12,
        ["n"] = 1e-9,
        ["u"] = 1e-6,
        ["µ"] = 1e-6,
        ["μ"] = 1e-6,
        ["m"] = 1e-3,
        ["k"] = 1e3,
        ["K"] = 1e3,    // bazen büyük K yazılır
        ["M"] = 1e6,
        ["G"] = 1e9,
    };

    private static readonly Dictionary<string, string> CurrencyMap = new()
    {
        ["€"] = "EUR", ["EUR"] = "EUR", ["EURO"] = "EUR",
        ["$"] = "USD", ["USD"] = "USD",
        ["₺"] = "TRY", ["TRY"] = "TRY", ["TL"] = "TRY",
        ["£"] = "GBP", ["GBP"] = "GBP",
    };

    // "0.27 pF" gibi bir metni kanonik SI sayısına çevirir; çeviremezse null.
    public double? NormalizeNumeric(string? raw, string decimalSeparator = ".")
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Excel kaçışı:  ="0.27 pF"  →  0.27 pF
        string s = raw.Trim();
        var excelMatch = Regex.Match(s, "^=\"(.*)\"$");
        if (excelMatch.Success)
            s = excelMatch.Groups[1].Value;

        // "0.27 pF" → sayı="0.27", ön ek+birim="pF"
        var match = Regex.Match(s, @"([-+]?[\d.,]+)\s*([a-zA-Zµμ]*)");
        if (!match.Success)
            return null;

        string numberPart = match.Groups[1].Value;
        string unitPart = match.Groups[2].Value;

        // ppm/ppb'yi burada yakalamazsak baştaki 'p' piko sanılıyor.
        if (unitPart.StartsWith("ppm", StringComparison.OrdinalIgnoreCase) ||
            unitPart.StartsWith("ppb", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseNumber(numberPart, decimalSeparator, out double ppValue))
                return null;

            double factor = unitPart.StartsWith("ppm", StringComparison.OrdinalIgnoreCase)
                ? 1e-6
                : 1e-9;
            return ppValue * factor;
        }

        double number;
        if (!TryParseNumber(numberPart, decimalSeparator, out number))
            return null;

        if (unitPart.Length > 0)
        {
            string prefix = unitPart.Substring(0, 1);

            // Önce birebir (M=mega, m=mili ayrımı bozulmasın), sonra harf varyantı.
            bool found = SiPrefixes.TryGetValue(prefix, out double multiplier)
                      || SiPrefixes.TryGetValue(prefix.ToLowerInvariant(), out multiplier)
                      || SiPrefixes.TryGetValue(prefix.ToUpperInvariant(), out multiplier);

            if (found)
                number *= multiplier;
        }

        return number;
    }

    // Hangi ayracın ondalık olduğunu değerin kendisine bakarak tespit eder.
    private static bool TryParseNumber(string text, string decimalSeparator, out double result)
    {
        text = text.Trim();

        bool hasDot = text.Contains('.');
        bool hasComma = text.Contains(',');

        if (hasDot && hasComma)
        {
            // İkisi de varsa sonda olan ondalıktır.
            int lastDot = text.LastIndexOf('.');
            int lastComma = text.LastIndexOf(',');
            if (lastComma > lastDot)
                text = text.Replace(".", "").Replace(",", "."); // virgül ondalık
            else
                text = text.Replace(",", "");                    // nokta ondalık
        }
        else if (hasComma)
        {
            // Virgülden sonra tam 3 basamak varsa binlik ayracıdır, ama "0,016" gibi
            // sıfırla başlayan sayı her zaman ondalıktır.
            int idx = text.LastIndexOf(',');
            int digitsAfter = text.Length - idx - 1;
            string before = text.Substring(0, idx);
            if (digitsAfter == 3 && before.Length > 0 && before[0] != '0')
                text = text.Replace(",", "");        // binlik → sil  (ör. "1,234" → 1234)
            else
                text = text.Replace(",", ".");       // ondalık → noktaya çevir (ör. "0,016" → 0.016)
        }
        else if (hasDot)
        {
            // Aynı kural nokta için: "10.000" binlik, "0.016" ondalık.
            int idx = text.LastIndexOf('.');
            int digitsAfter = text.Length - idx - 1;
            string before = text.Substring(0, idx);
            if (digitsAfter == 3 && before.Length > 0 && before[0] != '0')
                text = text.Replace(".", "");        // binlik → sil  (ör. "10.000" → 10000)
        }

        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    // "C0G, NP0" ve "C0G (NP0)" → "C0G/NP0"
    public string? NormalizeCategorical(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string s = raw.Trim();
        var excelMatch = Regex.Match(s, "^=\"(.*)\"$");
        if (excelMatch.Success)
            s = excelMatch.Groups[1].Value;

        s = s.Trim().ToUpperInvariant();

        // "-" gibi dolgular veri değil; uydurmak yerine null bırakıyoruz.
        if (s.Length == 0 || s == "-" || s == "N/A")
            return null;

        if (s.Contains("C0G") || s.Contains("NP0"))
            return "C0G/NP0";

        return s;
    }

    // "0,12 €" → "EUR". Bulamazsa null.
    public static string? ExtractCurrency(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var text = raw.ToUpperInvariant();
        foreach (var (token, iso) in CurrencyMap)
        {
            if (text.Contains(token)) return iso;
        }
        return null;
    }

    // NormalizeNumeric'in tersi: (2.7e-13, "F") → "0.27 pF", (16e6, "Hz") → "16 MHz"
    private static readonly (double Multiplier, string Prefix)[] FormatPrefixes =
    {
        (1e9, "G"), (1e6, "M"), (1e3, "k"),
        (1, ""),
        (1e-3, "m"), (1e-6, "µ"), (1e-9, "n"), (1e-12, "p"),
    };

    public static string FormatSi(double? value, string? unit)
    {
        if (value is null) return "";
        double v = value.Value;
        string u = unit ?? "";

        if (v == 0) return $"0 {u}".Trim();

        double abs = Math.Abs(v);

        foreach (var (multiplier, prefix) in FormatPrefixes)
        {
            if (abs >= multiplier)
                return $"{(v / multiplier).ToString("0.###", CultureInfo.InvariantCulture)} {prefix}{u}".Trim();
        }

        // Piko'dan da küçükse yine piko ile gösteriyoruz.
        return $"{(v / 1e-12).ToString("0.###", CultureInfo.InvariantCulture)} p{u}".Trim();
    }
}