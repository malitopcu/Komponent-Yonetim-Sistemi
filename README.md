# Komponent & BOM Yönetim Sistemi

Elektronik komponent envanteri, malzeme listesi (BOM) yönetimi ve parça
numarasından (MPN) otomatik veri çıkarımı yapan bir masaüstü uygulaması.
Direnç, kondansatör, diyot, transistör, osilatör, regülatör ve konnektör
olmak üzere yedi komponent tipini tek bir esnek veri modeliyle yönetir.
Model veri-güdümlü olduğu için ileride yeni komponent tipleri de
(entegre devreler, sensörler, röleler… gibi) kod değişikliği gerektirmeden,
yalnızca veri ekleyerek desteklenebilir.

## Özellikler

- **Envanter & arama** — Tüm komponentler tek tabloda; tipe göre filtre,
  değer aralığı sorgusu (örn. 1 kΩ – 10 kΩ), SI tabanlı karşılaştırma.
- **BOM (proje malzeme listesi)** — Proje bazlı listeler, adet, referans
  (R1, R5…), teklif seçimi ve CSV dışa aktarma.
- **CSV içe aktarma** — Tedarikçi dosyalarını içe aktarma; okuma profilleri,
  RoHS, ve geri alınabilir değişiklik geçmişi.
- **MPN çözücü** — Parça numarasından değer/tolerans/paket/üretici gibi
  özellikleri otomatik çıkarma (aşağıda ayrıntı).
- **Komponent Ekle formu** — Tip seçince alanları kendine göre kuran dinamik
  form; MPN yazıp "MPN'den Doldur" ile hızlı giriş.
- **KiCad entegrasyonu** — Şemadan footprint okuma/eşleştirme/yazma ve
  şemadan BOM önerisi.
- **Teklif/fiyat** — Bir komponentin birden çok tedarikçi teklifi.

## Mimari

Üç katmanlı, katmanlar arası bağımlılık tek yönlü:

```
KomponentSistemi.UI        (Avalonia + MVVM — arayüz)
        ↓
KomponentSistemi.Services  (iş mantığı — sorgu, arama, BOM, import, MPN çözücü)
        ↓
KomponentSistemi.Data      (EF Core entity'leri, DbContext, migration, tohum veri)
```

**Teknolojiler:** C# / .NET 10 · Avalonia 12 (MVVM, CommunityToolkit.Mvvm) ·
Entity Framework Core 10 · SQLite · CsvHelper.

## Veri modeli

Tasarımın merkezinde **tek bir `Component` tablosunun yedi tipi de tutması**
yatar. Her tipe ayrı tablo yoktur; farklar veriyle yönetilir:

- `Component` — envanterdeki gerçek parça. Aranabilir iki değer
  `PrimaryValueSi` / `SecondaryValueSi`'de (anlamı tipe göre değişir:
  dirençte Ω/W, kondansatörde F/V…), gerisi `ParamsJson`'da. Tüm sayısal
  değerler **SI temel biriminde** saklanır (1.69 kΩ ve 1690 aynı sayıdır).
- `ComponentType` — yedi tip.
- `ParameterDefinition` — her tipin hangi parametrelere sahip olduğunu
  tanımlar (dirençte resistance/power, kondansatörde capacitance/voltage…).
  Yeni tip/parametre eklemek **kod değil, satır** eklemektir.
- `MpnProfile` — bir üretici ailesinin parça-numarası şemasını **veri olarak**
  taşır (regex deseni + harf→anlam haritaları). Yeni üretici = yeni satır.
- `Offer` — tedarikçi/fiyat. `BomList` / `BomItem` — projeler ve kalemleri.
- `ImportProfile` / `ImportBatch` / `ImportChange` — içe aktarma ayarları,
  işlem kaydı ve geri alma geçmişi.

`ParameterDefinition` ve `MpnProfile` "veri olarak yapılandırma" örnekleridir:
uygulamanın davranışı kodda gömülü değil, veritabanında satır olarak durur.

## Kurulum & çalıştırma

**Gereksinim:** [.NET 10 SDK](https://dotnet.microsoft.com/).

```bash
dotnet run --project KomponentSistemi.UI
```

Tek komut yeterli. Uygulama ilk açılışta veritabanını (`~/KomponentSistemi/components.db`)
kendisi oluşturur, bekleyen migration'ları uygular ve tohum veriyi (tipler,
parametreler, MPN profilleri) yükler. Ayrı bir veritabanı komutuna gerek yoktur.

> Geliştirme sırasında yeni migration eklemek için:
> `dotnet ef migrations add <Ad> --project KomponentSistemi.Data --startup-project KomponentSistemi.Data`
> (`dotnet ef` kurulu değilse: `dotnet tool install --global dotnet-ef`)

## MPN çözücü

Parça numaraları özellikleri içlerinde şifreli taşır. Örneğin Vishay
`RCMT05 10R00 F H`: boy `05` → 0,25 W, `10R00` → 10 Ω, `F` → ±%1,
`H` → 50 ppm/°C. Çözücü her üretici ailesinin şemasını `MpnProfile`
satırından okuyup uygular; numarada kodlanmamış hiçbir alanı **uydurmaz**,
boş bırakır. Profili olmayan üreticilerde IEC 60062 / hane-kodu tabanlı bir
öneri katmanı devreye girer.

Güncel kapsam (23 profil):

| Tip | Profil | Kapsam |
| --- | --- | --- |
| Direnç | 16 | Yageo, Panasonic, KOA, Vishay (Dale + Sfernice), Bourns, ROHM, Walsin, Samsung, Ohmite, Stackpole + jenerik Çinli şema |
| Kondansatör | 2 | Seramik MLCC: TDK, Murata |
| Regülatör | 2 | 78xx + AMS1117/LM1117/LD1117 (çıkış gerilimi) |
| Osilatör | 1 | Frekansı numarada açık yazan seriler |
| Konnektör | 2 | JST XH/PH (pin sayısı) |
| Diyot / Transistör | 0 | (tip-adı numaraları — çözücüye uygun değil) |

## Proje yapısı

```
KomponentSistemi.UI/        Avalonia arayüz (Views + ViewModels)
KomponentSistemi.Services/  İş mantığı ve alan servisleri
KomponentSistemi.Data/      EF Core entity'leri, AppDbContext, SeedData, Migrations
```

## Yol haritası — ileride yapılabilecekler

Proje veri-güdümlü kurulduğu için aşağıdaki genişletmelerin çoğu, çekirdek
koda dokunmadan veri (profil/parametre) eklenerek gerçekleştirilebilir.

**MPN çözücü kapsamını artırma**

- Kondansatörde elektrolitik/tantal/film tipleri ve Samsung, KEMET, AVX,
  Yageo gibi eksik MLCC üreticilerinin profilleri.
- Diyot (Zener gerilimi) ve transistör desteği — bunlar tip-adı numaraları
  olduğundan regex yerine "bilinen parça → değer" lookup tablosu mekanizması.
- Daha fazla regülatör (switching/ayarlanabilir LDO aileleri), osilatör ve
  konnektör serisi.

**Yeni komponent tipleri**

- Entegre devre, sensör, röle, indüktör gibi tiplerin `ComponentType` ve
  `ParameterDefinition` satırlarıyla eklenmesi.

**Uygulama & veri**

- Otomatik veri zenginleştirme (tedarikçi API'leri: TME, Farnell, Mouser)
  ve kaynak/format otomatik algılama.
- Stok ve kritik eşik takibi, düşük stok uyarıları.
- KiCad entegrasyonunun derinleştirilmesi (MPN/üretici yazma, footprint
  haritasını yapılandırmaya taşıma).

**Arayüz**

- Kategorik filtreler ve aktif filtre çipleri, sürükle-bırak, sütun
  tercihlerini hatırlama, çoklu komponent karşılaştırma.
- Kurulumu kolaylaştırmak için açılışta otomatik migration.
