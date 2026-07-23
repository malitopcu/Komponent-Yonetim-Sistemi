using System.Text;

namespace KomponentSistemi.Services;

// Eşleşen sembollerin Footprint property'sini şema dosyasında yerinde günceller.
// Dosyayı yeniden üretmiyoruz; KiCad'in kendi biçimlendirmesi ve bizim
// okumadığımız alanlar aynen kalsın diye sadece ilgili metin aralıklarını değiştiriyoruz.
public class KiCadWriteService
{
    public class WriteResult
    {
        public int Written;
        public string BackupPath = "";
        public List<string> Details = new();
        public List<string> Errors = new();
    }

    public WriteResult Write(string schematicPath, int? bomListId = null)
    {
        var result = new WriteResult();

        var match = new KiCadMatchService().Match(schematicPath, bomListId);
        result.Errors.AddRange(match.Errors);

        var toWrite = match.Rows
            .Where(r => r.Status == KiCadMatchStatus.FootprintFound)
            .ToDictionary(r => r.Reference, r => r.Footprint, StringComparer.OrdinalIgnoreCase);

        if (toWrite.Count == 0)
        {
            result.Errors.Add("Yazılacak footprint yok (eşleşen yok).");
            return result;
        }

        string text = File.ReadAllText(schematicPath, Encoding.UTF8);

        result.BackupPath = schematicPath + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        File.WriteAllText(result.BackupPath, text, new UTF8Encoding(false));

        var edits = new List<(int start, int len, string replacement)>();
        foreach (var (blockStart, blockEnd, reference) in FindPlacedSymbols(text))
        {
            if (reference is null || !toWrite.TryGetValue(reference, out var fp)) continue;

            var span = FindPropertyValueSpan(text, blockStart, blockEnd, "Footprint");
            if (span is null)
            {
                result.Details.Add($"{reference}: Footprint property yok, atlandı");
                continue;
            }

            edits.Add((span.Value.start, span.Value.len, fp));
            result.Details.Add($"{reference} → {fp}");
        }

        // Sondan başa uyguluyoruz, yoksa ilk değişiklik sonraki indeksleri kaydırır.
        foreach (var e in edits.OrderByDescending(e => e.start))
            text = text.Substring(0, e.start) + e.replacement + text.Substring(e.start + e.len);

        File.WriteAllText(schematicPath, text, new UTF8Encoding(false));
        result.Written = edits.Count;
        return result;
    }

    // Yerleştirilmiş semboller (lib_id ile başlar; lib_symbols içindeki tanımlar başlamaz).
    private static IEnumerable<(int start, int end, string? reference)> FindPlacedSymbols(string t)
    {
        int i = 0;
        while ((i = t.IndexOf("(symbol", i, StringComparison.Ordinal)) >= 0)
        {
            int j = i + "(symbol".Length;
            while (j < t.Length && char.IsWhiteSpace(t[j])) j++;

            if (t.IndexOf("(lib_id", j, StringComparison.Ordinal) != j)
            {
                i += 7;
                continue;
            }

            int end = MatchParen(t, i);
            if (end < 0) yield break;

            yield return (i, end, FindProperty(t, i, end, "Reference"));
            i = end + 1;
        }
    }

    // t[open] == '(' varsayımıyla eşleşen kapanış parantezi. Tırnak içindekiler sayılmaz.
    private static int MatchParen(string t, int open)
    {
        int depth = 0;
        bool inStr = false;

        for (int k = open; k < t.Length; k++)
        {
            char c = t[k];

            if (inStr)
            {
                if (c == '\\') k++;
                else if (c == '"') inStr = false;
                continue;
            }

            if (c == '"') inStr = true;
            else if (c == '(') depth++;
            else if (c == ')' && --depth == 0) return k;
        }

        return -1;
    }

    private static string? FindProperty(string t, int start, int end, string name)
    {
        var span = FindPropertyValueSpan(t, start, end, name);
        return span is null ? null : t.Substring(span.Value.start, span.Value.len);
    }

    // (property "name" "VALUE" ...) içindeki VALUE'nun konumu ve uzunluğu.
    private static (int start, int len)? FindPropertyValueSpan(string t, int start, int end, string name)
    {
        string needle = "(property \"" + name + "\" \"";

        int p = t.IndexOf(needle, start, StringComparison.Ordinal);
        if (p < 0 || p > end) return null;

        int vStart = p + needle.Length;
        int vEnd = t.IndexOf('"', vStart);
        return vEnd < 0 ? null : (vStart, vEnd - vStart);
    }
}
