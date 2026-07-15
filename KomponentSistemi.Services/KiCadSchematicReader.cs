using System.Text;

namespace KomponentSistemi.Services;

// ============================================================================
//  KiCad şematiğini OKUYAN servis — SALT-OKUMA. Dosyaya ASLA yazmaz.
// ============================================================================
//  .kicad_sch dosyaları "S-expression" formatındadır: iç içe parantezler.
//    (symbol (lib_id "Device:C") ... (property "Reference" "C1") ...)
//  Bu, LISP'e benzer; her şey bir liste ya da bir yapraktır (atom/metin).
//
//  İşimiz iki adım:
//    1) Metni bir AĞACA çevir (ayrıştırıcı — SExpr).
//    2) Ağacın kökündeki (symbol ...) bloklarını gez, her birinden
//       Reference / Value / Footprint / lib_id / uuid çıkar.
//
//  ÖNEMLİ ayrım: Dosyanın başındaki büyük (lib_symbols ...) bloğunun İÇİNDE de
//  (symbol "Device:C" ...) tanımları var. Ama onlar kökün TORUNU (lib_symbols'ün
//  çocuğu). Biz sadece kökün DOĞRUDAN çocuğu olan (symbol ...) bloklarını alırız —
//  onlar tahtaya gerçekten yerleştirilmiş sembollerdir. Böylece tarif defterini
//  değil, pişmiş tabakları sayarız.
// ============================================================================
public class KiCadSchematicReader
{
    // Okuma sonucu: bulunan semboller + varsa hata/uyarılar.
    public class ReadResult
    {
        public List<KiCadSymbol> Symbols { get; } = new();
        public List<string> Errors { get; } = new();
    }

    // Ana metod: dosya yolunu al, yerleştirilmiş sembolleri döndür.
    public ReadResult Read(string filePath)
    {
        var result = new ReadResult();

        try
        {
            string text = File.ReadAllText(filePath, Encoding.UTF8);
            SNode root = SExpr.Parse(text);

            if (root.Head != "kicad_sch")
                result.Errors.Add($"Uyarı: dosyanın kökü '{root.Head}', beklenen 'kicad_sch'. Yine de denenecek.");

            // Kökün DOĞRUDAN çocukları arasındaki (symbol ...) = yerleştirilmiş semboller.
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

    // Tek bir yerleştirilmiş (symbol ...) bloğundan ilgili alanları çıkar.
    private static KiCadSymbol ReadPlacedSymbol(SNode sym)
    {
        string libId = sym.Child("lib_id")?.Nth(1) ?? "";
        string uuid = sym.Child("uuid")?.Nth(1) ?? "";

        string? propReference = null, value = null, footprint = null;
        foreach (var it in sym.Items)
        {
            if (it.IsAtom || it.Head != "property") continue;
            string? name = it.Nth(1);   // property adı
            string? val = it.Nth(2);    // property değeri
            switch (name)
            {
                case "Reference": propReference = val; break;
                case "Value": value = val; break;
                case "Footprint": footprint = val; break;
            }
        }

        // Otorite referans: (instances ... (reference "X")). Yoksa property "Reference".
        // (Düz tek-sayfa şemada ikisi aynıdır; ama instances bloğu hiyerarşik/çok-birimli
        //  durumları da doğru verir, o yüzden onu önceleriz.)
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

    // (instances ...) alt-ağacında ilk (reference "X") değerini bul.
    private static string? FindInstanceReference(SNode sym)
    {
        var instances = sym.Child("instances");
        return instances is null ? null : FindFirst(instances, "reference");
    }

    // Bir düğümün altında, başı 'head' olan ilk listenin 2. öğesini (değerini) döndür.
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

// ----------------------------------------------------------------------------
//  S-expression düğümü: ya bir LİSTE (Items dolu) ya da bir YAPRAK (IsAtom=true).
// ----------------------------------------------------------------------------
internal sealed class SNode
{
    public bool IsAtom;                       // true → yaprak (metin); false → liste
    public string Text = "";                  // yaprak değeri (tırnaklar çözülmüş)
    public List<SNode> Items = new();         // liste çocukları

    // Listenin "başı" = ilk çocuğun atom metni. (symbol ...) → "symbol".
    public string? Head =>
        !IsAtom && Items.Count > 0 && Items[0].IsAtom ? Items[0].Text : null;

    // index'inci öğe bir yapraksa metnini ver. (property "Reference" "C1") → Nth(2)="C1".
    public string? Nth(int index) =>
        index < Items.Count && Items[index].IsAtom ? Items[index].Text : null;

    // Bu listenin çocukları arasında başı 'head' olan ilk alt-liste.
    public SNode? Child(string head)
    {
        foreach (var it in Items)
            if (!it.IsAtom && it.Head == head) return it;
        return null;
    }
}

// ----------------------------------------------------------------------------
//  Küçük S-expression ayrıştırıcı (KiCad dosyaları için yeterli).
//  Tek geçişte, string üzerinde indeks yürüterek çalışır.
// ----------------------------------------------------------------------------
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

    // s[i] == '(' olmalı. Listeyi okur, i'yi kapanış ')' sonrasına taşır.
    private static SNode ParseList(string s, ref int i)
    {
        var node = new SNode { IsAtom = false };
        i++; // '(' yut
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

    // Yaprak okur: "tırnaklı metin" ya da çıplak atom (boşluk/parantez görene dek).
    private static SNode ParseAtom(string s, ref int i)
    {
        if (s[i] == '"')
        {
            i++; // açılış tırnağını yut
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length) { sb.Append(s[i + 1]); i += 2; }  // kaçış: \" \\ vb.
                else if (c == '"') { i++; break; }                                   // kapanış tırnağı
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
