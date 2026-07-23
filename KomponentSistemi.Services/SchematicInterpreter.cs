using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace KomponentSistemi.Services;

public enum SchematicValueKind
{
    Numeric,      // 100n, 1k, 5k → SI değerine çevrilir
    PartNumber,   // 1N4007, LM317 → MPN olarak aranır
    None          // boş, "~" veya jenerik sembol ekosu ("LED", "C")
}

// Şemadaki bir sembolün yorumu: hangi tip, hangi alt tür, değeri ne ifade ediyor.
// Henüz veritabanına bakmıyoruz; sadece lib_id ve Value metnini çözüyoruz.
public class SchematicSymbolInfo
{
    public string Reference { get; init; } = "";
    public string LibId { get; init; } = "";
    public string RawValue { get; init; } = "";

    public int? ComponentTypeId { get; init; }
    public string? Subtype { get; init; }

    public SchematicValueKind ValueKind { get; init; }
    public double? NumericSi { get; init; }
    public string? PartText { get; init; }

    // Konnektörlerde sembol adından çıkarılan giriş (pozisyon) sayısı: Screw_Terminal_01x02 → 2.
    public int? ExpectedPositions { get; init; }
}

// Şema sembollerini bizim tip/değer dünyamıza çeviren saf mantık.
// Belirli bir dosyaya değil, yaygın KiCad kütüphane adlarına göre kurallı.
public static class SchematicInterpreter
{
    // Sembol adlarının yer tutucu olduğu kütüphaneler. Buralarda Value alanı bilgiyi
    // taşır; specific kütüphanelerde (Regulator_Linear, Transistor_BJT...) sembol adının
    // kendisi parça numarasıdır.
    private static readonly HashSet<string> GenericNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "Device", "Connector", "Connector_Generic", "power", "Switch", "Mechanical"
    };

    public static List<SchematicSymbolInfo> InterpretFile(string schematicPath, out List<string> errors)
    {
        var read = new KiCadSchematicReader().Read(schematicPath);
        errors = read.Errors;

        var rows = new List<SchematicSymbolInfo>();
        foreach (var sym in read.Symbols)
        {
            if (sym.IsPowerOrVirtual) continue;
            rows.Add(Interpret(sym));
        }

        return rows;
    }

    public static SchematicSymbolInfo Interpret(KiCadSymbol sym)
    {
        var (typeId, subtype) = ResolveType(sym.LibId);
        var (kind, si, part) = InterpretValue(sym.Value, sym.LibId);
        int? positions = typeId == 7 ? ExtractConnectorPositions(sym.LibId) : null;

        return new SchematicSymbolInfo
        {
            Reference = sym.Reference,
            LibId = sym.LibId,
            RawValue = sym.Value,
            ComponentTypeId = typeId,
            Subtype = subtype,
            ValueKind = kind,
            NumericSi = si,
            PartText = part,
            ExpectedPositions = positions
        };
    }

    // Konnektör sembol adı giriş sayısını "AxB" olarak taşır: Screw_Terminal_01x02 → 1×2 = 2.
    private static readonly Regex ConnGrid = new(@"(\d+)\s*[xX]\s*(\d+)");

    private static int? ExtractConnectorPositions(string libId)
    {
        SplitLibId(libId, out _, out string name);
        var m = ConnGrid.Match(name);
        if (!m.Success) return null;

        int rows = int.Parse(m.Groups[1].Value);
        int perRow = int.Parse(m.Groups[2].Value);
        int total = rows * perRow;
        return total > 0 ? total : null;
    }

    // lib_id "Kütüphane:Sembol" → (tip id, alt tür). Tanımadığımızda tip null.
    public static (int? typeId, string? subtype) ResolveType(string libId)
    {
        SplitLibId(libId, out string ns, out string name);
        string n = name.ToUpperInvariant();

        if (ns.Equals("Device", StringComparison.OrdinalIgnoreCase))
        {
            if (n == "C" || n.StartsWith("C_") || n.StartsWith("CP")) return (1, null);

            if (n == "R" || n.StartsWith("R_") || n == "POTENTIOMETER")
            {
                bool pot = n.Contains("POTENTIOMETER") || n.Contains("VARIABLE")
                        || n.Contains("TRIMMER") || n.Contains("RHEOSTAT");
                return (2, pot ? "Pot" : null);
            }

            if (n.StartsWith("LED")) return (3, "LED");

            if (n == "D" || n.StartsWith("D_"))
            {
                if (n.Contains("ZENER")) return (3, "Zener");
                if (n.Contains("SCHOTTKY")) return (3, "Schottky");
                if (n.Contains("TVS")) return (3, "TVS");
                return (3, null);
            }

            if (n.StartsWith("Q_")) return (4, null);
            if (n.StartsWith("CRYSTAL") || n == "OSCILLATOR" || n == "RESONATOR") return (5, null);

            return (null, null);   // ör. Device:L (bobin) — henüz tipimiz yok
        }

        if (ns.StartsWith("Regulator_", StringComparison.OrdinalIgnoreCase)) return (6, null);
        if (ns.StartsWith("Transistor_", StringComparison.OrdinalIgnoreCase)) return (4, null);
        if (ns.Equals("Oscillator", StringComparison.OrdinalIgnoreCase) ||
            ns.Equals("Crystal", StringComparison.OrdinalIgnoreCase)) return (5, null);
        if (ns.StartsWith("Connector", StringComparison.OrdinalIgnoreCase)) return (7, null);
        if (ns.Equals("Diode", StringComparison.OrdinalIgnoreCase)) return (3, null);

        return (null, null);
    }

    public static (SchematicValueKind kind, double? si, string? part) InterpretValue(string? rawValue, string libId)
    {
        string v = (rawValue ?? "").Trim();
        if (v.Length == 0 || v == "~")
            return (SchematicValueKind.None, null, null);

        if (TryNumeric(v, out double si))
            return (SchematicValueKind.Numeric, si, null);

        // Value sembol adını tekrarlıyorsa: jenerik kütüphanede bilgi yok ("LED", "C"),
        // specific kütüphanede sembol adı zaten parça numarasıdır ("LM317_TO-220").
        SplitLibId(libId, out string ns, out string name);
        if (string.Equals(v, name, StringComparison.OrdinalIgnoreCase) && GenericNamespaces.Contains(ns))
            return (SchematicValueKind.None, null, null);

        return (SchematicValueKind.PartNumber, null, v);
    }

    // Standart değer: 100n, 1k, 4.7uF, 240. Kısayol gösterim: 4k7, 2R2, 1R.
    private static readonly Regex StdToken =
        new(@"^\d+([.,]\d+)?\s*[pnuµμmkKMGR]?\s*(F|Ω|ohm|R|H|Hz|W|V|A)?$", RegexOptions.IgnoreCase);
    private static readonly Regex RNotation =
        new(@"^(\d*)([RkKMG])(\d*)$");

    private static readonly Dictionary<char, double> RPrefix = new()
    {
        ['R'] = 1, ['k'] = 1e3, ['K'] = 1e3, ['M'] = 1e6, ['G'] = 1e9
    };

    private static bool TryNumeric(string v, out double si)
    {
        si = 0;
        if (!v.Any(char.IsDigit))
            return false;

        // Önce kısayol gösterimi: "4k7" = 4.7k, "2R2" = 2.2Ω. Böylece ValueNormalizer'ın
        // ortadaki harfi görmezden gelip yanlış sayı vermesini engelliyoruz.
        var rm = RNotation.Match(v);
        if (rm.Success)
        {
            string left = rm.Groups[1].Value;
            string right = rm.Groups[3].Value;
            double mult = RPrefix[rm.Groups[2].Value[0]];
            string num = right.Length == 0
                ? (left.Length == 0 ? "0" : left)
                : (left.Length == 0 ? "0" : left) + "." + right;

            if (double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double baseVal))
            {
                si = baseVal * mult;
                return true;
            }
        }

        if (StdToken.IsMatch(v))
        {
            double? norm = new ValueNormalizer().NormalizeNumeric(v);
            if (norm.HasValue)
            {
                si = norm.Value;
                return true;
            }
        }

        return false;
    }

    private static void SplitLibId(string libId, out string ns, out string name)
    {
        int idx = libId.IndexOf(':');
        if (idx >= 0)
        {
            ns = libId.Substring(0, idx);
            name = libId.Substring(idx + 1);
        }
        else
        {
            ns = "";
            name = libId;
        }
    }
}
