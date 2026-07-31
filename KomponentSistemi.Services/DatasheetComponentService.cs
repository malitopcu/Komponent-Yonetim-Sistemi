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
        public int ComponentTypeId { get; set; }
        public string Mpn { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public double? PrimaryValueSi { get; set; }
        public double? SecondaryValueSi { get; set; }
        // HotColumn dışı parametreler (key → sayısal SI değer ya da metin).
        public Dictionary<string, object?> Params { get; set; } = new();
    }

    // Tipe bağımsız kayıt: CSV import ile aynı upsert anahtarı (Mpn + Manufacturer).
    // Ok=true ise mesaj kısa onay, false ise ekranda kalması gereken hata.
    public async Task<(bool Ok, string Message)> SaveAsync(SaveRequest req)
    {
        string mpn = req.Mpn.Trim();
        string manufacturer = req.Manufacturer.Trim();
        if (mpn.Length == 0) return (false, "MPN boş olamaz.");
        if (req.ComponentTypeId <= 0) return (false, "Önce komponent tipini seç.");
        if (req.PrimaryValueSi is null) return (false, "Birincil değer boş olamaz (MPN çözülemediyse elle gir).");

        string paramsJson = System.Text.Json.JsonSerializer.Serialize(req.Params);

        var existing = await _db.Components
            .FirstOrDefaultAsync(c => c.Mpn == mpn && c.Manufacturer == manufacturer);

        if (existing is null)
        {
            _db.Components.Add(new Component
            {
                ComponentTypeId = req.ComponentTypeId,
                Mpn = mpn,
                Manufacturer = manufacturer,
                PrimaryValueSi = req.PrimaryValueSi,
                SecondaryValueSi = req.SecondaryValueSi,
                ParamsJson = paramsJson,
            });
            await _db.SaveChangesAsync();
            return (true, $"Eklendi: {mpn} ({manufacturer})");
        }

        existing.ComponentTypeId = req.ComponentTypeId;
        existing.PrimaryValueSi = req.PrimaryValueSi;
        existing.SecondaryValueSi = req.SecondaryValueSi;
        existing.ParamsJson = paramsJson;
        await _db.SaveChangesAsync();
        return (true, $"Güncellendi: {mpn} ({manufacturer})");
    }
}
