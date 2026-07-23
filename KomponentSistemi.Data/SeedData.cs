using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Data;

public static class SeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        // ===== KOMPONENT TİPLERİ =====
        modelBuilder.Entity<ComponentType>().HasData(
            new ComponentType { Id = 1, Name = "Kondansatör" },
            new ComponentType { Id = 2, Name = "Direnç" },
            new ComponentType { Id = 3, Name = "Diyot" },
            new ComponentType { Id = 4, Name = "Transistör" },
            new ComponentType { Id = 5, Name = "Osilatör" },
            new ComponentType { Id = 6, Name = "Regülatör" },
            new ComponentType { Id = 7, Name = "Konnektör" }
        );

        modelBuilder.Entity<ParameterDefinition>().HasData(
            // --- Kondansatör (Id=1) ---
            new ParameterDefinition
            {
                Id = 101, ComponentTypeId = 1, Key = "capacitance", DisplayName = "Kapasitans", Unit = "F",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 102, ComponentTypeId = 1, Key = "voltage", DisplayName = "Anma Gerilimi", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 103, ComponentTypeId = 1, Key = "dielectric", DisplayName = "Dielektrik", Unit = null,
                DataType = "text", IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 104, ComponentTypeId = 1, Key = "tolerance", DisplayName = "Tolerans", Unit = null,
                DataType = "text", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 105, ComponentTypeId = 1, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Direnç (Id=2) ---
            new ParameterDefinition
            {
                Id = 201, ComponentTypeId = 2, Key = "resistance", DisplayName = "Direnç", Unit = "Ω",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 202, ComponentTypeId = 2, Key = "power", DisplayName = "Güç", Unit = "W", DataType = "numeric",
                IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 203, ComponentTypeId = 2, Key = "tolerance", DisplayName = "Tolerans", Unit = null,
                DataType = "text", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 204, ComponentTypeId = 2, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            // Direnç alt türü: sabit direnç / potansiyometre / trimpot
            new ParameterDefinition
            {
                Id = 205, ComponentTypeId = 2, Key = "subtype", DisplayName = "Alt Tür", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 206, ComponentTypeId = 2, Key = "taper", DisplayName = "Taper (Eğri)", Unit = null, DataType = "text",
                IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 207, ComponentTypeId = 2, Key = "turns", DisplayName = "Tur Sayısı", Unit = null, DataType = "text",
                IsSearchable = false, HotColumn = null
            },
            // Datasheet'ten gelen seri özellikleri (Adım 4).
            new ParameterDefinition
            {
                Id = 208, ComponentTypeId = 2, Key = "tcr", DisplayName = "TCR", Unit = "ppm/°C",
                DataType = "numeric", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 209, ComponentTypeId = 2, Key = "composition", DisplayName = "Kompozisyon", Unit = null,
                DataType = "text", IsSearchable = true, HotColumn = null
            },

            // --- Diyot (Id=3) ---
            new ParameterDefinition
            {
                Id = 301, ComponentTypeId = 3, Key = "reverse_voltage", DisplayName = "Ters Gerilim (Vr)", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 302, ComponentTypeId = 3, Key = "forward_current", DisplayName = "İleri Akım (If)", Unit = "A",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 303, ComponentTypeId = 3, Key = "forward_voltage", DisplayName = "İleri Gerilim (Vf)", Unit = "V",
                DataType = "numeric", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 304, ComponentTypeId = 3, Key = "subtype", DisplayName = "Alt Tür", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 305, ComponentTypeId = 3, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Transistör (Id=4) ---
            new ParameterDefinition
            {
                Id = 401, ComponentTypeId = 4, Key = "voltage", DisplayName = "Gerilim (Vds/Vce)", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 402, ComponentTypeId = 4, Key = "current", DisplayName = "Akım", Unit = "A", DataType = "numeric",
                IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 403, ComponentTypeId = 4, Key = "power", DisplayName = "Güç", Unit = "W", DataType = "numeric",
                IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 404, ComponentTypeId = 4, Key = "subtype", DisplayName = "Alt Tür", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 405, ComponentTypeId = 4, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Osilatör (Id=5) ---
            new ParameterDefinition
            {
                Id = 501, ComponentTypeId = 5, Key = "frequency", DisplayName = "Frekans", Unit = "Hz",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 502, ComponentTypeId = 5, Key = "supply_voltage", DisplayName = "Besleme Gerilimi", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 503, ComponentTypeId = 5, Key = "frequency_tolerance", DisplayName = "Frekans Toleransı",
                Unit = "ppm", DataType = "numeric", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 504, ComponentTypeId = 5, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                // DigiKey "Package / Case" osilatörde boyutsuz ("4-SMD, No Lead"); gerçek boyut
                // "Size / Dimension" sütununda. Footprint seçimi için boyutu ayrı param olarak tutuyoruz.
                Id = 505, ComponentTypeId = 5, Key = "size", DisplayName = "Boyut", Unit = null, DataType = "text",
                IsSearchable = false, HotColumn = null
            },

            // --- Regülatör (Id=6) ---
            new ParameterDefinition
            {
                Id = 601, ComponentTypeId = 6, Key = "output_voltage", DisplayName = "Çıkış Gerilimi", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 602, ComponentTypeId = 6, Key = "output_current", DisplayName = "Çıkış Akımı", Unit = "A",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 603, ComponentTypeId = 6, Key = "subtype", DisplayName = "Alt Tür", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 604, ComponentTypeId = 6, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Konnektör (Id=7) ---
            new ParameterDefinition
            {
                Id = 701, ComponentTypeId = 7, Key = "positions", DisplayName = "Pozisyon", Unit = null,
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 702, ComponentTypeId = 7, Key = "current", DisplayName = "Akım", Unit = "A",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 703, ComponentTypeId = 7, Key = "voltage", DisplayName = "Gerilim", Unit = "V",
                DataType = "numeric", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 704, ComponentTypeId = 7, Key = "pitch", DisplayName = "Aralık (Pitch)", Unit = null,
                DataType = "text", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 705, ComponentTypeId = 7, Key = "mounting", DisplayName = "Montaj", Unit = null,
                DataType = "text", IsSearchable = false, HotColumn = null
            }
        );

        // Python'da doğrulanan şemalar: CSV 25/25 + üretici doküman örnekleri 15/15.
        // Kaynaklar: Vishay 52007/52009/52011, Ohmite res-40/res-hsx, Yageo RC_L,
        // KOA RK73H, Panasonic AOA0000C304 sipariş bilgisi bölümleri.
        modelBuilder.Entity<MpnProfile>().HasData(
            new MpnProfile
            {
                Id = 1,
                Name = "Vishay Sfernice RCMS",
                Manufacturer = "Vishay Sfernice",
                PatternRegex = @"^RCMS\d{2}(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"B":0.1,"A":0.2,"D":0.5}""",
                TcrMapJson = """{"H":50,"E":25,"D":15}"""
            },
            new MpnProfile
            {
                Id = 2,
                Name = "Vishay Sfernice RLP",
                Manufacturer = "Vishay Sfernice",
                PatternRegex = @"^RLP\d{2}(?<value>[0-9R]{5})(?<tol>[A-Z])",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"J":5.0,"D":0.5}"""
            },
            new MpnProfile
            {
                Id = 3,
                Name = "Ohmite 40 Serisi",
                Manufacturer = "Ohmite",
                PatternRegex = @"^4(?<power>[123570])N?(?<tol>[FJ])(?<value>\d+[RKM]\d*|R\d+|\d+)(?:E)?(?:-T)?$",
                ValueEncoding = "rkm",
                ToleranceMapJson = """{"F":1.0,"J":5.0}""",
                PowerMapJson = """{"1":1.0,"2":2.0,"3":3.0,"5":5.0,"7":7.0,"0":10.0}"""
            },
            new MpnProfile
            {
                Id = 4,
                Name = "Ohmite HSX",
                Manufacturer = "Ohmite",
                PatternRegex = @"^HSX-2[WZ](?<value>\d{4})(?<tol>[A-Z])E$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"J":5.0}"""
            },
            // RCMT/RCMA ayrı satır çünkü boy kodu gücü tek başına belirliyor (P70 tablosu);
            // RCMS'te aynı boyun birden çok güç seçeneği var, o yüzden onda harita yok.
            new MpnProfile
            {
                Id = 5,
                Name = "Vishay Sfernice RCMT",
                Manufacturer = "Vishay Sfernice",
                PatternRegex = @"^RCMT(?<power>\d{2})(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"B":0.1,"A":0.2,"D":0.5}""",
                TcrMapJson = """{"H":50,"E":25,"D":15}""",
                PowerMapJson = """{"01":0.063,"02":0.125,"05":0.25,"08":0.5,"10":1.0,"20":2.0,"40":4.0}"""
            },
            new MpnProfile
            {
                Id = 6,
                Name = "Vishay Sfernice RCMA",
                Manufacturer = "Vishay Sfernice",
                PatternRegex = @"^RCMA(?<power>\d{2})(?<value>[0-9R]{5})(?<tol>[A-Z])(?<tcr>[A-Z])",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"B":0.1,"A":0.2,"D":0.5}""",
                TcrMapJson = """{"H":50,"E":25,"D":15}""",
                PowerMapJson = """{"02":0.125,"05":0.25,"08":0.5,"10":0.75,"20":1.0,"40":2.0}"""
            },
            new MpnProfile
            {
                Id = 7,
                Name = "Yageo RC",
                Manufacturer = "Yageo",
                PatternRegex = @"^RC(?<size>\d{4})(?<tol>[BDFJ])[RKS]-(?:07|10|13|7W|7D|7N|3W)(?<value>\d+[RKM]\d*|R\d+)[A-Z]?$",
                ValueEncoding = "rkm",
                ToleranceMapJson = """{"B":0.1,"D":0.5,"F":1.0,"J":5.0}""",
                // Yageo'da boy kodu paket adının kendisi (RC0603 -> 0603).
                PackageMapJson = """{"0075":"0075","0100":"0100","0201":"0201","0402":"0402","0603":"0603","0805":"0805","1206":"1206","1210":"1210","1218":"1218","2010":"2010","2512":"2512"}"""
            },
            new MpnProfile
            {
                Id = 8,
                Name = "KOA RK73H",
                Manufacturer = "KOA Speer",
                PatternRegex = @"^RK73H(?<size>W3A2|W2H|W3A|1[FHEJ]|2[ABEH]|3A)A?[TGL](?:TX|TBL|TCM|TPL|TP|TD|TE)(?<value>[0-9R]{4})(?<tol>[DF])$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"D":0.5,"F":1.0}""",
                // KOA boy kodu -> EIA paket adı (RK73H datasheet boyut tablosu).
                PackageMapJson = """{"1F":"01005","1H":"0201","1E":"0402","1J":"0603","2A":"0805","2B":"1206","2E":"1210","2H":"2010","W2H":"2010","3A":"2512","W3A":"2512","W3A2":"2512"}"""
            },
            new MpnProfile
            {
                // Stackpole sei-sp.pdf "How to Order": SP3A + J(%5) + T(makara) + değer (R ondalık).
                Id = 10,
                Name = "Stackpole SP",
                Manufacturer = "Stackpole Electronics",
                PatternRegex = @"^SP3A(?<tol>[J])T(?<value>[0-9R]{4})$",
                ValueEncoding = "rkm",
                ToleranceMapJson = """{"J":5.0}"""
            },
            new MpnProfile
            {
                Id = 9,
                Name = "Panasonic ERJ",
                Manufacturer = "Panasonic",
                // Değer 3-4 hane: J (%5) parçaları 3 hane, F/D (%1/%0.5) 4 hane kullanır.
                PatternRegex = @"^ERJ-?[A-Z0-9]{2,4}(?<tol>[DFGJ])(?<value>[0-9R]{3,4})[A-Z]?$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"D":0.5,"F":1.0,"G":2.0,"J":5.0}"""
            },
            // === Araştırmayla eklenen üreticiler (Python'da 18/18 doğrulandı) ===
            // Kaynak: Vishay 20035, Bourns chpreztr, Rohm part-no açıklaması, Walsin WR ASC.
            new MpnProfile
            {
                Id = 11,
                Name = "Vishay Dale CRCW",
                Manufacturer = "Vishay",
                // CRCW + boy(4) + değer(4, RKM) + tol + TCR + paketleme.
                PatternRegex = @"^CRCW(?<size>\d{4})(?<value>[0-9RKM]{4})(?<tol>[FJDZ])(?<tcr>[KNH])[A-Z0-9]*$",
                ValueEncoding = "rkm",
                ToleranceMapJson = """{"F":1.0,"D":0.5,"J":5.0}""",
                TcrMapJson = """{"K":100,"N":200,"H":50}""",
                PackageMapJson = """{"0201":"0201","0402":"0402","0603":"0603","0805":"0805","1206":"1206","1210":"1210","2010":"2010","2512":"2512"}"""
            },
            new MpnProfile
            {
                Id = 12,
                Name = "Bourns CR",
                Manufacturer = "Bourns",
                // CR + boy(4) - tol + paketleme - değer + E(kurşunsuz).
                PatternRegex = @"^CR(?<size>\d{4})-(?<tol>[FGJ])[A-Z]-(?<value>[0-9R]{3,4})E[A-Z]*$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"G":2.0,"J":5.0}""",
                PackageMapJson = """{"0402":"0402","0603":"0603","0805":"0805","1206":"1206","1210":"1210","2010":"2010","2512":"2512"}"""
            },
            new MpnProfile
            {
                Id = 13,
                Name = "Rohm MCR",
                Manufacturer = "ROHM",
                // MCR + boy + paketleme(3) + tol + [X] + değer. Boy kodları metrik/inç
                // karışık olduğu için paket haritası konmadı (yanlış eşleme riski).
                PatternRegex = @"^MCR(?<size>\d{2,3})[A-Z]{3}(?<tol>[FJD])X?(?<value>[0-9R]{3,4})[A-Z]?$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"J":5.0,"D":0.5}"""
            },
            new MpnProfile
            {
                Id = 14,
                Name = "Walsin WR",
                Manufacturer = "Walsin",
                // WR + boy + tip(X/W) + değer + tol + paketleme + L(kurşunsuz).
                PatternRegex = @"^WR(?<size>10|12|08|06|04)[XW](?<value>[0-9R]{3,4})(?<tol>[FJ])[TQGHBDA]L?$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"J":5.0}""",
                PackageMapJson = """{"10":"1210","12":"1206","08":"0805","06":"0603","04":"0402"}"""
            },
            // === Hacim boşluklarını kapatan ek profiller ===
            new MpnProfile
            {
                Id = 15,
                Name = "Samsung RC",
                Manufacturer = "Samsung Electro-Mechanics",
                // RC + boy(metrik,4) + tol + değer + paketleme(CS/ES/AS). Yageo RC'den
                // ayrışır: Yageo'da tolerans sonrası [RKS]-07.. yapısı var, burada yok.
                PatternRegex = @"^RC(?<size>\d{4})(?<tol>[DFGJ])(?<value>[0-9R]{3,4})(?:CS|ES|AS)$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"D":0.5,"F":1.0,"G":2.0,"J":5.0}""",
                PackageMapJson = """{"0402":"01005","0603":"0201","1005":"0402","1608":"0603","2012":"0805","3216":"1206","3225":"1210","5025":"2010","6432":"2512"}"""
            },
            new MpnProfile
            {
                // Royal Ohm/Uni-Royal ve birçok Çinli üreticinin paylaştığı ortak şema.
                // Şema paylaşımlı olduğu için ÜRETİCİ boş bırakılır (tahmin edilemez);
                // değer + tolerans + paket yine de çıkar.
                Id = 16,
                Name = "Standart boy-W kodu",
                Manufacturer = "",
                PatternRegex = @"^(?<size>0201|0402|0603|0805|1206|1210|2010|2512)W[0-9A-Z](?<tol>[FGJD])(?<value>[0-9R]{3,4})T[0-9A-Z]*$",
                ValueEncoding = "sig-zeros-R",
                ToleranceMapJson = """{"F":1.0,"G":2.0,"J":5.0,"D":0.5}""",
                PackageMapJson = """{"0402":"0402","0603":"0603","0805":"0805","1206":"1206","1210":"1210","2010":"2010","2512":"2512"}"""
            }
        );
    }
}