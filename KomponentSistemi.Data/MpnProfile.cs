namespace KomponentSistemi.Data;

// Bir üretici ailesinin parça numarası (MPN) şemasını VERİ olarak tanımlar;
// çözme motoru (MpnDecoder) bu veriyi uygular, üretici bilgisi içermez.
// Yeni üretici = yeni satır, kod değişikliği değil (ImportProfile ile aynı felsefe).
public class MpnProfile
{
    public int Id { get; set; }

    // Hangi komponent tipine ait: 1=Kondansatör, 2=Direnç. Çözülen "value"nun
    // anlamını (Farad mı Ohm mu) ve hangi alanların dolacağını belirler.
    public int ComponentTypeId { get; set; } = 2;

    public string Name { get; set; } = "";

    // Profil eşleşince arayüzde üretici alanını doldurmak için; DB'deki
    // Component.Manufacturer değerleriyle aynı yazımda tutulmalı.
    public string Manufacturer { get; set; } = "";

    // İsimli gruplu .NET regex'i. Tanınan gruplar: value (zorunlu), tol, tcr, power.
    // Dizilim üreticiye göre değiştiği için (tolerans değerden önce/sonra olabilir)
    // konum alanları yerine desen kullanılır — desen de veridir.
    public string PatternRegex { get; set; } = "";

    // value grubunun kodlama kuralı; üreticiye değil biçime bağlı:
    // "sig-zeros-R": R = ondalık nokta; yoksa son hane sıfır sayısı (16900 -> 1690).
    // "rkm":         IEC 60062 R/K/M gösterimi (2K2 -> 2200, 1R0 -> 1, düz 250 -> 250).
    public string ValueEncoding { get; set; } = "";

    // {"F":1.0,"B":0.1} — tol grubu harfi → tolerans %.
    public string ToleranceMapJson { get; set; } = "{}";

    // {"H":50,"E":25} — tcr grubu harfi → TCR ppm/°C.
    public string TcrMapJson { get; set; } = "{}";

    // {"1":1.0,"0":10.0} — power grubu → güç W (ör. Ohmite watt hanesi).
    public string PowerMapJson { get; set; } = "{}";

    // {"1J":"0603"} — size grubu → paket adı (çip dirençlerde boy kodu paketi söyler).
    public string PackageMapJson { get; set; } = "{}";

    // Kondansatör: dielektrik kodu → ad ("R7":"X7R"). Boşsa "diel" grubu
    // olduğu gibi kullanılır (TDK gibi açık "X7R" yazan üreticiler için).
    public string DielectricMapJson { get; set; } = "{}";

    // Kondansatör: gerilim kodu → V. Boşsa motorun EIA tablosu kullanılır
    // (0J=6.3, 1H=50...); farklı kodlu üreticiler için buradan geçilebilir.
    public string VoltageMapJson { get; set; } = "{}";
}
