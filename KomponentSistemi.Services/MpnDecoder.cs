using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using KomponentSistemi.Data;

namespace KomponentSistemi.Services;

public class DecodedMpn
{
    public double Ohms { get; init; }
    public double? TolerancePercent { get; init; }
    public int? TcrPpm { get; init; }
    public double? PowerW { get; init; }
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

        double? ohms = DecodeValue(g.Groups["value"].Value, p.ValueEncoding);
        if (ohms is null) return null;

        var tolMap = ReadMap<double>(p.ToleranceMapJson);
        var tcrMap = ReadMap<int>(p.TcrMapJson);
        var powMap = ReadMap<double>(p.PowerMapJson);
        var pkgMap = ReadMap<string>(p.PackageMapJson);

        return new DecodedMpn
        {
            Ohms = ohms.Value,
            TolerancePercent = Lookup(tolMap, g.Groups["tol"]),
            TcrPpm = Lookup(tcrMap, g.Groups["tcr"]),
            PowerW = Lookup(powMap, g.Groups["power"]),
            Package = g.Groups["size"].Success && pkgMap.TryGetValue(g.Groups["size"].Value, out var pkg) ? pkg : null,
            Manufacturer = p.Manufacturer,
            ProfileName = p.Name,
        };
    }

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
