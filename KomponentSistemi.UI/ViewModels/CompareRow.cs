using System;

namespace KomponentSistemi.UI.ViewModels;

// Karşılaştırma tablosunda tek satır: parametre adı + iki komponentin değerleri.
// Salt-okuma veri taşıyıcı (değişmez), bu yüzden ObservableObject gerekmiyor.
public class CompareRow
{
    public string Name { get; init; } = "";
    public string ValueA { get; init; } = "";
    public string ValueB { get; init; } = "";

    // Değerler farklıysa fark say — ama önce NORMALİZE et, yoksa aynı şeyi farklı sanarız:
    //   "0.05 PF"  ↔ "±0.05PF"            (± ve boşluk yazım farkı)
    //   "0402"     ↔ "0402 (1005 METRIC)" (parantezli ek gösterim)
    public bool IsDifferent => Normalize(ValueA) != Normalize(ValueB);

    private static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = s.Trim().ToUpperInvariant();

        int p = t.IndexOf('(');                 // "0402 (1005 METRIC)" → "0402"
        if (p > 0) t = t.Substring(0, p);

        return t.Replace("±", "")               // tolerans işareti
                .Replace("+/-", "")
                .Replace(" ", "")               // "0.05 PF" → "0.05PF"
                .Trim();
    }

    // Tabloda gözle görünür işaret.
    public string DiffMark => IsDifferent ? "●" : "";
}
