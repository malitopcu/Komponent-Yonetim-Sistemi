using System.Text;

namespace KomponentSistemi.Services;

// .kicad_sch dosyasından yerleştirilmiş sembolleri okur. Dosyaya yazmaz.
//
// Dikkat: dosyanın başındaki (lib_symbols ...) bloğunun içinde de (symbol ...)
// düğümleri var, ama onlar kütüphane tanımı. Gerçekten tahtaya konmuş semboller
// kökün doğrudan çocuğu olanlar, o yüzden sadece root.Items'ı geziyoruz.
public class KiCadSchematicReader
{
    public class ReadResult
    {
        public List<KiCadSymbol> Symbols { get; } = new();
        public List<string> Errors { get; } = new();
    }

    public ReadResult Read(string filePath)
    {
        var result = new ReadResult();

        try
        {
            string text = File.ReadAllText(filePath, Encoding.UTF8);
            SNode root = SExpr.Parse(text);

            if (root.Head != "kicad_sch")
                result.Errors.Add($"Uyarı: dosyanın kökü '{root.Head}', beklenen 'kicad_sch'. Yine de denenecek.");

            foreach (var child in root.Items)
            {
                if (child.IsAtom || child.Head != "symbol") continue;
                result.Symbols.Add(ReadPlacedSymbol(child));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Okuma hatası: {ex.Message}");
        }

        return result;
    }

    private static KiCadSymbol ReadPlacedSymbol(SNode sym)
    {
        string libId = sym.Child("lib_id")?.Nth(1) ?? "";
        string uuid = sym.Child("uuid")?.Nth(1) ?? "";

        string? propReference = null, value = null, footprint = null;
        foreach (var it in sym.Items)
        {
            if (it.IsAtom || it.Head != "property") continue;

            switch (it.Nth(1))
            {
                case "Reference": propReference = it.Nth(2); break;
                case "Value": value = it.Nth(2); break;
                case "Footprint": footprint = it.Nth(2); break;
            }
        }

        // Tek sayfalık şemada property "Reference" ile aynı, ama hiyerarşik ve
        // çok birimli sembollerde doğru olan (instances ...) altındaki değer.
        string reference = FindInstanceReference(sym) ?? propReference ?? "";

        return new KiCadSymbol
        {
            Reference = reference,
            LibId = libId,
            Value = value ?? "",
            Footprint = footprint ?? "",
            Uuid = uuid
        };
    }

    private static string? FindInstanceReference(SNode sym)
    {
        var instances = sym.Child("instances");
        return instances is null ? null : FindFirst(instances, "reference");
    }

    private static string? FindFirst(SNode node, string head)
    {
        foreach (var it in node.Items)
        {
            if (it.IsAtom) continue;
            if (it.Head == head) return it.Nth(1);

            var deeper = FindFirst(it, head);
            if (deeper is not null) return deeper;
        }

        return null;
    }
}

// S-expression düğümü: ya bir liste ya da bir yaprak.
internal sealed class SNode
{
    public bool IsAtom;
    public string Text = "";
    public List<SNode> Items = new();

    // Listenin ilk atomu, yani (symbol ...) için "symbol".
    public string? Head =>
        !IsAtom && Items.Count > 0 && Items[0].IsAtom ? Items[0].Text : null;

    // (property "Reference" "C1") → Nth(2) = "C1"
    public string? Nth(int index) =>
        index < Items.Count && Items[index].IsAtom ? Items[index].Text : null;

    public SNode? Child(string head)
    {
        foreach (var it in Items)
            if (!it.IsAtom && it.Head == head) return it;

        return null;
    }
}

// KiCad dosyalarına yetecek kadarlık bir S-expression ayrıştırıcı.
internal static class SExpr
{
    public static SNode Parse(string s)
    {
        int i = 0;
        SkipWs(s, ref i);

        if (i >= s.Length || s[i] != '(')
            throw new FormatException("Beklenen '(' bulunamadı — geçerli bir S-expression değil.");

        return ParseList(s, ref i);
    }

    private static SNode ParseList(string s, ref int i)
    {
        var node = new SNode { IsAtom = false };
        i++;

        while (true)
        {
            SkipWs(s, ref i);
            if (i >= s.Length)
                throw new FormatException("Beklenmeyen dosya sonu — kapanmayan parantez.");

            char c = s[i];
            if (c == ')') { i++; break; }

            node.Items.Add(c == '(' ? ParseList(s, ref i) : ParseAtom(s, ref i));
        }

        return node;
    }

    private static SNode ParseAtom(string s, ref int i)
    {
        if (s[i] == '"')
        {
            i++;
            var sb = new StringBuilder();

            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length) { sb.Append(s[i + 1]); i += 2; }
                else if (c == '"') { i++; break; }
                else { sb.Append(c); i++; }
            }

            return new SNode { IsAtom = true, Text = sb.ToString() };
        }

        int start = i;
        while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] != '(' && s[i] != ')' && s[i] != '"')
            i++;

        return new SNode { IsAtom = true, Text = s.Substring(start, i - start) };
    }

    private static void SkipWs(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }
}
