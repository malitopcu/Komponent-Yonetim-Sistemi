using System.Globalization;
using System.Text.RegularExpressions;

namespace KomponentSistemi.Services;

public class ValueNormalizer
{
    // SI ön ekleri → çarpan. Farklı yazımları da ekliyoruz (µ, u, μ hepsi mikro)
    private static readonly Dictionary<string, double> SiPrefixes = new()
    {
        ["p"] = 1e-12,
        ["n"] = 1e-9,
        ["u"] = 1e-6,   // mikro (u olarak yazılmış)
        ["µ"] = 1e-6,   // mikro (µ sembolü — micro sign)
        ["μ"] = 1e-6,   // mikro (farklı Unicode)
        ["m"] = 1e-3,
        ["k"] = 1e3,
        ["K"] = 1e3,    // bazen büyük K yazılır
        ["M"] = 1e6,
        ["G"] = 1e9,
    };

    // Para birimi işareti/kodu → ISO 4217 kodu.
    private static readonly Dictionary<string, string> CurrencyMap = new()
    {
        ["€"] = "EUR", ["EUR"] = "EUR", ["EURO"] = "EUR",
        ["$"] = "USD", ["USD"] = "USD",
        ["₺"] = "TRY", ["TRY"] = "TRY", ["TL"] = "TRY",
        ["£"] = "GBP", ["GBP"] = "GBP",
    };

    // "0.27 pF" gibi bir metni kanonik sayıya çevirir (ör. Farad).
    // Çeviremezse null döndürür (karar: çökme, devam et).
    public double? NormalizeNumeric(string? raw, string decimalSeparator = ".")
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // 1) Excel kaçışını temizle:  ="0.27 pF"  →  0.27 pF
        string s = raw.Trim();
        var excelMatch = Regex.Match(s, "^=\"(.*)\"$");
        if (excelMatch.Success)
            s = excelMatch.Groups[1].Value;

        // 2) Sayı kısmı + ön ek + birim'i ayıkla
        //    Örn: "0.27 pF" → sayı="0.27", ön ek+birim="pF"
        var match = Regex.Match(s, @"([-+]?[\d.,]+)\s*([a-zA-Zµμ]*)");
        if (!match.Success)
            return null;

        string numberPart = match.Groups[1].Value;
        string unitPart = match.Groups[2].Value;

        // ppm/ppb istisnası: "±25ppm" → 25e-6, "±500ppb" → 500e-9
        // (yoksa ilk harf 'p' piko sanılır!)
        if (unitPart.StartsWith("ppm", StringComparison.OrdinalIgnoreCase) ||
            unitPart.StartsWith("ppb", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseNumber(numberPart, decimalSeparator, out double ppValue))
                return null;

            double factor = unitPart.StartsWith("ppm", StringComparison.OrdinalIgnoreCase)
                ? 1e-6    // parts per million
                : 1e-9;   // parts per billion
            return ppValue * factor;
        }

        // 3) Ondalık ayracına göre sayıyı çöz
        double number;
        if (!TryParseNumber(numberPart, decimalSeparator, out number))
            return null;

        // 4) Ön ek varsa çarpanı uygula
        if (unitPart.Length > 0)
        {
            string prefix = unitPart.Substring(0, 1); // ilk harf ön ek adayı

            // Önce birebir ara (M=mega, m=mili ayrımı korunur);
            // bulunamazsa küçük/büyük harf varyantını dene (P→p, g→G)
            bool found = SiPrefixes.TryGetValue(prefix, out double multiplier)
                      || SiPrefixes.TryGetValue(prefix.ToLowerInvariant(), out multiplier)
                      || SiPrefixes.TryGetValue(prefix.ToUpperInvariant(), out multiplier);

            if (found)
                number *= multiplier;
        }

        return number;
    }

    // Sayıyı akıllıca çözer: hangi ayracın ondalık olduğunu değere bakarak tespit eder.
    private static bool TryParseNumber(string text, string decimalSeparator, out double result)
    {
        text = text.Trim();

        bool hasDot = text.Contains('.');
        bool hasComma = text.Contains(',');

        if (hasDot && hasComma)
        {
            // İkisi de var: sonda olan ondalıktır, öteki binliktir
            int lastDot = text.LastIndexOf('.');
            int lastComma = text.LastIndexOf(',');
            if (lastComma > lastDot)
                text = text.Replace(".", "").Replace(",", "."); // virgül ondalık
            else
                text = text.Replace(",", "");                    // nokta ondalık
        }
        else if (hasComma)
        {
            // Sadece virgül var: sonrasında tam 3 basamak varsa binlik, değilse ondalık
            int idx = text.LastIndexOf(',');
            int digitsAfter = text.Length - idx - 1;
            if (digitsAfter == 3)
                text = text.Replace(",", "");        // binlik → sil
            else
                text = text.Replace(",", ".");       // ondalık → noktaya çevir
        }
        else if (hasDot)
        {
            // Sadece nokta var: sonrasında tam 3 basamak varsa binlik, değilse ondalık
            int idx = text.LastIndexOf('.');
            int digitsAfter = text.Length - idx - 1;
            if (digitsAfter == 3)
                text = text.Replace(".", "");        // binlik → sil
            // değilse dokunma, zaten nokta ondalık
        }

        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    // Kategorik değeri kanonik metne çevirir.
    // Örn: "C0G, NP0" ve "C0G (NP0)" → "C0G/NP0"
    public string? NormalizeCategorical(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Excel kaçışını temizle (kategorik değerler de sarılı gelebilir)
        string s = raw.Trim();
        var excelMatch = Regex.Match(s, "^=\"(.*)\"$");
        if (excelMatch.Success)
            s = excelMatch.Groups[1].Value;

        // Büyük harfe çevir, fazla boşlukları sadeleştir
        s = s.Trim().ToUpperInvariant();

        // "-", "" gibi dolgu değerler = "veri yok" → null döndür (DB'ye şive sokma)
        if (s.Length == 0 || s == "-" || s == "N/A")
            return null;

        // C0G ile NP0 aynı dielektrik → tek kanona indir
        if (s.Contains("C0G") || s.Contains("NP0"))
            return "C0G/NP0";

        return s;
    }

    // Ham metinde para birimi işareti/kodu arar (örn: "0,12 €" → "EUR").
    // Bulursa ISO 4217 kodu, bulamazsa null döndürür.
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
}