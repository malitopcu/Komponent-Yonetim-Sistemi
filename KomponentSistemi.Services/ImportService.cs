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

    private Dictionary<string, ParameterDefinition> LoadParameters(int componentTypeId)
    {
        return _db.ParameterDefinitions
            .Where(p => p.ComponentTypeId == componentTypeId)
            .ToDictionary(p => p.Key, p => p);
    }

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

            string? identity = SynonymDictionary.ResolveIdentity(header);
            if (identity == "Mpn")          { comp.Mpn = Clean(rawValue); continue; }
            if (identity == "Manufacturer") { comp.Manufacturer = Clean(rawValue); continue; }

            if (SynonymDictionary.IsRohsHeader(header))
            {
                var r = SynonymDictionary.NormalizeRohs(rawValue);
                if (!string.IsNullOrEmpty(r)) comp.Rohs = r;
                continue;
            }

            string? paramKey = SynonymDictionary.ResolveParameter(header);
            if (paramKey == "package" && SynonymDictionary.IsPackagingValue(rawValue))
                continue;   // "Tape & Reel" gibi paketleme değeri → kasa/paket değil, atla
            if (paramKey != null && paramDefs.TryGetValue(paramKey, out var def))
            {
                if (def.DataType == "numeric")
                {
                    double? num = _normalizer.NormalizeNumeric(rawValue);
                    if (def.HotColumn == "primary")        comp.PrimaryValueSi = num;
                    else if (def.HotColumn == "secondary") comp.SecondaryValueSi = num;
                    else if (num.HasValue)                 jsonParams[paramKey] = num;
                }
                else
                {
                    var cat = _normalizer.NormalizeCategorical(rawValue);
                    if (cat != null)
                        jsonParams[paramKey] = cat;
                }
                continue;
            }

            if (SynonymDictionary.ResolveOffer(header) != null) continue;
            if (SynonymDictionary.IsIgnorable(header)) continue;   // bilinen meta sütun → sessizce atla

            unmapped.Add(header);
        }

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

    private static string Clean(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("=\"") && raw.EndsWith("\""))
            raw = raw.Substring(2, raw.Length - 3);
        return raw.Trim();
    }

    public class ImportResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int BatchId { get; set; }
        public List<string> UnmappedHeaders { get; set; } = new();
        public List<string> MissingExpected { get; set; } = new();   // tipte olması beklenen ama dosyada olmayan parametreler
        public List<string> Errors { get; set; } = new();
    }

    public async Task<ImportResult> ImportAsync(string filePath, int componentTypeId, string source, string? defaultCurrency = null, Dictionary<string, string>? fixedParams = null, string? note = null)
    {
        var result = new ImportResult();
        var paramDefs = LoadParameters(componentTypeId);

        var reader = new CsvImportReader();
        var readResult = reader.Read(filePath);
        result.Errors.AddRange(readResult.Errors);

        // Bu tipin beklenen çekirdek parametreleri (primary/secondary) dosyada var mı?
        var headers = readResult.Rows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
        var mappedKeys = headers
            .Select(SynonymDictionary.ResolveParameter)
            .Where(k => k != null).Select(k => k!)
            .ToHashSet();
        foreach (var def in paramDefs.Values.Where(d => d.HotColumn == "primary" || d.HotColumn == "secondary"))
            if (!mappedKeys.Contains(def.Key))
                result.MissingExpected.Add(def.DisplayName);

        var unmappedSet = new HashSet<string>();
        var pending = new List<PendingChange>();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var row in readResult.Rows)
        {
            var unmapped = new List<string>();
            var comp = BuildComponent(row, componentTypeId, paramDefs, unmapped, fixedParams);
            foreach (var h in unmapped) unmappedSet.Add(h);

            if (string.IsNullOrWhiteSpace(comp.Mpn))
            {
                result.Errors.Add("MPN'siz satır atlandı.");
                continue;
            }

            var existing = await _db.Components
                .Include(c => c.Offers)
                .FirstOrDefaultAsync(c => c.Mpn == comp.Mpn && c.Manufacturer == comp.Manufacturer);

            if (existing == null)
            {
                _db.Components.Add(comp);
                pending.Add(new PendingChange("Component", () => comp.Id, "Added", null));

                var added = UpsertOffer(comp, row, source, defaultCurrency);
                pending.Add(new PendingChange("Offer", () => added.offer.Id, added.wasUpdate ? "Updated" : "Added", added.prevJson));
                result.Added++;
            }
            else
            {
                var compPrev = System.Text.Json.JsonSerializer.Serialize(
                    new ComponentPrev(existing.PrimaryValueSi, existing.SecondaryValueSi, existing.ParamsJson, existing.Rohs));
                existing.PrimaryValueSi = comp.PrimaryValueSi;
                existing.SecondaryValueSi = comp.SecondaryValueSi;
                existing.ParamsJson = comp.ParamsJson;
                if (!string.IsNullOrEmpty(comp.Rohs)) existing.Rohs = comp.Rohs;
                pending.Add(new PendingChange("Component", () => existing.Id, "Updated", compPrev));

                var up = UpsertOffer(existing, row, source, defaultCurrency);
                pending.Add(new PendingChange("Offer", () => up.offer.Id, up.wasUpdate ? "Updated" : "Added", up.prevJson));
                result.Updated++;
            }
        }

        await _db.SaveChangesAsync();   // id'ler burada atanır

        // İçe aktarma partisini + tekil değişiklikleri kaydet (geri alma için).
        var typeName = (await _db.ComponentTypes.FindAsync(componentTypeId))?.Name ?? "";
        var batch = new ImportBatch
        {
            FileName = System.IO.Path.GetFileName(filePath),
            Source = source,
            TypeName = typeName,
            ImportedAt = DateTime.Now,
            AddedCount = result.Added,
            UpdatedCount = result.Updated,
            Note = note?.Trim() ?? ""
        };
        foreach (var p in pending)
            batch.Changes.Add(new ImportChange
            {
                EntityType = p.EntityType,
                EntityId = p.GetId(),
                Operation = p.Operation,
                PrevJson = p.PrevJson
            });
        _db.ImportBatches.Add(batch);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        result.UnmappedHeaders = unmappedSet.ToList();
        result.BatchId = batch.Id;
        return result;
    }

    // Teklifi ekler ya da günceller. Döner: (teklif, güncelleme miydi, güncellemeyse eski değerlerin JSON'u).
    private (Offer offer, bool wasUpdate, string? prevJson) UpsertOffer(Component comp, Dictionary<string, string> row, string source, string? defaultCurrency)
    {
        double? price = null;
        string? sourcePartNo = null;
        string? currency = null;

        foreach (var cell in row)
        {
            string? offerField = SynonymDictionary.ResolveOffer(cell.Key);
            if (offerField == "Price")
            {
                price = _normalizer.NormalizeNumeric(cell.Value);
                currency = ValueNormalizer.ExtractCurrency(cell.Value);
            }
            if (offerField == "SourcePartNo") sourcePartNo = Clean(cell.Value);
        }

        currency ??= defaultCurrency;

        // Aynı satıcı (boşluk/büyük-küçük harf farkını yok say) → yeni teklif açma, güncelle.
        var offer = comp.Offers.FirstOrDefault(o =>
            string.Equals(o.Source?.Trim(), source?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (offer == null)
        {
            offer = new Offer
            {
                Source = source,
                SourcePartNo = sourcePartNo ?? "",
                Price = price,
                Currency = currency,
                PriceUpdatedAt = price.HasValue ? DateTime.UtcNow : null
            };
            comp.Offers.Add(offer);
            return (offer, false, null);
        }

        // Güncelleme: önce eski hali sakla (geri alma için).
        var prev = System.Text.Json.JsonSerializer.Serialize(
            new OfferPrev(offer.Price, offer.Currency, offer.SourcePartNo, offer.PriceUpdatedAt));

        offer.Price = price;
        offer.Currency = currency ?? offer.Currency;
        offer.SourcePartNo = sourcePartNo ?? offer.SourcePartNo;
        offer.PriceUpdatedAt = price.HasValue ? DateTime.UtcNow : offer.PriceUpdatedAt;
        return (offer, true, prev);
    }

    // İçe aktarma sırasında biriktirilen değişiklik (id'ler save sonrası okunur).
    private class PendingChange
    {
        public string EntityType;
        public Func<int> GetId;
        public string Operation;
        public string? PrevJson;
        public PendingChange(string entityType, Func<int> getId, string operation, string? prevJson)
        {
            EntityType = entityType;
            GetId = getId;
            Operation = operation;
            PrevJson = prevJson;
        }
    }

    // OTONOMİ: dosyanın başlıklarına bakıp en olası komponent tipini önerir.
    // Mantık: başlıkları sözlükten geçir → parametre anahtarları çıkar →
    // hangi tipin tanımları en çok eşleşiyorsa o tip önerilir.
    public async Task<ComponentTypeDto?> SuggestTypeAsync(string filePath)
    {
        var reader = new CsvImportReader();
        var readResult = reader.Read(filePath);

        var firstRow = readResult.Rows.FirstOrDefault();
        if (firstRow == null) return null;

        var keys = firstRow.Keys
            .Select(SynonymDictionary.ResolveParameter)
            .Where(k => k != null)
            .Select(k => k!)
            .ToHashSet();

        if (keys.Count == 0) return null;

        var best = await _db.ParameterDefinitions
            .Where(p => keys.Contains(p.Key))
            .GroupBy(p => p.ComponentTypeId)
            .Select(g => new { TypeId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync();

        if (best == null) return null;

        var type = await _db.ComponentTypes.FindAsync(best.TypeId);
        return type == null ? null : new ComponentTypeDto { Id = type.Id, Name = type.Name };
    }
}