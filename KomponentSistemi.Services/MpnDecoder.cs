using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using KomponentSistemi.Data;

namespace KomponentSistemi.Services;

public class DecodedMpn
{
    public int ComponentTypeId { get; init; }
    // Birincil/ikincil değer, tipin SI temel biriminde:
    //   Direnç Ω/W · Kondansatör F/V · Regülatör V · Osilatör Hz · Konnektör adet.
    public double? PrimaryValueSi { get; init; }
    public double? SecondaryValueSi { get; init; }
    public double? TolerancePercent { get; init; }
    public int? TcrPpm { get; init; }           // direnç
    public string? Dielectric { get; init; }    // kondansatör
    public string? Package { get; init; }
    public string Manufacturer { get; init; } = "";
    public string ProfileName { get; init; } = "";
}

// MpnProfile verisini (isimli gruplu regex + kodlama kuralı) uygulayan jenerik
// çözücü; üretici bilgisi içermez. Hiçbir profil eşleşmezse null: değer
// uydurulmaz, evrensel önerici (SuggestValues) ya da kullanıcı devreye girer.
public static class MpnDecoder
{
    public static DecodedMpn? TryDecode(string? mpn, IEnumerable<MpnProfile> profiles)
    {
        if (string.IsNullOrWhiteSpace(mpn)) return null;
        string m = mpn.Trim().ToUpperInvariant();

        // Sıra belirlenimci olmalı: dar desenler önce, gevşek desenler (ör. ERJ) sonda.
        // Profil Id'leri bu sırayla verildi; DB'nin satır sırasına güvenilmez.
        foreach (var p in profiles.OrderBy(p => p.Id))
        {
            var result = TryDecodeWith(m, p);
            if (result is not null) return result;
        }
        return null;
    }

    private static DecodedMpn? TryDecodeWith(string mpn, MpnProfile p)
    {
        if (string.IsNullOrWhiteSpace(p.PatternRegex)) return null;

        Match g;
        try { g = Regex.Match(mpn, p.PatternRegex); }
        catch (ArgumentException) { return null; } // bozuk profil deseni motoru düşürmesin

        if (!g.Success) return null;

        double? value = DecodeValue(g.Groups["value"].Value, p.ValueEncoding);
        if (value is null) return null;

        var tolMap = ReadMap<double>(p.ToleranceMapJson);
        var pkgMap = ReadMap<string>(p.PackageMapJson);
        string? pkg = g.Groups["size"].Success && pkgMap.TryGetValue(g.Groups["size"].Value, out var pk) ? pk : null;

        // Birincil değer: kondansatörde "value" pikofarad → Farad; diğer tiplerde
        // value zaten SI (literal kodlama ya da Ω hane kodu).
        double primary = p.ComponentTypeId == 1 ? value.Value * 1e-12 : value.Value;

        double? secondary = null;
        string? diel = null;
        int? tcr = null;

        if (p.ComponentTypeId == 1) // Kondansatör: ikincil = gerilim, + dielektrik
        {
            var voltMap = ReadMap<double>(p.VoltageMapJson);
            string vCode = g.Groups["volt"].Value;
            secondary = voltMap.Count > 0
                ? (voltMap.TryGetValue(vCode, out var vm) ? vm : (double?)null)
                : (EiaVoltage.TryGetValue(vCode, out var ev) ? ev : (double?)null);

            var dielMap = ReadMap<string>(p.DielectricMapJson);
            string dCode = g.Groups["diel"].Value;
            diel = dCode.Length == 0 ? null
                : dielMap.Count > 0 ? (dielMap.TryGetValue(dCode, out var dm) ? dm : dCode)
                : dCode; // harita yoksa kod zaten açık ad (TDK: "X7R")
        }
        else if (p.ComponentTypeId == 2) // Direnç: ikincil = güç, + TCR
        {
            secondary = Lookup(ReadMap<double>(p.PowerMapJson), g.Groups["power"]);
            tcr = Lookup(ReadMap<int>(p.TcrMapJson), g.Groups["tcr"]);
        }
        // Diğer tipler (regülatör/osilatör/konnektör): sadece birincil + tolerans + paket.

        return new DecodedMpn
        {
            ComponentTypeId = p.ComponentTypeId,
            PrimaryValueSi = primary,
            SecondaryValueSi = secondary,
            Dielectric = diel,
            TcrPpm = tcr,
            TolerancePercent = Lookup(tolMap, g.Groups["tol"]),
            Package = pkg,
            Manufacturer = p.Manufacturer,
            ProfileName = p.Name,
        };
    }

    // EIA standart gerilim kodları (MLCC); üretici-bağımsız, motora gömülü.
    private static readonly Dictionary<string, double> EiaVoltage = new()
    {
        ["0G"] = 4, ["0J"] = 6.3, ["1A"] = 10, ["1C"] = 16, ["1D"] = 20, ["1E"] = 25,
        ["1V"] = 35, ["1H"] = 50, ["1J"] = 63, ["2A"] = 100, ["2D"] = 200,
        ["2E"] = 250, ["2W"] = 450, ["2H"] = 500,
    };

    // Profil yokken MPN içinde standart değer kalıpları arar (öneri, kesin değil).
    // Kurallar biçime dayalı: IEC 60062 R/K/M ve anlamlı-hane+sıfır kodları.
    public static List<(double Ohms, string Reason)> SuggestValues(string? mpn)
    {
        var result = new List<(double, string)>();
        if (string.IsNullOrWhiteSpace(mpn)) return result;
        string m = mpn.Trim().ToUpperInvariant();

        foreach (Match g in Regex.Matches(m, @"(?<![0-9])(\d{1,4}[RKM]\d{0,3})(?![0-9RKM])"))
        {
            double? v = DecodeValue(g.Groups[1].Value, "rkm");
            if (v is not null) result.Add((v.Value, "RKM " + g.Groups[1].Value));
        }

        // R/K/M ile biten hane dizileri RKM belirtecinin parçasıdır (100R'daki "100"),
        // ayrıca hane kodu sayılmaz — yoksa aynı MPN'den iki çelişen aday çıkar.
        foreach (Match g in Regex.Matches(m, @"(?<![0-9R])(\d{3,5})(?![0-9RKM])"))
        {
            string t = g.Groups[1].Value;
            // Paket boyu kodları (0603, 0402...) sıfırla başlar; değer kodları başlamaz.
            if (t.StartsWith('0') || PackageCodes.Contains(t)) continue;
            double? v = DecodeValue(t, "sig-zeros-R");
            if (v is not null) result.Add((v.Value, "hane kodu " + t));
        }

        // Fiziksel olarak anlamsız adayları ele (1 mΩ altı, 100 GΩ üstü).
        return result.Where(x => x.Item1 is >= 0.001 and <= 1e11).Distinct().ToList();
    }

    private static readonly HashSet<string> PackageCodes = new()
        { "0075", "01005", "0201", "0402", "0603", "0805", "1206", "1210", "1218", "2010", "2512" };

    // Kodlama kuralları biçime göre adlandırılır, üreticiye göre değil;
    // yeni biçim gerektiğinde buraya yeni bir ad eklenir, profiller o adı kullanır.
    private static double? DecodeValue(string value, string encoding)
    {
        string t = value.ToUpperInvariant();
        switch (encoding)
        {
            // R = ondalık nokta (10R00 -> 10.00). R yoksa son hane sıfır sayısı,
            // öncekiler anlamlı hane (16900 -> 1690, 1002 -> 10000).
            case "sig-zeros-R":
                if (t.Contains('R'))
                    return ParseOrNull(t.Replace('R', '.'));
                if (t.Length >= 3 && t.All(char.IsDigit))
                    return ParseOrNull(t[..^1]) * Math.Pow(10, t[^1] - '0');
                return null;

            // IEC 60062 R/K/M: harf hem çarpan hem ondalık nokta (2K2 -> 2200,
            // 1R0 -> 1.0, 10K -> 10000, R10 -> 0.10); düz sayı doğrudan ohm (250).
            case "rkm":
                var m = Regex.Match(t, @"^(\d+)([RKM])(\d*)$");
                if (m.Success)
                {
                    double mult = m.Groups[2].Value switch { "K" => 1e3, "M" => 1e6, _ => 1 };
                    string frac = m.Groups[3].Value;
                    double f = frac.Length > 0
                        ? double.Parse(frac, CultureInfo.InvariantCulture) / Math.Pow(10, frac.Length)
                        : 0;
                    return (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) + f) * mult;
                }
                if (Regex.IsMatch(t, @"^R\d+$"))
                    return ParseOrNull("0." + t[1..]);
                if (t.Length > 0 && t.All(char.IsDigit))
                    return ParseOrNull(t);
                return null;

            // Değer numarada açık yazılı: "3.3", "05", "16.000MHZ", "4". Nokta HER ZAMAN
            // ondalık (NormalizeNumeric'in binlik-ayracı sezgisi burada yanlış olur:
            // "16.000MHZ" 16 MHz'dir, 16000 değil). Ön ek varsa SI çarpanı uygulanır.
            case "literal":
                var lm = Regex.Match(t, @"^([-+]?\d+(?:\.\d+)?)\s*([A-ZΜ]*)$");
                if (!lm.Success) return null;
                if (!double.TryParse(lm.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lnum))
                    return null;
                string lunit = lm.Groups[2].Value;
                if (lunit.Length > 0)
                    lnum *= lunit[0] switch
                    {
                        'G' => 1e9, 'M' => 1e6, 'K' => 1e3,
                        'U' or 'Μ' => 1e-6, 'N' => 1e-9, 'P' => 1e-12,
                        _ => 1,
                    };
                return lnum;

            default:
                return null;
        }
    }

    private static Dictionary<string, T> ReadMap<T>(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, T>>(json) ?? new(); }
        catch (JsonException) { return new(); }
    }

    private static T? Lookup<T>(Dictionary<string, T> map, Group g) where T : struct
        => g.Success && map.TryGetValue(g.Value, out var v) ? v : null;

    private static double? ParseOrNull(string s)
        => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
}
