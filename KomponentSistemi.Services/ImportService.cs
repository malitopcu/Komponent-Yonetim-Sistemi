using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Services;

public class ImportService
{
    private readonly AppDbContext _db;
    private readonly ValueNormalizer _normalizer = new();

    public ImportService(AppDbContext db)
    {
        _db = db;
    }

    // Bir tipin parametrelerini (Key → tanım) veritabanından yükler.
    private Dictionary<string, ParameterDefinition> LoadParameters(int componentTypeId)
    {
        return _db.ParameterDefinitions
            .Where(p => p.ComponentTypeId == componentTypeId)
            .ToDictionary(p => p.Key, p => p);
    }

    // Bir ham satırı (sütun adı → metin) bir Component nesnesine çevirir.
    // Eşlenemeyen sütunları 'unmapped' listesine yazar.
    private Component BuildComponent(
        Dictionary<string, string> row,
        int componentTypeId,
        Dictionary<string, ParameterDefinition> paramDefs,
        List<string> unmapped,
        Dictionary<string, string>? fixedParams)
    {
        var comp = new Component { ComponentTypeId = componentTypeId };
        var jsonParams = new Dictionary<string, object?>();

        foreach (var cell in row)
        {
            string header = cell.Key;
            string rawValue = cell.Value;

            // 1) Kimlik alanı mı? (MPN, üretici → doğrudan Component sütunu)
            string? identity = SynonymDictionary.ResolveIdentity(header);
            if (identity == "Mpn")          { comp.Mpn = Clean(rawValue); continue; }
            if (identity == "Manufacturer") { comp.Manufacturer = Clean(rawValue); continue; }

            // 2) Parametre mi?
            string? paramKey = SynonymDictionary.ResolveParameter(header);
            if (paramKey != null && paramDefs.TryGetValue(paramKey, out var def))
            {
                if (def.DataType == "numeric")
                {
                    double? num = _normalizer.NormalizeNumeric(rawValue);
                    if (def.HotColumn == "primary")        comp.PrimaryValueSi = num;
                    else if (def.HotColumn == "secondary") comp.SecondaryValueSi = num;
                    else if (num.HasValue)                 jsonParams[paramKey] = num;
                }
                else // text / kategorik
                {
                    var cat = _normalizer.NormalizeCategorical(rawValue);
                    if (cat != null)
                        jsonParams[paramKey] = cat;
                }
                continue;
            }

            // 3) Offer alanı mı? (fiyat, kaynak parça no) — AddOrUpdateOffer'da işlenir
            if (SynonymDictionary.ResolveOffer(header) != null) continue;

            // 4) Hiçbiri → eşlenemedi
            unmapped.Add(header);
        }

        // Sabit parametreler: dosyada sütunu olmayan ama içe aktaranın bildiği
        // bilgiler (örn. Zener dosyası → subtype=ZENER). CSV'den gelen kazanır.
        if (fixedParams != null)
        {
            foreach (var (k, v) in fixedParams)
            {
                if (paramDefs.ContainsKey(k) && !jsonParams.ContainsKey(k))
                    jsonParams[k] = _normalizer.NormalizeCategorical(v);
            }
        }

        comp.ParamsJson = System.Text.Json.JsonSerializer.Serialize(jsonParams);
        return comp;
    }

    // Excel kaçışını temizleyen basit yardımcı
    private static string Clean(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("=\"") && raw.EndsWith("\""))
            raw = raw.Substring(2, raw.Length - 3);
        return raw.Trim();
    }

    // İçe aktarma sonucunu paketleyen yapı
    public class ImportResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public List<string> UnmappedHeaders { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    // Ana metot: dosyayı okur, komponentleri kurar, upsert eder, kaydeder.
    public async Task<ImportResult> ImportAsync(string filePath, int componentTypeId, string source, string? defaultCurrency = null, Dictionary<string, string>? fixedParams = null)
    {
        var result = new ImportResult();
        var paramDefs = LoadParameters(componentTypeId);

        // 1) Dosyayı oku
        var reader = new CsvImportReader();
        var readResult = reader.Read(filePath);
        result.Errors.AddRange(readResult.Errors);

        var unmappedSet = new HashSet<string>();

        // 2) Transaction başlat — ya hepsi ya hiçbiri
        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var row in readResult.Rows)
        {
            var unmapped = new List<string>();
            var comp = BuildComponent(row, componentTypeId, paramDefs, unmapped, fixedParams);
            foreach (var h in unmapped) unmappedSet.Add(h);

            // MPN yoksa bu satırı atla (kimliksiz komponent olmaz)
            if (string.IsNullOrWhiteSpace(comp.Mpn))
            {
                result.Errors.Add("MPN'siz satır atlandı.");
                continue;
            }

            // 3) Upsert: bu MPN + üretici zaten var mı?
            var existing = await _db.Components
                .Include(c => c.Offers)
                .FirstOrDefaultAsync(c => c.Mpn == comp.Mpn && c.Manufacturer == comp.Manufacturer);

            if (existing == null)
            {
                // Yeni komponent — comp burada Add ile resmi kayda giriyor
                _db.Components.Add(comp);
                AddOrUpdateOffer(comp, row, source, defaultCurrency);
                result.Added++;
            }
            else
            {
                // Var olanı güncelle (sıcak değerler + JSON tazelenir)
                existing.PrimaryValueSi = comp.PrimaryValueSi;
                existing.SecondaryValueSi = comp.SecondaryValueSi;
                existing.ParamsJson = comp.ParamsJson;
                AddOrUpdateOffer(existing, row, source, defaultCurrency);
                result.Updated++;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        result.UnmappedHeaders = unmappedSet.ToList();
        return result;
    }

    // Bir komponente, kaynağa göre teklif ekler ya da mevcut teklifi günceller.
    private void AddOrUpdateOffer(Component comp, Dictionary<string, string> row, string source, string? defaultCurrency)
    {
        // Satırdan fiyat, para birimi ve kaynak parça no'yu bul
        double? price = null;
        string? sourcePartNo = null;
        string? currency = null;

        foreach (var cell in row)
        {
            string? offerField = SynonymDictionary.ResolveOffer(cell.Key);
            if (offerField == "Price")
            {
                price = _normalizer.NormalizeNumeric(cell.Value);
                currency = ValueNormalizer.ExtractCurrency(cell.Value); // "0,12 €" → "EUR"
            }
            if (offerField == "SourcePartNo") sourcePartNo = Clean(cell.Value);
        }

        // Öncelik: hücredeki işaret > kullanıcının verdiği varsayılan > null
        currency ??= defaultCurrency;

        // Bu kaynaktan zaten teklif var mı?
        var offer = comp.Offers.FirstOrDefault(o => o.Source == source);
        if (offer == null)
        {
            comp.Offers.Add(new Offer
            {
                Source = source,
                SourcePartNo = sourcePartNo ?? "",
                Price = price,
                Currency = currency,
                PriceUpdatedAt = price.HasValue ? DateTime.UtcNow : null
            });
        }
        else
        {
            offer.Price = price;
            offer.Currency = currency ?? offer.Currency;   // yeni bilgi yoksa eskisini koru
            offer.SourcePartNo = sourcePartNo ?? offer.SourcePartNo;
            offer.PriceUpdatedAt = price.HasValue ? DateTime.UtcNow : offer.PriceUpdatedAt;
        }
    }
}