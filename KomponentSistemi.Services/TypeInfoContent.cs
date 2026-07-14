namespace KomponentSistemi.Services;

// Komponent tipleri için referans açıklamaları (arama ekranında gösterilir).
// İçerik özgündür; sonradan serbestçe düzenlenebilir.
public static class TypeInfoContent
{
    private static readonly Dictionary<int, (string Title, string Body)> Map = new()
    {
        [1] = ("Kondansatör nedir?",
            "Kondansatör, elektrik enerjisini bir yalıtkan (dielektrik) ile ayrılmış iki iletken plaka arasında " +
            "elektrik alanı biçiminde depolayan pasif bir bileşendir. Temel değeri kapasitanstır (Farad, F); ayrıca " +
            "dayanabileceği bir anma gerilimi (V) ve dielektrik tipi önemlidir.\n\n" +
            "Başlıca alt türleri:\n" +
            "• Seramik / MLCC: küçük, ucuz, yüksek frekansa uygun. Dielektrik sınıfı önemlidir — C0G/NP0 çok kararlı " +
            "ama düşük kapasiteli; X7R/X5R daha yüksek kapasiteli ama sıcaklık/gerilimle değişkendir.\n" +
            "• Elektrolitik (alüminyum): yüksek kapasite, kutupludur (polarite önemli), güç kaynağı filtrelemesinde " +
            "yaygın; ömrü ve sıcaklık hassasiyeti sınırlıdır.\n" +
            "• Tantal: küçük hacimde yüksek kapasite, kutupludur, kararlıdır; taşınabilir cihazlarda kullanılır ama " +
            "ters/aşırı gerilime duyarlıdır.\n" +
            "• Film (polyester/polipropilen): kararlı, düşük kayıplı, kutupsuz; ses ve yüksek frekans devrelerinde.\n" +
            "• Süperkapasitör: çok yüksek kapasite, hızlı şarj/deşarj; enerji depolama ve yedekleme, pil ile " +
            "kondansatör arasını köprüler.\n" +
            "• Mika: çok kararlı ve hassas; yüksek frekans/RF uygulamaları.\n\n" +
            "Kullanım: filtreleme, kuplaj/dekuplaj (bypass), enerji depolama, zamanlama ve güç hattı düzenleme."),

        [2] = ("Direnç nedir?",
            "Direnç, devrede akımı sınırlamak veya gerilimi bölmek için kullanılan, akıma karşı belirli bir zorluk " +
            "(Ohm, Ω) gösteren pasif bir bileşendir. Ohm yasası (V = I·R) ile çalışır. Anma değerleri direnç (Ω) ve " +
            "harcayabileceği güçtür (Watt, W); ayrıca tolerans (±%1 gibi) ve sıcaklık katsayısı önemlidir.\n\n" +
            "Başlıca alt türleri:\n" +
            "• Kalın/ince film SMD (0402, 0603...): en yaygın, genel amaçlı yüzey montaj dirençleri.\n" +
            "• Metal film: düşük gürültülü ve hassas; ölçüm/analog devreler.\n" +
            "• Tel sargılı (wirewound): yüksek güç ve hassasiyet, ama endüktiftir (yüksek frekansa uygun değil).\n" +
            "• Şönt / akım algılama: çok düşük değerli, üzerindeki gerilimden akım ölçmek için.\n" +
            "• Ayarlı (potansiyometre/trimpot): değeri elle değiştirilebilen dirençler.\n\n" +
            "Kullanım: akım sınırlama, gerilim bölücü, pull-up/pull-down, transistör kutuplama ve akım algılama."),

        [3] = ("Diyot nedir?",
            "Diyot, akımı yalnızca tek yönde (anottan katoda) geçiren yarı iletken bir bileşendir. Önemli değerleri " +
            "ters gerilim dayanımı (Vr), ileri akım (If) ve ileri gerilim düşümüdür (Vf).\n\n" +
            "Başlıca alt türleri:\n" +
            "• Doğrultucu (rectifier): AC'yi DC'ye çevirmede kullanılan genel amaçlı diyot.\n" +
            "• Schottky: düşük ileri gerilim (Vf) ve yüksek hız; anahtarlamalı güç kaynaklarında kayıpları azaltır.\n" +
            "• Zener: ters yönde belirli bir gerilimde (Vz) iletime geçer; gerilim referansı/regülasyonu ve kırpma " +
            "(clamping) için kullanılır.\n" +
            "• LED: ileri yönde akımda ışık yayan diyot.\n" +
            "• TVS (geçici gerilim bastırıcı): ani yüksek gerilim/ESD darbelerine karşı koruma.\n" +
            "• Hızlı/ultra hızlı toparlanan: yüksek frekanslı güç devreleri.\n\n" +
            "Kullanım: doğrultma (AC→DC), ters polarite ve aşırı gerilim koruması, Zener ile regülasyon, sinyal algılama."),

        [4] = ("Transistör nedir?",
            "Transistör, küçük bir kontrol sinyaliyle daha büyük bir akımı anahtarlayan veya yükselten yarı iletken " +
            "bir bileşendir. Önemli değerleri dayanma gerilimi (Vds/Vce), geçirebildiği akım ve güçtür; MOSFET'lerde " +
            "ayrıca iletim direnci Rds(on) önemlidir.\n\n" +
            "Başlıca alt türleri:\n" +
            "• BJT (NPN/PNP): akım kontrollü; yükseltme ve düşük güçlü anahtarlama.\n" +
            "• MOSFET (N/P kanal): gerilim kontrollü; yüksek verimli anahtarlama ve güç uygulamaları.\n" +
            "• IGBT: yüksek güçlü anahtarlama (motor sürücü, invertör).\n" +
            "• JFET: yüksek giriş empedansı, düşük gürültü; hassas analog devreler.\n\n" +
            "Kullanım: dijital devrelerde anahtar (aç/kapa), analog devrelerde yükselteç, güç anahtarlama ve motor sürme."),

        [5] = ("Osilatör nedir?",
            "Osilatör, devrenin ihtiyaç duyduğu düzenli saat/frekans sinyalini üreten bir bileşendir. Temel değeri " +
            "frekanstır (Hz); ayrıca besleme gerilimi ve frekans kararlılığı (ppm) önemlidir. Not: 'kristal/rezonatör' " +
            "pasif bir titreşim elemanıyken, 'osilatör' sürücü devresiyle birlikte hazır sinyal üretir.\n\n" +
            "Başlıca alt türleri:\n" +
            "• XO (kristal osilatör): kuvars tabanlı, yüksek doğruluk; genel amaçlı saat kaynağı.\n" +
            "• TCXO (sıcaklık dengeli): sıcaklık değişimlerine karşı daha kararlı.\n" +
            "• VCXO (gerilim kontrollü): frekansı bir kontrol gerilimiyle ayarlanabilir.\n" +
            "• OCXO (fırınlı): çok yüksek kararlılık (haberleşme altyapısı).\n" +
            "• MEMS: mekanik olarak dayanıklı, küçük ve programlanabilir.\n\n" +
            "Kullanım: mikrodenetleyici saat kaynağı, haberleşme ve zamanlama devreleri."),

        [6] = ("Regülatör nedir?",
            "Gerilim regülatörü, giriş gerilimi veya yük değişse de çıkışta sabit ve temiz bir gerilim sağlayan bir " +
            "bileşendir. Önemli değerleri çıkış gerilimi ve çıkış akımıdır; ayrıca dropout (giriş-çıkış farkı) ve " +
            "verim önemlidir.\n\n" +
            "Başlıca alt türleri:\n" +
            "• Doğrusal (linear) / LDO: basit, düşük gürültülü, az parça ister; ama giriş-çıkış farkı büyükse fazla " +
            "ısı üretir (verimsiz).\n" +
            "• Anahtarlamalı (switching): buck (düşüren), boost (yükselten), buck-boost; yüksek verimli, ısınması az, " +
            "ama daha karmaşık ve gürültülü olabilir.\n" +
            "• Gerilim referansı: regülasyondan çok, hassas ve sabit bir referans gerilim üretmek için.\n\n" +
            "Kullanım: güç besleme devreleri, mikrodenetleyici ve sensörlerin kararlı beslenmesi."),
    };

    public static (string Title, string Body)? Get(int typeId)
        => Map.TryGetValue(typeId, out var v) ? v : null;
}
