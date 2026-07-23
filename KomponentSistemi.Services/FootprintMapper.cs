using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KomponentSistemi.Services;

// Komponentin paket bilgisinden KiCad footprint adını üretir.
// Tanıyamadığı paket için null döner; tahmin yürütmez.
public static class FootprintMapper
{
    private static readonly Dictionary<string, string> Imp2Met = new()
    {
        ["01005"] = "0402", ["0201"] = "0603", ["0402"] = "1005", ["0603"] = "1608",
        ["0805"] = "2012", ["1206"] = "3216", ["1210"] = "3225", ["2010"] = "5025",
        ["2512"] = "6332",
    };

    private static readonly Dictionary<string, string> Met2Imp = new()
    {
        ["1005"] = "0402", ["1608"] = "0603", ["2012"] = "0805",
        ["3216"] = "1206", ["3225"] = "1210",
    };

    public static string? Resolve(int componentTypeId, string? paramsJson, double? primary = null, double? secondary = null)
    {
        var pr = ParseParams(paramsJson);
        string? package = Val(pr, "package");
        string? subtype = Val(pr, "subtype");
        string pkgUp = (package ?? "").ToUpperInvariant();

        switch (componentTypeId)
        {
            case 1: return Passive(package, "C", "Capacitor_SMD") ?? CapTht(pkgUp);   // kondansatör
            case 2: return Passive(package, "R", "Resistor_SMD") ?? ResTht(pkgUp);    // direnç
            case 3:                                                                   // diyot
                if (string.Equals(subtype, "LED", StringComparison.OrdinalIgnoreCase))
                    return Led(package);
                return MatchTokens(pkgUp, DiodeTable);
            case 4: return MatchTokens(pkgUp, ToSotTable);                            // transistör
            case 5: return Oscillator(Val(pr, "size"));                               // osilatör
            case 6: return MatchTokens(pkgUp, ToSotTable);                            // regülatör
            case 7: return Connector(Val(pr, "pitch"), primary, Val(pr, "mounting")); // konnektör
            default: return null;
        }
    }

    private static Dictionary<string, string> ParseParams(string? json)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return d;

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var m in doc.RootElement.EnumerateObject())
                d[m.Name] = m.Value.ValueKind == JsonValueKind.String ? (m.Value.GetString() ?? "") : m.Value.ToString();
        }
        catch
        {
            return d;
        }

        return d;
    }

    private static string? Val(Dictionary<string, string> d, string key) => d.TryGetValue(key, out var v) ? v : null;

    // "0402 (1005 METRIC)" gibi metinlerin başındaki emperyal kodu alır.
    private static string? LeadCode(string? pkg)
    {
        var m = Regex.Match(pkg ?? "", @"^\s*(\d{4,5})");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? Passive(string? pkg, string letter, string lib)
    {
        var code = LeadCode(pkg);
        return code != null && Imp2Met.TryGetValue(code, out var met)
            ? $"{lib}:{letter}_{code}_{met}Metric"
            : null;
    }

    // "AXIAL" / "RADIAL" ölçü vermediği için yaygın land pattern'e düşüyoruz.
    private static string? ResTht(string p) =>
        p.Contains("AXIAL") ? "Resistor_THT:R_Axial_DIN0207_L6.3mm_D2.5mm_P7.62mm_Horizontal" : null;

    private static string? CapTht(string p) =>
        p.Contains("RADIAL") || p.Contains("DISC") ? "Capacitor_THT:C_Disc_D7.5mm_W5.0mm_P5.00mm" : null;

    private static string? Led(string? pkg)
    {
        var code = LeadCode(pkg);
        if (code == null) return null;

        if (Imp2Met.TryGetValue(code, out var met) &&
            code is "0201" or "0402" or "0603" or "0805" or "1206" or "1210")
            return $"LED_SMD:LED_{code}_{met}Metric";

        if (Met2Imp.TryGetValue(code, out var imp))
            return $"LED_SMD:LED_{imp}_{code}Metric";

        // 1204 ve 1208 KiCad'de yok; boyu aynı olan 1206'ya düşürüyoruz.
        if (code is "1204" or "1208")
            return "LED_SMD:LED_1206_3216Metric";

        return null;
    }

    // Osilatörde paket alanı boyut vermiyor, ölçü "Size / Dimension" metninden okunuyor.
    private static string? Oscillator(string? size)
    {
        if (string.IsNullOrWhiteSpace(size)) return null;

        var m = Regex.Match(size, @"([\d.]+)\s*mm\s*x\s*([\d.]+)\s*mm", RegexOptions.IgnoreCase);
        if (!m.Success) return null;

        return OscMap.GetValueOrDefault($"{Norm1(m.Groups[1].Value)}x{Norm1(m.Groups[2].Value)}");
    }

    private static string Norm1(string num) =>
        double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d.ToString("0.0", CultureInfo.InvariantCulture)
            : num;

    // 2.0x1.6mm için KiCad'de 4 pinli osilatör footprint'i yok, o boyut listede yer almıyor.
    private static readonly Dictionary<string, string> OscMap = new()
    {
        ["3.2x2.5"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG8002CE-4Pin_3.2x2.5mm",
        ["2.5x2.0"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG210-4Pin_2.5x2.0mm",
        ["1.6x1.2"] = "Oscillator:Oscillator_SMD_Abracon_ASCO-4Pin_1.6x1.2mm",
        ["5.0x3.2"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG8002LB-4Pin_5.0x3.2mm",
        ["7.0x5.0"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG8002CA-4Pin_7.0x5.0mm",
    };

    // Klemenste footprint'i pitch ve pozisyon sayısı belirler, paket kodu değil.
    private static string? Connector(string? pitch, double? positions, string? mounting)
    {
        if (positions is null) return null;

        int n = (int)Math.Round(positions.Value);
        if (n < 2) return null;

        // SMD klemensler üreticiye özel, sadece delikli olanları eşliyoruz.
        if (mounting is not null && !mounting.ToUpperInvariant().Contains("THROUGH")) return null;

        double mm = PitchMm(pitch);
        if (mm is >= 4.9 and <= 5.2)
            return $"TerminalBlock:TerminalBlock_MaiXu_MX126-5.0-{n:D2}P_1x{n:D2}_P5.00mm";

        return null;
    }

    private static double PitchMm(string? pitch)
    {
        if (string.IsNullOrWhiteSpace(pitch)) return 0;

        var m = Regex.Match(pitch, @"([\d.]+)\s*mm", RegexOptions.IgnoreCase);
        return m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d : 0;
    }

    // Tablodaki ilk eşleşen grup kazanır. fp değeri null olan satırlar,
    // belirsiz varyantın daha genel bir token'a düşmesini engellemek için var.
    private static string? MatchTokens(string p, (string[] tokens, string? fp)[] table)
    {
        foreach (var (tokens, fp) in table)
            foreach (var t in tokens)
                if (p.Contains(t))
                    return fp;

        return null;
    }

    // Sıra önemli: özel varyantlar genel olanlardan önce gelmeli.
    private static readonly (string[] tokens, string? fp)[] DiodeTable =
    {
        (new[] { "SOD-123F" },                         "Diode_SMD:D_SOD-123F"),
        (new[] { "SOD-123-2", "SOD-123" },             "Diode_SMD:D_SOD-123"),
        (new[] { "SOD-323F" },                         "Diode_SMD:D_SOD-323F"),
        (new[] { "SOD-323HE", "SOD-323" },             "Diode_SMD:D_SOD-323"),
        (new[] { "SOD-523F", "SOD-523" },              "Diode_SMD:D_SOD-523"),
        (new[] { "SOD-128" },                          "Diode_SMD:D_SOD-128"),
        (new[] { "MINI-MELF", "SOD-80", "DO-213AC" },  "Diode_SMD:D_MiniMELF"),
        (new[] { "SMA", "DO-214AC", "DO-221AC" },      "Diode_SMD:D_SMA"),
        (new[] { "SMB", "DO-214AA" },                  "Diode_SMD:D_SMB"),
        (new[] { "SMC", "DO-214AB" },                  "Diode_SMD:D_SMC"),
        (new[] { "SOT-323", "SC-70" },                 "Package_TO_SOT_SMD:SOT-323"),
        (new[] { "SOT-523" },                          "Package_TO_SOT_SMD:SOT-523"),
        (new[] { "SOT-23", "TO-236", "SC-59" },        "Package_TO_SOT_SMD:SOT-23"),
        (new[] { "DO-201" },                           "Diode_THT:D_DO-201AD_P15.24mm_Horizontal"),
        (new[] { "DO-41", "DO-204AC", "DO-204AL" },    "Diode_THT:D_DO-41_SOD81_P10.16mm_Horizontal"),
        (new[] { "DO-35", "DO-204AH" },                "Diode_THT:D_DO-35_SOD27_P10.16mm_Horizontal"),
        (new[] { "TO-247" },                           "Package_TO_SOT_THT:TO-247-3_Vertical"),
        (new[] { "TO-220" },                           "Package_TO_SOT_THT:TO-220-3_Vertical"),
    };

    private static readonly (string[] tokens, string? fp)[] ToSotTable =
    {
        (new[] { "TO-92S" },                           "Package_TO_SOT_THT:TO-92S"),
        (new[] { "TO-92L", "TO-92" },                  "Package_TO_SOT_THT:TO-92_Inline"),
        (new[] { "SOT-23-5" },                         "Package_TO_SOT_SMD:SOT-23-5"),
        (new[] { "SOT-23" },                           "Package_TO_SOT_SMD:SOT-23"),
        (new[] { "SOT-323" },                          "Package_TO_SOT_SMD:SOT-323"),
        (new[] { "SOT-553" },                          "Package_TO_SOT_SMD:SOT-553"),
        (new[] { "SOT-223" },                          "Package_TO_SOT_SMD:SOT-223"),
        (new[] { "TO-220-5" },                         null),
        (new[] { "TO-220" },                           "Package_TO_SOT_THT:TO-220-3_Vertical"),
        (new[] { "TO-3P" },                            "Package_TO_SOT_THT:TO-3P-3_Vertical"),
        (new[] { "TO-247" },                           "Package_TO_SOT_THT:TO-247-3_Vertical"),
        (new[] { "TO-36", "TO-46", "TO-71", "TO-72" }, null),
        (new[] { "TO-3" },                             "Package_TO_SOT_THT:TO-3"),
        (new[] { "D2PAK", "TO-263" },                  "Package_TO_SOT_SMD:TO-263-2"),
    };
}
