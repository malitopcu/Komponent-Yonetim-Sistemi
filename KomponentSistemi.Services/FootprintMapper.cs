using System.Globalization;
using System.Text.RegularExpressions;

namespace KomponentSistemi.Services;

// ============================================================================
//  Paket → KiCad footprint eşleyici (Adım 3). SAF/yan-etkisiz, DB'ye dokunmaz.
// ============================================================================
//  Girdi: komponentin tipi + ham "package" metni (+ diyotlarda subtype).
//  Çıktı: KiCad footprint TAM adı ("Kütüphane:Footprint") ya da null.
//
//  Felsefe (null felsefesi): paketi TANIYAMAZSAK ya da boyut belli değilse
//  → null döneriz ("footprint yok" diye raporlanır). ASLA uydurmayız.
//
//  Neden token arıyoruz: distribütör paket metinleri dağınık ve çok-kodlu —
//  "0402 (1005 METRIC)", "DO-204AH, DO-35, AXIAL", "TO-236-3, SC-59, SOT-23-3".
//  Normalize edip (büyük harf) içinde bildiğimiz kodu ararız.
//
//  Footprint adları KiCad standart kütüphanesine göredir (v9/v10 ortak) ve
//  web'den tam yazımı doğrulandı. KiCad bir adı bulamazsa yalnızca o ad düzeltilir.
// ============================================================================
public static class FootprintMapper
{
    // Emperyal (inch) kod → metrik kod. Pasif çip footprint adı ikisini de içerir.
    private static readonly Dictionary<string, string> Imp2Met = new()
    {
        ["01005"] = "0402", ["0201"] = "0603", ["0402"] = "1005", ["0603"] = "1608",
        ["0805"] = "2012", ["1206"] = "3216", ["1210"] = "3225", ["2010"] = "5025",
        ["2512"] = "6332",
    };

    // Metrik kod → emperyal (LED paketleri bazen metrik yazılır).
    private static readonly Dictionary<string, string> Met2Imp = new()
    {
        ["1005"] = "0402", ["1608"] = "0603", ["2012"] = "0805",
        ["3216"] = "1206", ["3225"] = "1210",
    };

    // ANA GİRİŞ: tip + paket (+ subtype, + osilatörde size) → footprint ya da null.
    public static string? Resolve(int componentTypeId, string? package, string? subtype, string? size = null)
    {
        string p = (package ?? "").ToUpperInvariant();
        switch (componentTypeId)
        {
            case 1: return Passive(package, "C", "Capacitor_SMD") ?? CapTht(p);   // Kondansatör (SMD→THT)
            case 2: return Passive(package, "R", "Resistor_SMD") ?? ResTht(p);    // Direnç (SMD→THT)
            case 3:                                                   // Diyot
                if (string.Equals(subtype, "LED", StringComparison.OrdinalIgnoreCase))
                    return Led(package);
                return MatchTokens(p, DiodeTable);
            case 4: return MatchTokens(p, ToSotTable);               // Transistör
            case 6: return MatchTokens(p, ToSotTable);               // Regülatör
            case 5: return Oscillator(size);   // Osilatör: paket boyutsuz → Size/Dimension'dan
            default: return null;
        }
    }

    // Baştaki emperyal kodu yakalar: "0402 (1005 METRIC)" → "0402", "01005 ..." → "01005".
    private static string? LeadCode(string? pkg)
    {
        var m = Regex.Match(pkg ?? "", @"^\s*(\d{4,5})");
        return m.Success ? m.Groups[1].Value : null;
    }

    // Pasif çip (direnç/kondansatör): "Resistor_SMD:R_0402_1005Metric" gibi.
    private static string? Passive(string? pkg, string letter, string lib)
    {
        var code = LeadCode(pkg);
        return code != null && Imp2Met.TryGetValue(code, out var met)
            ? $"{lib}:{letter}_{code}_{met}Metric"
            : null;
    }

    // THT pasifler: paket ("AXIAL"/"RADIAL") ölçü/pitch vermez → yaygın VARSAYILAN
    // land pattern (1/4W eksenel direnç, 5mm disk kondansatör). KiCad'de değişebilir.
    private static string? ResTht(string p) =>
        p.Contains("AXIAL") ? "Resistor_THT:R_Axial_DIN0207_L6.3mm_D2.5mm_P7.62mm_Horizontal" : null;

    private static string? CapTht(string p) =>
        (p.Contains("RADIAL") || p.Contains("DISC")) ? "Capacitor_THT:C_Disc_D7.5mm_W5.0mm_P5.00mm" : null;

    // LED (Diyot + subtype=LED): "LED_SMD:LED_1206_3216Metric" gibi.
    private static string? Led(string? pkg)
    {
        var code = LeadCode(pkg);
        if (code == null) return null;
        if (Imp2Met.TryGetValue(code, out var met) &&
            code is "0201" or "0402" or "0603" or "0805" or "1206" or "1210")
            return $"LED_SMD:LED_{code}_{met}Metric";
        if (Met2Imp.TryGetValue(code, out var imp))
            return $"LED_SMD:LED_{imp}_{code}Metric";
        if (code is "1204" or "1208")                 // KiCad'de yok → en yakın 1206 (3.2mm boy aynı)
            return "LED_SMD:LED_1206_3216Metric";
        return null;
    }

    // Osilatör: paket ("4-SMD, No Lead") boyutsuz olduğu için DigiKey "Size / Dimension"
    // metninden (ör. '0.126" L x 0.098" W (3.20mm x 2.50mm)') boyutu çıkarıp eşleriz.
    // KiCad'in o boyutta 4-pin SMD footprint'i yoksa (ör. 2.0x1.6mm) → null (dürüst).
    private static string? Oscillator(string? size)
    {
        if (string.IsNullOrWhiteSpace(size)) return null;
        var m = Regex.Match(size, @"([\d.]+)\s*mm\s*x\s*([\d.]+)\s*mm", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        string key = $"{Norm1(m.Groups[1].Value)}x{Norm1(m.Groups[2].Value)}";
        return OscMap.GetValueOrDefault(key);
    }

    // "3.20" → "3.2", "2.00" → "2.0" (tek ondalık, tutarlı anahtar için).
    private static string Norm1(string num) =>
        double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d.ToString("0.0", CultureInfo.InvariantCulture)
            : num;

    // Boyut (GxY mm) → KiCad standart osilatör footprint'i. Aynı boyuttaki bir
    // temsilci footprint seçilir (pad geometrisi boyutla belirlenir).
    private static readonly Dictionary<string, string> OscMap = new()
    {
        ["3.2x2.5"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG8002CE-4Pin_3.2x2.5mm",
        ["2.5x2.0"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG210-4Pin_2.5x2.0mm",
        ["1.6x1.2"] = "Oscillator:Oscillator_SMD_Abracon_ASCO-4Pin_1.6x1.2mm",
        ["5.0x3.2"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG8002LB-4Pin_5.0x3.2mm",
        ["7.0x5.0"] = "Oscillator:Oscillator_SMD_SeikoEpson_SG8002CA-4Pin_7.0x5.0mm",
        // 2.0x1.6mm: KiCad'de 4-pin osilatör footprint'i YOK → eşleşmez (null).
    };

    // Öncelik SIRALI token tablosu: pakette geçen ilk grup kazanır.
    // fp == null olan giriş: "tanı ama footprint atama" — belirsiz/çok-bacaklı
    // varyantın (TO-220-5 gibi) daha geniş bir token'a (TO-220) düşmesini engeller.
    private static string? MatchTokens(string p, (string[] tokens, string? fp)[] table)
    {
        foreach (var (tokens, fp) in table)
            foreach (var t in tokens)
                if (p.Contains(t))
                    return fp;
        return null;
    }

    // SIRA ÖNEMLİ: özel varyant önce (SOD-123F, SOD-123'ten önce; SOT-323, SOT-23'ten önce).
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
        (new[] { "TO-220-5" },                         null),   // 5-bacak varyant belirsiz → null
        (new[] { "TO-220" },                           "Package_TO_SOT_THT:TO-220-3_Vertical"),
        (new[] { "TO-3P" },                            "Package_TO_SOT_THT:TO-3P-3_Vertical"),
        (new[] { "TO-247" },                           "Package_TO_SOT_THT:TO-247-3_Vertical"),
        (new[] { "TO-36", "TO-46", "TO-71", "TO-72" }, null),   // metal kutu nadir → null (TO-3'e karışmasın)
        (new[] { "TO-3" },                             "Package_TO_SOT_THT:TO-3"),
        (new[] { "D2PAK", "TO-263" },                  "Package_TO_SOT_SMD:TO-263-2"),
    };
}
