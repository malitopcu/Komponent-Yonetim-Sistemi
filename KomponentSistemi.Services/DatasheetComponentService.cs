using System.Globalization;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

// MPN'den komponent oluşturma akışı: çözme (Komponent Ekle formunun hızlandırıcısı)
// + kayıt. Kayıt CSV import ile aynı upsert anahtarını (Mpn + Manufacturer) kullanır.
// Bilinmeyen alanlar null kalır, uydurulmaz.
public class DatasheetComponentService
{
    private readonly AppDbContext _db;
    private readonly ValueNormalizer _normalizer = new();

    public DatasheetComponentService(AppDbContext db)
    {
        _db = db;
    }

    public class DecodeResult
    {
        public DecodedMpn? Decoded { get; set; }
        // Profil çözemeyince standart kalıplardan (RKM, hane kodu) çıkan adaylar.
        public List<(double Ohms, string Reason)> Suggestions { get; } = new();
    }

    // MPN'i bilinen üretici profilleriyle çözer; olmazsa standart kalıp önerisi verir.
    public DecodeResult DecodeMpn(string? mpn)
    {
        var r = new DecodeResult();
        if (string.IsNullOrWhiteSpace(mpn)) return r;

        r.Decoded = MpnDecoder.TryDecode(mpn, _db.MpnProfiles.ToList());
        if (r.Decoded is null)
            r.Suggestions.AddRange(MpnDecoder.SuggestValues(mpn));
        return r;
    }

    public class SaveRequest
    {
        public string Mpn { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public double? Ohms { get; set; }
        public double? PowerW { get; set; }
        public double? TolerancePercent { get; set; }
        public int? TcrPpm { get; set; }
        public string? Composition { get; set; }
        public string? Package { get; set; }
    }

    // Ok=true ise mesaj kısa onay ("Eklendi"), false ise ekranda kalması gereken hata.
    public async Task<(bool Ok, string Message)> SaveAsync(SaveRequest req)
    {
        string mpn = req.Mpn.Trim();
        string manufacturer = req.Manufacturer.Trim();
        if (mpn.Length == 0) return (false, "MPN boş olamaz.");
        if (req.Ohms is null) return (false, "Direnç değeri boş olamaz (MPN çözülemediyse elle gir).");

        var jsonParams = new Dictionary<string, object?>();
        if (req.TolerancePercent is double tol)
            jsonParams["tolerance"] = _normalizer.NormalizeCategorical(
                "±" + tol.ToString(CultureInfo.InvariantCulture) + "%");
        if (req.TcrPpm is int tcr) jsonParams["tcr"] = tcr;
        if (_normalizer.NormalizeCategorical(req.Composition) is string comp)
            jsonParams["composition"] = comp;
        if (_normalizer.NormalizeCategorical(req.Package) is string pkg)
            jsonParams["package"] = pkg;

        string paramsJson = System.Text.Json.JsonSerializer.Serialize(jsonParams);

        var existing = await _db.Components
            .FirstOrDefaultAsync(c => c.Mpn == mpn && c.Manufacturer == manufacturer);

        if (existing is null)
        {
            _db.Components.Add(new Component
            {
                ComponentTypeId = 2, // Direnç — bu form şimdilik dirençlere özel
                Mpn = mpn,
                Manufacturer = manufacturer,
                PrimaryValueSi = req.Ohms,
                SecondaryValueSi = req.PowerW,
                ParamsJson = paramsJson,
            });
            await _db.SaveChangesAsync();
            return (true, $"Eklendi: {mpn} ({manufacturer})");
        }

        existing.PrimaryValueSi = req.Ohms;
        existing.SecondaryValueSi = req.PowerW;
        existing.ParamsJson = paramsJson;
        await _db.SaveChangesAsync();
        return (true, $"Güncellendi: {mpn} ({manufacturer})");
    }
}
