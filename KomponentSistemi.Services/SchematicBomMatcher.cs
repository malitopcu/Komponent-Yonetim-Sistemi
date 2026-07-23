using System.Text.Json;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

public enum SchematicSuggestStatus
{
    ExactValue,      // sayısal değere tam oturan aday(lar) var
    NearValue,       // en yakınlar var ama tam değil
    MpnMatch,        // MPN araması aday buldu
    NoValueManual,   // tip belli, değer yok → elle seçilecek
    NoMatch,         // aday çıkmadı
    UnknownType      // lib_id'yi tanıyamadık
}

public class SuggestedCandidate
{
    public int ComponentId { get; init; }
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public double? PrimaryValueSi { get; init; }
    public double? SecondaryValueSi { get; init; }
    public string? PrimaryUnit { get; init; }
    public string? SecondaryUnit { get; init; }
    public string? Subtype { get; init; }
    public string? Tolerance { get; init; }
    public string? Dielectric { get; init; }
    public string? Package { get; init; }
    public string? Pitch { get; init; }
    public double RelError { get; init; }
    public bool IsExact { get; init; }

    // Açılır listede sütun sütun hizalanmış gösterim için ayrı alanlar.
    public string PrimaryDisplay => ValueNormalizer.FormatSi(PrimaryValueSi, PrimaryUnit);
    public string SecondaryDisplay => ValueNormalizer.FormatSi(SecondaryValueSi, SecondaryUnit);
    public string ToleranceDisplay => Tolerance ?? "";

    public string PackageDisplay
    {
        get
        {
            var bits = new List<string>();
            if (!string.IsNullOrWhiteSpace(Dielectric)) bits.Add(Dielectric!);
            var pkg = !string.IsNullOrWhiteSpace(Package) ? Package : Pitch;
            if (!string.IsNullOrWhiteSpace(pkg)) bits.Add(pkg!);
            return string.Join(" ", bits);
        }
    }

    // Konsol/tek-satır kullanımı için birleşik gösterim.
    public string Display
    {
        get
        {
            var parts = new List<string> { Mpn };
            if (!string.IsNullOrWhiteSpace(Manufacturer)) parts.Add(Manufacturer);
            if (PrimaryDisplay.Length > 0) parts.Add(PrimaryDisplay);
            if (SecondaryDisplay.Length > 0) parts.Add(SecondaryDisplay);
            if (!string.IsNullOrWhiteSpace(Tolerance)) parts.Add(Tolerance!);
            if (PackageDisplay.Length > 0) parts.Add(PackageDisplay);
            return string.Join("  ·  ", parts);
        }
    }
}

public class SchematicSuggestion
{
    public string Reference { get; init; } = "";
    public int? ComponentTypeId { get; init; }
    public string TypeName { get; set; } = "";
    public string? Subtype { get; init; }
    public SchematicValueKind ValueKind { get; init; }
    public double? TargetSi { get; init; }
    public string RawValue { get; init; } = "";
    public string? PartText { get; init; }
    public int? ExpectedPositions { get; init; }

    public SchematicSuggestStatus Status { get; set; }
    public int TypePoolCount { get; set; }
    public List<SuggestedCandidate> Candidates { get; } = new();
}

// Adım 2: şema yorumlarını (Adım 1) veritabanındaki komponentlerle eşleştirip aday önerir.
// Sayısal değerler için değere tam/yakın parçalar, MPN metni için parça-no araması. Salt-okuma.
public class SchematicBomMatcher
{
    private const int NearWhenNoExact = 12;   // tam eşleşme yoksa en yakın kaç aday
    private const int MpnCap = 25;
    private const int ManualPoolCap = 50;

    public List<SchematicSuggestion> Suggest(string schematicPath, out List<string> errors)
    {
        var interpreted = SchematicInterpreter.InterpretFile(schematicPath, out errors);

        using var db = new AppDbContext();
        var typeNames = db.ComponentTypes.AsNoTracking().ToDictionary(t => t.Id, t => t.Name);
        var units = LoadUnits(db);

        var byType = db.Components.AsNoTracking()
            .Select(c => new { c.Id, c.Mpn, c.Manufacturer, c.ComponentTypeId, c.PrimaryValueSi, c.SecondaryValueSi, c.ParamsJson })
            .AsEnumerable()
            .Select(c =>
            {
                var p = ReadParams(c.ParamsJson);
                return new Candidate(c.Id, c.Mpn, c.Manufacturer, c.ComponentTypeId,
                    c.PrimaryValueSi, c.SecondaryValueSi, p.subtype, p.tolerance, p.dielectric, p.package, p.pitch);
            })
            .GroupBy(c => c.TypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<SchematicSuggestion>();
        foreach (var sym in interpreted)
            result.Add(BuildSuggestion(sym, byType, typeNames, units));

        return result;
    }

    private static SchematicSuggestion BuildSuggestion(
        SchematicSymbolInfo sym, Dictionary<int, List<Candidate>> byType,
        Dictionary<int, string> typeNames, Dictionary<int, (string? primary, string? secondary)> units)
    {
        var s = new SchematicSuggestion
        {
            Reference = sym.Reference,
            ComponentTypeId = sym.ComponentTypeId,
            Subtype = sym.Subtype,
            ValueKind = sym.ValueKind,
            TargetSi = sym.NumericSi,
            RawValue = sym.RawValue,
            PartText = sym.PartText,
            ExpectedPositions = sym.ExpectedPositions
        };

        if (sym.ComponentTypeId is not int typeId)
        {
            s.Status = SchematicSuggestStatus.UnknownType;
            return s;
        }

        s.TypeName = typeNames.GetValueOrDefault(typeId, "");
        var pool = byType.GetValueOrDefault(typeId) ?? new List<Candidate>();
        var u = units.GetValueOrDefault(typeId);

        switch (sym.ValueKind)
        {
            case SchematicValueKind.Numeric when sym.NumericSi is double target:
                RankByValue(s, pool, target, sym.Subtype, u);
                break;

            case SchematicValueKind.PartNumber when sym.PartText is string part:
                RankByMpn(s, pool, part, u);
                break;

            default:
                FillManualPool(s, pool, sym.Subtype, sym.ExpectedPositions, u);
                break;
        }

        return s;
    }

    private static void RankByValue(
        SchematicSuggestion s, List<Candidate> pool, double target, string? interpSubtype,
        (string? primary, string? secondary) units)
    {
        var scoped = pool.Where(c => c.PrimaryValueSi.HasValue).ToList();

        var filtered = FilterBySubtype(scoped, interpSubtype);
        if (filtered.Count > 0) scoped = filtered;

        var scored = scoped
            .Select(c => (c, err: target == 0 ? Math.Abs(c.PrimaryValueSi!.Value) : Math.Abs(c.PrimaryValueSi!.Value - target) / target))
            .OrderBy(x => x.err)
            .ToList();

        if (scored.Count == 0)
        {
            s.Status = SchematicSuggestStatus.NoMatch;
            return;
        }

        // Tam eşleşenlerin hepsini göster; hiç tam eşleşme yoksa en yakınları listele.
        var exact = scored.Where(x => x.err < 1e-6).ToList();
        var chosen = exact.Count > 0 ? exact : scored.Take(NearWhenNoExact).ToList();

        foreach (var (c, err) in chosen)
            s.Candidates.Add(MakeCandidate(c, units, err, err < 1e-6));

        s.Status = exact.Count > 0 ? SchematicSuggestStatus.ExactValue : SchematicSuggestStatus.NearValue;
    }

    private static void RankByMpn(
        SchematicSuggestion s, List<Candidate> pool, string part, (string? primary, string? secondary) units)
    {
        var hits = pool.Where(c => c.Mpn.Contains(part, StringComparison.OrdinalIgnoreCase)).ToList();

        // KiCad sembol adı çoğu zaman "PARÇA_PAKET" (LM317_TO-220). Tam metin tutmazsa
        // ilk alt çizgiye kadar olan çekirdek parça numarasıyla tekrar deniyoruz.
        if (hits.Count == 0 && part.Contains('_'))
        {
            string core = part.Substring(0, part.IndexOf('_'));
            hits = pool.Where(c => c.Mpn.Contains(core, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (hits.Count == 0)
        {
            s.Status = SchematicSuggestStatus.NoMatch;
            return;
        }

        foreach (var c in hits.OrderBy(c => c.Mpn, StringComparer.OrdinalIgnoreCase).Take(MpnCap))
            s.Candidates.Add(MakeCandidate(c, units, 0, false));

        s.Status = SchematicSuggestStatus.MpnMatch;
    }

    // Değersiz sembol (LED, konnektör): sıralayacak değer yok ama tip belli.
    // Konnektörde giriş sayısı biliniyorsa ona göre süzüyoruz; kalanları elle seçtiriyoruz.
    private static void FillManualPool(
        SchematicSuggestion s, List<Candidate> pool, string? interpSubtype, int? expectedPositions,
        (string? primary, string? secondary) units)
    {
        s.Status = SchematicSuggestStatus.NoValueManual;

        var scoped = FilterBySubtype(pool, interpSubtype);
        if (scoped.Count == 0) scoped = pool;

        if (expectedPositions is int pos)
        {
            var byPos = scoped
                .Where(c => c.PrimaryValueSi.HasValue && (int)Math.Round(c.PrimaryValueSi.Value) == pos)
                .ToList();
            if (byPos.Count > 0) scoped = byPos;
        }

        s.TypePoolCount = scoped.Count;

        foreach (var c in scoped.OrderBy(c => c.Mpn, StringComparer.OrdinalIgnoreCase).Take(ManualPoolCap))
            s.Candidates.Add(MakeCandidate(c, units, 0, false));
    }

    private static SuggestedCandidate MakeCandidate(
        Candidate c, (string? primary, string? secondary) units, double relError, bool isExact) =>
        new()
        {
            ComponentId = c.Id,
            Mpn = c.Mpn,
            Manufacturer = c.Manufacturer,
            PrimaryValueSi = c.PrimaryValueSi,
            SecondaryValueSi = c.SecondaryValueSi,
            PrimaryUnit = units.primary,
            SecondaryUnit = units.secondary,
            Subtype = c.Subtype,
            Tolerance = c.Tolerance,
            Dielectric = c.Dielectric,
            Package = c.Package,
            Pitch = c.Pitch,
            RelError = relError,
            IsExact = isExact
        };

    private static List<Candidate> FilterBySubtype(List<Candidate> pool, string? interpSubtype)
    {
        string want = SubtypeCategory(interpSubtype);
        return pool.Where(c => SubtypeCategory(c.Subtype) == want).ToList();
    }

    // DB'de alt tür etiketleri tutarsız yazılmış (TRIMPOT, POTANSIYOMETRE, ZENER...).
    // Yorumlayıcının ürettiği etiketle (Pot, Zener...) buluşsun diye ortak kategoriye indiriyoruz.
    private static string SubtypeCategory(string? subtype)
    {
        if (string.IsNullOrWhiteSpace(subtype)) return "";
        string u = subtype.ToUpperInvariant();

        if (u.Contains("POT") || u.Contains("RHEOSTAT") || u.Contains("VARIABLE") || u.Contains("TRIMMER"))
            return "POT";
        if (u.Contains("ZENER")) return "ZENER";
        if (u.Contains("SCHOTTKY")) return "SCHOTTKY";
        if (u.Contains("TVS")) return "TVS";
        if (u.Contains("LED")) return "LED";

        return u;
    }

    private static Dictionary<int, (string? primary, string? secondary)> LoadUnits(AppDbContext db)
    {
        var defs = db.ParameterDefinitions.AsNoTracking()
            .Where(p => p.HotColumn == "primary" || p.HotColumn == "secondary")
            .Select(p => new { p.ComponentTypeId, p.HotColumn, p.Unit })
            .ToList();

        var map = new Dictionary<int, (string? primary, string? secondary)>();
        foreach (var g in defs.GroupBy(d => d.ComponentTypeId))
        {
            string? pri = g.FirstOrDefault(d => d.HotColumn == "primary")?.Unit;
            string? sec = g.FirstOrDefault(d => d.HotColumn == "secondary")?.Unit;
            map[g.Key] = (pri, sec);
        }

        // Konnektörde birincil değer giriş sayısı; birimi yok, listede anlamlı görünsün diye etiketliyoruz.
        if (map.TryGetValue(7, out var conn) && string.IsNullOrWhiteSpace(conn.primary))
            map[7] = ("giriş", conn.secondary);

        return map;
    }

    private static (string? subtype, string? tolerance, string? dielectric, string? package, string? pitch) ReadParams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (null, null, null, null, null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            string? Get(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            return (Get("subtype"), Get("tolerance"), Get("dielectric"), Get("package"), Get("pitch"));
        }
        catch
        {
            return (null, null, null, null, null);
        }
    }

    private readonly record struct Candidate(
        int Id, string Mpn, string Manufacturer, int TypeId,
        double? PrimaryValueSi, double? SecondaryValueSi,
        string? Subtype, string? Tolerance, string? Dielectric, string? Package, string? Pitch);
}
