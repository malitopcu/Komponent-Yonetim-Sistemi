using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KomponentSistemi.Services;

namespace KomponentSistemi.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ComponentQueryService _query;
    private readonly SearchService _search;
    private readonly BomService _bom;
    private readonly DataManagementService _mgmt;
    private readonly ValueNormalizer _normalizer = new();

    private int _bomListId;

    [ObservableProperty] private string? _schematicPath;
    [ObservableProperty] private string _kiCadStatus = "";
    public ObservableCollection<KiCadMatchRow> KiCadRows { get; } = new();

    [ObservableProperty] private string? _schematicBomPath;
    [ObservableProperty] private string _schematicBomStatus = "";
    public ObservableCollection<SchematicBomRow> SchematicBomRows { get; } = new();

    // Komponent Ekle formu — tip seçilince alanlar dinamik kurulur; MPN opsiyonel hızlandırıcı.
    [ObservableProperty] private ComponentTypeDto? _addType;
    [ObservableProperty] private string? _addMpn;
    [ObservableProperty] private string? _addManufacturer;
    [ObservableProperty] private string _addStatus = "";
    public ObservableCollection<ComponentFieldVm> AddFields { get; } = new();

    // Tip uyuşmazlığı: seçili tip ile MPN'in çözüldüğü tip farklıysa uyar.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTypeMismatch))]
    private ComponentTypeDto? _mismatchType;
    public bool HasTypeMismatch => MismatchType != null;

    [ObservableProperty] private ComponentDetailDto? _compareA;
    [ObservableProperty] private ComponentDetailDto? _compareB;
    public ObservableCollection<CompareRow> CompareRows { get; } = new();

    public ObservableCollection<ComponentSummaryDto> Components { get; } = new();
    public ObservableCollection<ComponentTypeDto> Types { get; } = new();
    public ObservableCollection<ComponentTypeDto> FilterTypes { get; } = new();

    // Ör. "Direnç (Ω)", "Güç (W)"
    private readonly Dictionary<int, (string primary, string secondary)> _typeHeaders = new();
    [ObservableProperty] private string _primaryHeader = "Değer";
    [ObservableProperty] private string _secondaryHeader = "2. Değer";

    [ObservableProperty] private bool _hasTypeInfo;
    [ObservableProperty] private string _typeInfoTitle = "";
    [ObservableProperty] private string _typeInfoBody = "";

    [ObservableProperty] private ComponentTypeDto? _selectedType;
    [ObservableProperty] private string? _searchText;

    // Alt tür filtresi (arama panosu). İlk öğe "(Tümü)" = filtre kapalı.
    private const string AllSubtypes = "(Tümü)";
    public ObservableCollection<string> Subtypes { get; } = new();
    [ObservableProperty] private string? _selectedSubtype;

    // Tip değişince o tipin alt türlerini envanterden tazele.
    partial void OnSelectedTypeChanged(ComponentTypeDto? value) => _ = RefreshSubtypesAsync();

    private async Task RefreshSubtypesAsync()
    {
        int? tid = (SelectedType == null || SelectedType.Id == 0) ? null : SelectedType.Id;
        var subs = await _query.GetSubtypesAsync(tid);
        Subtypes.Clear();
        Subtypes.Add(AllSubtypes);
        foreach (var s in subs) Subtypes.Add(s);
        SelectedSubtype = AllSubtypes;
    }

    [ObservableProperty] private string _resultCountText = "";

    // Her tuşta sorgu atmamak için 300 ms bekliyoruz; yeni tuş öncekini iptal eder.
    private System.Threading.CancellationTokenSource? _searchCts;

    partial void OnSearchTextChanged(string? value)
    {
        _searchCts?.Cancel();
        var cts = new System.Threading.CancellationTokenSource();
        _searchCts = cts;
        _ = LiveSearchAsync(cts.Token);
    }

    private async Task LiveSearchAsync(System.Threading.CancellationToken ct)
    {
        try { await Task.Delay(300, ct); }
        catch (System.OperationCanceledException) { return; }
        if (ct.IsCancellationRequested) return;
        await SearchAsync();
    }
    [ObservableProperty] private string? _minPrimaryText;
    [ObservableProperty] private string? _maxPrimaryText;
    [ObservableProperty] private string? _minSecondaryText;
    [ObservableProperty] private string? _maxSecondaryText;

    [ObservableProperty] private string _status = "Yükleniyor...";

    [ObservableProperty] private ComponentSummaryDto? _selectedRow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDetail))]
    private ComponentDetailDto? _detail;

    public bool HasDetail => Detail != null;

    [ObservableProperty] private ComponentTypeDto? _importType;
    [ObservableProperty] private string? _importSource;
    [ObservableProperty] private string? _importCurrency;
    [ObservableProperty] private string? _importSubtype;
    [ObservableProperty] private string? _importNote;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImportReport))]
    private string? _importReport;

    public bool HasImportReport => !string.IsNullOrEmpty(ImportReport);

    // Seçildi ama henüz içe aktarılmadı.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStagedFile))]
    [NotifyPropertyChangedFor(nameof(StagedFileName))]
    private string? _stagedFilePath;

    public bool HasStagedFile => !string.IsNullOrEmpty(StagedFilePath);
    public string StagedFileName => string.IsNullOrEmpty(StagedFilePath) ? "" : System.IO.Path.GetFileName(StagedFilePath);

    public ObservableCollection<BomRowDto> BomRows { get; } = new();

    public ObservableCollection<BomListDto> BomLists { get; } = new();
    [ObservableProperty] private BomListDto? _selectedBomList;
    [ObservableProperty] private string? _bomNameInput;      // seçili projenin adı (yeniden adlandırma)
    [ObservableProperty] private string? _newProjectName;    // yeni proje oluşturma kutusu

    public ObservableCollection<ImportBatchDto> Batches { get; } = new();
    [ObservableProperty] private ImportBatchDto? _selectedBatch;

    public ObservableCollection<ManagedComponentRow> ManagedComponents { get; } = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasManagedComponent))]
    private ManagedComponentRow? _selectedManagedComponent;
    public bool HasManagedComponent => SelectedManagedComponent != null;
    [ObservableProperty] private string? _manageSearchText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedManageCountText))]
    private int _selectedManageCount;
    public string SelectedManageCountText =>
        SelectedManageCount == 0 ? "Silmek için işaretle" : $"{SelectedManageCount} komponent seçili";

    [ObservableProperty] private bool _isConfirmingDelete;
    [ObservableProperty] private string _deleteConfirmMessage = "";

    [ObservableProperty] private bool _isConfirmingProjectDelete;
    [ObservableProperty] private string _projectDeleteMessage = "";

    public ObservableCollection<ManagedOfferDto> ManagedOffers { get; } = new();
    [ObservableProperty] private ManagedOfferDto? _selectedManagedOffer;

    [ObservableProperty] private string _bomName = "";
    [ObservableProperty] private string _bomTotalsDisplay = "—";
    [ObservableProperty] private string _bomPricelessNote = "";

    [ObservableProperty] private BomRowDto? _selectedBomRow;

    // "Otomatik": temel para birimini BOM'un kendi çoğunluğundan bul.
    private const string AutoCurrency = "Otomatik (çoğunluk)";

    public ObservableCollection<string> Currencies { get; } = new();
    [ObservableProperty] private string? _preferredCurrency;

    // İlk yükleme sırasında tercih atanınca gereksiz/çakışan yenileme olmasın diye.
    private bool _bomReady;

    [ObservableProperty] private int _addQuantity = 1;
    [ObservableProperty] private string? _addReferences;

    public MainWindowViewModel(ComponentQueryService query, SearchService search, BomService bom, DataManagementService mgmt)
    {
        _query = query;
        _search = search;
        _bom = bom;
        _mgmt = mgmt;
        _ = LoadAsync();
    }

    partial void OnSelectedRowChanged(ComponentSummaryDto? value)
    {
        _ = LoadDetailAsync(value);
    }

    // Seçim anında değil arama sonrası çağrılıyor ki başlık sonuçlarla birlikte değişsin.
    private void UpdateHeaders(int? typeId)
    {
        if (typeId is int id && _typeHeaders.TryGetValue(id, out var h))
        {
            PrimaryHeader = h.primary;
            SecondaryHeader = h.secondary;
        }
        else
        {
            PrimaryHeader = "Değer";
            SecondaryHeader = "2. Değer";
        }
    }

    private void UpdateTypeInfo(int? typeId)
    {
        var info = typeId is int id ? TypeInfoContent.Get(id) : null;
        if (info.HasValue)
        {
            TypeInfoTitle = info.Value.Title;
            TypeInfoBody = info.Value.Body;
            HasTypeInfo = true;
        }
        else
        {
            HasTypeInfo = false;
        }
    }

    private async Task LoadDetailAsync(ComponentSummaryDto? row)
    {
        Detail = row == null ? null : await _query.GetDetailAsync(row.Id);
    }

    // Detay panelini kapat. Seçimi de bırakıyoruz ki aynı satıra tekrar tıklayınca yeniden açılsın.
    [RelayCommand]
    private void CloseDetail()
    {
        Detail = null;
        SelectedRow = null;
        SelectedBomRow = null;
    }

    // Arama sonuçlarıyla aynı detay panelini besliyor.
    partial void OnSelectedBomRowChanged(BomRowDto? value)
        => _ = LoadDetailByIdAsync(value?.ComponentId);

    private async Task LoadDetailByIdAsync(int? componentId)
    {
        Detail = componentId is null ? null : await _query.GetDetailAsync(componentId.Value);
    }

    [RelayCommand]
    private void KiCadPreview()
    {
        KiCadRows.Clear();
        if (string.IsNullOrWhiteSpace(SchematicPath)) { KiCadStatus = "Önce bir .kicad_sch dosyası seç."; return; }

        var res = new KiCadMatchService().Match(SchematicPath, _bomListId);
        foreach (var r in res.Rows) KiCadRows.Add(r);
        int found = res.Rows.Count(r => r.Status == KiCadMatchStatus.FootprintFound);
        KiCadStatus = res.Errors.Count > 0
            ? string.Join(" | ", res.Errors)
            : $"{found} sembol footprint aldı ({KiCadRows.Count} satır). 'Şemaya Yaz' ile aktar.";
    }

    [RelayCommand]
    private void KiCadWrite()
    {
        if (string.IsNullOrWhiteSpace(SchematicPath)) { KiCadStatus = "Önce bir .kicad_sch dosyası seç."; return; }

        var res = new KiCadWriteService().Write(SchematicPath, _bomListId);
        KiCadStatus = res.Errors.Count > 0
            ? string.Join(" | ", res.Errors)
            : $"{res.Written} footprint yazıldı ✓  Yedek: {System.IO.Path.GetFileName(res.BackupPath)}";
    }

    // Şemadan BOM: sembolleri yorumla, her biri için aday komponent öner.
    [RelayCommand]
    private void SuggestSchematicBom()
    {
        SchematicBomRows.Clear();
        if (string.IsNullOrWhiteSpace(SchematicBomPath)) { SchematicBomStatus = "Önce bir .kicad_sch dosyası seç."; return; }

        var suggestions = new SchematicBomMatcher().Suggest(SchematicBomPath, out var errors);

        foreach (var g in suggestions)
        {
            bool auto = g.Status is SchematicSuggestStatus.ExactValue
                     or SchematicSuggestStatus.NearValue
                     or SchematicSuggestStatus.MpnMatch;

            var row = new SchematicBomRow
            {
                Reference = g.Reference,
                TypeName = g.ComponentTypeId is null ? "?" : g.TypeName,
                ValueLabel = ValueLabelFor(g),
                StatusLabel = StatusLabelFor(g.Status),
                Candidates = g.Candidates
            };
            row.SelectedCandidate = auto ? g.Candidates.FirstOrDefault() : null;
            row.Include = auto;
            SchematicBomRows.Add(row);
        }

        int ready = SchematicBomRows.Count(r => r.Include);
        SchematicBomStatus = errors.Count > 0
            ? string.Join(" | ", errors)
            : $"{SchematicBomRows.Count} sembol yorumlandı, {ready} tanesi hazır. Adayları gözden geçir, 'Seçilenleri BOM'a Ekle' de.";
    }

    // İşaretli satırları seçili adaylarıyla aktif projeye ekler.
    [RelayCommand]
    private async Task AddSchematicBomToProjectAsync()
    {
        var picked = SchematicBomRows.Where(r => r.Include && r.SelectedCandidate != null).ToList();
        if (picked.Count == 0) { SchematicBomStatus = "Eklenecek işaretli satır yok."; return; }

        foreach (var r in picked)
            await _bom.AddItemAsync(_bomListId, r.SelectedCandidate!.ComponentId, 1, r.Reference);

        await RefreshBomListsAsync();
        SelectBomListById(_bomListId);
        await RefreshBomAsync();

        int skipped = SchematicBomRows.Count - picked.Count;
        SchematicBomStatus = $"{picked.Count} komponent projeye eklendi" + (skipped > 0 ? $", {skipped} atlandı." : ".");
    }

    // Tip programatik değiştirilirken (tip değiştir düğmesi) rebuild'i biz yönetiyoruz.
    private bool _suppressAddRebuild;

    // Tip seçilince o tipin alanlarını ParameterDefinitions'tan yeniden kur.
    partial void OnAddTypeChanged(ComponentTypeDto? value)
    {
        MismatchType = null;
        if (_suppressAddRebuild) return;
        _ = RebuildAddFieldsAsync(value);
    }

    private async Task RebuildAddFieldsAsync(ComponentTypeDto? type)
    {
        AddFields.Clear();
        if (type is null) return;

        var defs = await _query.GetParametersAsync(type.Id);
        foreach (var d in defs)
        {
            bool numeric = d.DataType == "numeric";
            string label = string.IsNullOrEmpty(d.Unit) ? d.DisplayName : $"{d.DisplayName} ({d.Unit})";
            AddFields.Add(new ComponentFieldVm
            {
                Key = d.Key, Label = label, Unit = d.Unit,
                IsNumeric = numeric, HotColumn = d.HotColumn,
            });
        }
    }

    // MPN'den doldur: bilinen üretici şemalarıyla çözer, çözebildiği alanları yazar.
    // Çözemezse standart kalıptan öneri verir; hiçbir alan uydurulmaz.
    [RelayCommand]
    private void DecodeComponentMpn()
    {
        MismatchType = null;
        if (AddType is null) { AddStatus = "Önce komponent tipini seç."; return; }
        if (string.IsNullOrWhiteSpace(AddMpn))
        {
            AddStatus = "Önce MPN yaz (ya da alanları elle doldurup kaydet).";
            return;
        }

        // "MPN'den Doldur" = her şeyi MPN'den türet: önceki değerler + üretici sıfırlanır,
        // yoksa eski üretici/değer yeni MPN'e aitmiş gibi kalır.
        foreach (var f in AddFields) f.Value = null;
        AddManufacturer = null;

        var res = ServiceFactory.CreateDatasheetComponentService().DecodeMpn(AddMpn);

        if (res.Decoded is { } d)
        {
            // Seçili tip ile çözülen tip farklıysa doldurmadan uyar (yanlış alanlara yazmayalım).
            if (d.ComponentTypeId != AddType.Id)
            {
                MismatchType = Types.FirstOrDefault(t => t.Id == d.ComponentTypeId);
                AddStatus = $"Bu MPN {MismatchType?.Name ?? "başka bir tip"} profiline uyuyor [{d.ProfileName}].";
                return;
            }

            FillFieldsFromDecode(d);
            if (!string.IsNullOrWhiteSpace(d.Manufacturer)) AddManufacturer = d.Manufacturer;
            AddStatus = $"MPN çözüldü [{d.ProfileName}] — boş kalan alanları elle tamamla.";
            return;
        }

        // Öneri katmanı değeri direnç mantığıyla üretir (kod→Ω). Yalnız Direnç tipinde
        // anlamlı; başka tipte (kondansatör/regülatör...) Ω önerisi yanıltıcı olur, gösterilmez.
        if (AddType.Id == 2 && res.Suggestions.Count > 0)
        {
            if (res.Suggestions.Count == 1)
            {
                SetHot("primary", res.Suggestions[0].Ohms);
                AddStatus = $"Profil yok; standart kalıptan ÖNERİ ({res.Suggestions[0].Reason}) — kontrol et.";
            }
            else
            {
                string list = string.Join(", ", res.Suggestions.Select(x => $"{x.Ohms:g} Ω ({x.Reason})"));
                AddStatus = $"Profil yok; olası değer(ler): {list} — kontrol edip elle gir.";
            }
            return;
        }
        AddStatus = "MPN çözülemedi (bu seri için profil yok) — alanları elle doldur.";
    }

    [RelayCommand]
    private async Task SwitchToMismatchTypeAsync()
    {
        var target = MismatchType;
        if (target is null) return;
        MismatchType = null;

        // Tipi normal özellikten set ediyoruz ama rebuild'i bastırıyoruz; böylece
        // alanları burada TEK sefer, bekleyerek kuruyoruz (yarış ve çift-ekleme önlenir).
        _suppressAddRebuild = true;
        AddType = target;
        _suppressAddRebuild = false;
        await RebuildAddFieldsAsync(target);

        DecodeComponentMpn();     // artık tip uyuyor ve alanlar hazır, doldurur
    }

    // Çözülen değerleri alan anahtarlarına yerleştirir (birim SI; insan-okur gösterilir).
    // Tip-bağımsız: hangi alan varsa dolar, olmayan (ör. kondansatörde tcr) atlanır.
    private void FillFieldsFromDecode(DecodedMpn d)
    {
        SetHot("primary", d.PrimaryValueSi);
        SetHot("secondary", d.SecondaryValueSi);
        SetKey("dielectric", d.Dielectric);
        SetKey("tcr", d.TcrPpm);
        if (d.TolerancePercent is double tol)
            SetKey("tolerance", "±" + tol.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%");
        if (!string.IsNullOrWhiteSpace(d.Package)) SetKey("package", d.Package);
    }

    private void SetHot(string hot, double? si)
    {
        if (si is null) return;
        var f = AddFields.FirstOrDefault(x => x.HotColumn == hot);
        if (f != null) f.Value = FormatField(si.Value, f.Unit);
    }

    private void SetKey(string key, object? val)
    {
        if (val is null) return;
        var f = AddFields.FirstOrDefault(x => x.Key == key);
        if (f is null) return;
        f.Value = f.IsNumeric && val is IConvertible
            ? FormatField(Convert.ToDouble(val), f.Unit)
            : val.ToString();
    }

    // Öneri katmanı: birincil sayısal alana ham SI değeri koy.
    private void SetFieldValue(string _, double si)
    {
        var f = AddFields.FirstOrDefault(x => x.HotColumn == "primary");
        if (f != null) f.Value = FormatField(si, f.Unit);
    }

    // SI değeri insan-okur ama birimsiz gösterir ("10 k"); alan etiketinde birim zaten var.
    private static string FormatField(double si, string? unit)
    {
        string s = KomponentSistemi.Services.ValueNormalizer.FormatSi(si, unit);
        return string.IsNullOrEmpty(unit) ? s : s.Replace(unit, "").Trim();
    }

    [RelayCommand]
    private async Task SaveComponentAsync()
    {
        if (AddType is null) { AddStatus = "Önce komponent tipini seç."; return; }

        var norm = new KomponentSistemi.Services.ValueNormalizer();
        double? primary = null, secondary = null;
        var paramValues = new Dictionary<string, object?>();

        foreach (var f in AddFields)
        {
            if (string.IsNullOrWhiteSpace(f.Value)) continue;
            if (f.IsNumeric)
            {
                double? si = norm.NormalizeNumeric(f.Value);
                if (si is null) continue;
                if (f.HotColumn == "primary") primary = si;
                else if (f.HotColumn == "secondary") secondary = si;
                else paramValues[f.Key] = si;
            }
            else
            {
                var cat = norm.NormalizeCategorical(f.Value);
                if (cat != null) paramValues[f.Key] = cat;
            }
        }

        var (ok, message) = await ServiceFactory.CreateDatasheetComponentService()
            .SaveAsync(new DatasheetComponentService.SaveRequest
            {
                ComponentTypeId = AddType.Id,
                Mpn = AddMpn ?? "",
                Manufacturer = AddManufacturer ?? "",
                PrimaryValueSi = primary,
                SecondaryValueSi = secondary,
                Params = paramValues,
            });

        AddStatus = message;
        if (!ok) return; // hata ekranda kalsın, form silinmesin

        AddMpn = null;
        AddManufacturer = null;
        MismatchType = null;
        foreach (var f in AddFields) f.Value = null;
        await Task.Delay(4000);
        if (AddStatus == message) AddStatus = "";
    }

    private static string ValueLabelFor(SchematicSuggestion g)
    {
        if (g.ValueKind == SchematicValueKind.Numeric) return g.RawValue;
        if (g.ValueKind == SchematicValueKind.PartNumber) return "MPN: " + g.PartText;
        if (g.ExpectedPositions is int p) return $"{p} giriş";
        return "(değersiz)";
    }

    private static string StatusLabelFor(SchematicSuggestStatus st) => st switch
    {
        SchematicSuggestStatus.ExactValue => "tam değer",
        SchematicSuggestStatus.NearValue => "yakın değer",
        SchematicSuggestStatus.MpnMatch => "MPN eşleşti",
        SchematicSuggestStatus.NoValueManual => "değer yok — elle seç",
        SchematicSuggestStatus.NoMatch => "eşleşme yok",
        SchematicSuggestStatus.UnknownType => "tip bilinmiyor",
        _ => ""
    };

    // Aynı anda en fazla iki komponent karşılaştırılıyor.
    [RelayCommand]
    private async Task AddToCompareAsync()
    {
        if (SelectedRow == null) { Status = "Önce listeden bir komponent seç."; return; }

        var detail = await _query.GetDetailAsync(SelectedRow.Id);
        if (detail == null) return;

        if (CompareA == null)
        { CompareA = detail; Status = $"Karşılaştırmaya eklendi (A): {detail.Mpn}"; }
        else if (CompareB == null)
        { CompareB = detail; Status = $"Karşılaştırmaya eklendi (B): {detail.Mpn}"; }
        else
        { CompareA = CompareB; CompareB = detail; Status = $"İki slot doluydu, en eski çıkarıldı. Eklendi: {detail.Mpn}"; }

        BuildCompareRows();
    }

    [RelayCommand]
    private void ClearCompare()
    {
        CompareA = null;
        CompareB = null;
        CompareRows.Clear();
        Status = "Karşılaştırma temizlendi.";
    }

    // Parametre isimlerini birleştirip satır satır karşılaştırır.
    private void BuildCompareRows()
    {
        CompareRows.Clear();

        var names = new List<string>();
        if (CompareA != null)
            foreach (var p in CompareA.Parameters) if (!names.Contains(p.Name)) names.Add(p.Name);
        if (CompareB != null)
            foreach (var p in CompareB.Parameters) if (!names.Contains(p.Name)) names.Add(p.Name);

        foreach (var n in names)
            CompareRows.Add(new CompareRow
            {
                Name = n,
                ValueA = CompareA?.Parameters.FirstOrDefault(p => p.Name == n)?.Value ?? "",
                ValueB = CompareB?.Parameters.FirstOrDefault(p => p.Name == n)?.Value ?? ""
            });
    }

    private async Task LoadAsync()
    {
        var types = await _query.GetTypesAsync();
        foreach (var t in types)
            Types.Add(t);

        FilterTypes.Add(new ComponentTypeDto { Id = 0, Name = "Tümü" });
        foreach (var t in types)
            FilterTypes.Add(t);

        // Komponent Ekle formu açılışta Direnç ile gelsin (alanları kurar).
        AddType = Types.FirstOrDefault(t => t.Id == 2) ?? Types.FirstOrDefault();

        foreach (var h in await _query.GetTypeHeadersAsync())
            _typeHeaders[h.TypeId] = (h.PrimaryHeader, h.SecondaryHeader);

        SelectedType = FilterTypes[0];

        FillComponents(await _query.GetAllAsync());
        Status = $"{Components.Count} komponent yüklendi.";

        _bomListId = await _bom.GetOrCreateDefaultListAsync();

        Currencies.Add(AutoCurrency);
        foreach (var c in await _bom.GetCurrenciesAsync())
            Currencies.Add(c);
        PreferredCurrency = AutoCurrency;   // henüz yenileme tetiklemez (_bomReady false)

        await RefreshBomListsAsync();
        SelectedBomList = BomLists.FirstOrDefault(b => b.Id == _bomListId) ?? BomLists.FirstOrDefault();

        _bomReady = true;
        await RefreshBomAsync();

        await RefreshManagementAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        int? typeId = (SelectedType == null || SelectedType.Id == 0) ? null : SelectedType.Id;

        var criteria = new SearchCriteria
        {
            ComponentTypeId = typeId,
            Text = SearchText,
            MinPrimary = _normalizer.NormalizeNumeric(MinPrimaryText),
            MaxPrimary = _normalizer.NormalizeNumeric(MaxPrimaryText),
            MinSecondary = _normalizer.NormalizeNumeric(MinSecondaryText),
            MaxSecondary = _normalizer.NormalizeNumeric(MaxSecondaryText),
        };

        if (!string.IsNullOrEmpty(SelectedSubtype) && SelectedSubtype != AllSubtypes)
            criteria.CategoricalFilters["subtype"] = SelectedSubtype;

        FillComponents(await _search.SearchAsync(criteria));
        UpdateHeaders(typeId);   // başlıklar sonuçlarla birlikte (Ara'ya basınca) değişsin
        UpdateTypeInfo(typeId);
        Status = $"{Components.Count} sonuç bulundu.";
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        SelectedType = FilterTypes.FirstOrDefault();
        SelectedSubtype = AllSubtypes;
        SearchText = null;
        MinPrimaryText = null;
        MaxPrimaryText = null;
        MinSecondaryText = null;
        MaxSecondaryText = null;

        FillComponents(await _query.GetAllAsync());
        UpdateHeaders(null);
        UpdateTypeInfo(null);
        Status = $"{Components.Count} komponent yüklendi.";
    }

    [RelayCommand]
    private async Task ImportStagedAsync()
    {
        if (string.IsNullOrEmpty(StagedFilePath)) return;
        await ImportFileAsync(StagedFilePath);
        StagedFilePath = null;
    }

    [RelayCommand]
    private void ClearStaged() => StagedFilePath = null;

    public async Task ImportFileAsync(string path)
    {
        try
        {
            var import = ServiceFactory.CreateImportService();

            // Tip seçilmemişse başlıklardan tahmin ediyoruz.
            if (ImportType == null)
            {
                var suggested = await import.SuggestTypeAsync(path);
                if (suggested == null)
                {
                    Status = "Tip önerilemedi — lütfen içe aktarma tipini elle seçin.";
                    return;
                }
                // ComboBox referans eşitliğine baktığı için listedeki aynı Id'li nesneyi seçiyoruz.
                ImportType = Types.FirstOrDefault(t => t.Id == suggested.Id);
                Status = $"Başlıklara göre tip önerildi: {suggested.Name}";
            }

            var source = string.IsNullOrWhiteSpace(ImportSource) ? "Bilinmeyen" : ImportSource.Trim();
            var currency = string.IsNullOrWhiteSpace(ImportCurrency)
                ? null
                : ImportCurrency.Trim().ToUpperInvariant();

            Dictionary<string, string>? fixedParams = null;
            if (!string.IsNullOrWhiteSpace(ImportSubtype))
                fixedParams = new Dictionary<string, string> { ["subtype"] = ImportSubtype.Trim() };

            Status = "İçe aktarılıyor...";
            var result = await import.ImportAsync(path, ImportType!.Id, source, currency, fixedParams, ImportNote);

            FillComponents(await _query.GetAllAsync());
            await RefreshManagementAsync();
            await RefreshBomAsync();          // teklif/fiyat değişince BOM tablosu da anında tazelensin
            Status = $"İçe aktarma bitti: {result.Added} eklendi, {result.Updated} güncellendi, {result.Errors.Count} hata.";

            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"Dosya: {System.IO.Path.GetFileName(path)}");
            lines.AppendLine($"Tip: {ImportType.Name}   Kaynak: {source}   Para birimi: {currency ?? "(hücreden)"}");
            lines.AppendLine($"Eklenen: {result.Added}   Güncellenen: {result.Updated}   Hata: {result.Errors.Count}");
            if (result.MissingExpected.Count > 0)
                lines.AppendLine($"⚠ Beklenen parametre(ler) bu dosyada YOK: {string.Join(", ", result.MissingExpected)}");
            else
                lines.AppendLine("Beklenen tüm parametreler bulundu.");
            if (result.UnmappedHeaders.Count > 0)
                lines.AppendLine($"Not: tanınmayan başlık(lar) (istersen parametre olarak eklenebilir): {string.Join(", ", result.UnmappedHeaders)}");
            ImportReport = lines.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            Status = $"İçe aktarma hatası: {ex.Message}";
        }
    }

    // Tablodaki "+" düğmesi: 1 adet, referanssız.
    public async Task QuickAddToBomAsync(ComponentSummaryDto row)
    {
        await _bom.AddItemAsync(_bomListId, row.Id, 1, null);
        await RefreshBomListsAsync();
        SelectBomListById(_bomListId);
        await RefreshBomAsync();
        Status = $"Sepete eklendi: {row.Mpn}";
    }

    public async Task QuickRemoveFromBomAsync(BomRowDto row)
    {
        await _bom.RemoveItemAsync(row.BomItemId);
        await RefreshBomListsAsync();
        SelectBomListById(_bomListId);
        await RefreshBomAsync();
        Status = $"Sepetten çıkarıldı: {row.Mpn}";
    }

    // BOM satırında adedi ± ile değiştir (en az 1).
    public async Task ChangeBomQuantityAsync(BomRowDto row, int delta)
        => await ApplyBomQuantityAsync(row, row.Quantity + delta);

    // BOM satırına elle yazılan adedi uygula.
    public async Task SetBomQuantityAsync(BomRowDto row, int quantity)
    {
        if (quantity < 1) quantity = 1;
        if (quantity == row.Quantity) return;   // değişmediyse gereksiz yazma yok
        await ApplyBomQuantityAsync(row, quantity);
    }

    private async Task ApplyBomQuantityAsync(BomRowDto row, int quantity)
    {
        if (quantity < 1) quantity = 1;
        await _bom.UpdateItemAsync(row.BomItemId, quantity, row.References);
        await RefreshBomAsync();
        Status = $"Adet güncellendi: {row.Mpn} ×{quantity}";
    }

    [RelayCommand]
    private async Task AddToBomAsync()
    {
        if (SelectedRow == null)
        {
            Status = "Önce listeden bir komponent seç.";
            return;
        }

        await _bom.AddItemAsync(_bomListId, SelectedRow.Id, AddQuantity, AddReferences);
        await RefreshBomListsAsync();
        SelectBomListById(_bomListId);     // aktif projeyi yeni DTO ile yeniden seç → BOM tablosu da tazelenir
        await RefreshBomAsync();

        Status = $"Projeye eklendi: {SelectedRow.Mpn} ×{AddQuantity}";
        AddReferences = null;
        AddQuantity = 1;
    }

    [RelayCommand]
    private async Task RemoveFromBomAsync(BomRowDto? row)
    {
        if (row == null) return;

        await _bom.RemoveItemAsync(row.BomItemId);
        await RefreshBomListsAsync();      // proje sayısı hemen azalsın
        SelectBomListById(_bomListId);
        await RefreshBomAsync();
        Status = $"Projeden çıkarıldı: {row.Mpn}";
    }

    private async Task RefreshBomAsync()
    {
        var detail = await _bom.GetDetailAsync(_bomListId, EffectivePreferred());

        BomRows.Clear();
        foreach (var r in detail.Rows)
            BomRows.Add(r);

        BomName = detail.Name;
        BomTotalsDisplay = detail.TotalsDisplay;
        BomPricelessNote = detail.PricelessNote;
    }

    partial void OnPreferredCurrencyChanged(string? value)
    {
        if (_bomReady) _ = RefreshBomAsync();
    }

    // "Otomatik (çoğunluk)" seçiliyse servise null geçeriz (servis çoğunluğu bulur).
    private string? EffectivePreferred()
        => PreferredCurrency == AutoCurrency ? null : PreferredCurrency;

    [RelayCommand]
    private async Task SetRowOfferAsync(OfferOptionDto? opt)
    {
        if (opt == null) return;

        await _bom.SetSelectedOfferAsync(opt.BomItemId, opt.OfferId);
        await RefreshBomAsync();
    }

    partial void OnSelectedBomListChanged(BomListDto? value)
    {
        if (value == null) return;
        _bomListId = value.Id;
        BomNameInput = value.Name;
        if (_bomReady) _ = RefreshBomAsync();
    }

    private async Task RefreshBomListsAsync()
    {
        var lists = await _bom.GetListsAsync();
        BomLists.Clear();
        foreach (var l in lists)
            BomLists.Add(l);
    }

    private void SelectBomListById(int id)
        => SelectedBomList = BomLists.FirstOrDefault(b => b.Id == id) ?? BomLists.FirstOrDefault();

    [RelayCommand]
    private async Task NewBomAsync()
    {
        var name = string.IsNullOrWhiteSpace(NewProjectName) ? $"Proje {BomLists.Count + 1}" : NewProjectName!.Trim();
        int id = await _bom.CreateListAsync(name);
        NewProjectName = null;
        await RefreshBomListsAsync();
        SelectBomListById(id);
        Status = $"Proje oluşturuldu: {name}";
    }

    [RelayCommand]
    private async Task RenameBomAsync()
    {
        if (SelectedBomList == null || string.IsNullOrWhiteSpace(BomNameInput)) return;
        int id = SelectedBomList.Id;
        await _bom.RenameListAsync(id, BomNameInput!.Trim());
        await RefreshBomListsAsync();
        SelectBomListById(id);
        Status = "Proje adı güncellendi.";
    }

    // Sil butonu artık doğrudan silmiyor, önce onay penceresi açıyor.
    [RelayCommand]
    private void DeleteBom()
    {
        if (SelectedBomList == null) return;
        if (BomLists.Count <= 1) { Status = "Son proje silinemez."; return; }

        ProjectDeleteMessage = $"'{SelectedBomList.Name}' projesi ve içindeki tüm satırlar silinecek. Emin misiniz? Bu işlem geri alınamaz.";
        IsConfirmingProjectDelete = true;
    }

    [RelayCommand]
    private void CancelProjectDelete() => IsConfirmingProjectDelete = false;

    [RelayCommand]
    private async Task ConfirmProjectDeleteAsync()
    {
        IsConfirmingProjectDelete = false;
        if (SelectedBomList == null) return;
        if (BomLists.Count <= 1) { Status = "Son proje silinemez."; return; }

        await _bom.DeleteListAsync(SelectedBomList.Id);
        await RefreshBomListsAsync();
        SelectBomListById(BomLists.FirstOrDefault()?.Id ?? 0);
        Status = "Proje silindi.";
    }

    partial void OnSelectedManagedComponentChanged(ManagedComponentRow? value)
    {
        _ = LoadManagedOffersAsync(value?.Id);
    }

    private async Task LoadManagedOffersAsync(int? componentId)
    {
        ManagedOffers.Clear();
        if (componentId == null) return;
        foreach (var o in await _mgmt.GetOffersAsync(componentId.Value))
            ManagedOffers.Add(o);
    }

    private async Task RefreshManagementAsync()
    {
        Batches.Clear();
        foreach (var b in await _mgmt.GetBatchesAsync())
            Batches.Add(b);

        ManagedComponents.Clear();
        var comps = string.IsNullOrWhiteSpace(ManageSearchText)
            ? await _query.GetAllAsync()
            : await _search.SearchAsync(new SearchCriteria { Text = ManageSearchText });
        foreach (var c in comps.OrderByDescending(c => c.Id))   // en son eklenen en üstte
        {
            var row = new ManagedComponentRow
            {
                Id = c.Id,
                Mpn = c.Mpn,
                Manufacturer = c.Manufacturer,
                TypeDisplay = c.TypeDisplay,
                OfferCount = c.OfferCount
            };
            row.PropertyChanged += ManagedRow_PropertyChanged;
            ManagedComponents.Add(row);
        }
        UpdateSelectedManageCount();
    }

    private void ManagedRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ManagedComponentRow.IsSelected))
            UpdateSelectedManageCount();
    }

    private void UpdateSelectedManageCount()
        => SelectedManageCount = ManagedComponents.Count(r => r.IsSelected);

    [RelayCommand]
    private async Task ManageSearchAsync() => await RefreshManagementAsync();

    [RelayCommand]
    private async Task UndoBatchAsync()
    {
        if (SelectedBatch == null) { Status = "Önce bir içe aktarma seç."; return; }

        await _mgmt.UndoBatchAsync(SelectedBatch.Id);
        await RefreshManagementAsync();
        FillComponents(await _query.GetAllAsync());
        await RefreshBomAsync();                       // BOM etkilenmiş olabilir
        Status = "İçe aktarma geri alındı.";
    }

    [RelayCommand]
    private void RequestDeleteComponents()
    {
        int n = ManagedComponents.Count(r => r.IsSelected);
        if (n == 0) { Status = "Önce silmek için komponent(ler) işaretle."; return; }
        DeleteConfirmMessage = $"{n} komponent (ve teklifleri) silinecek. Emin misiniz? Bu işlem geri alınamaz.";
        IsConfirmingDelete = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsConfirmingDelete = false;

        var ids = ManagedComponents.Where(r => r.IsSelected).Select(r => r.Id).ToList();
        foreach (var id in ids)
            await _mgmt.DeleteComponentAsync(id);

        ManagedOffers.Clear();
        await RefreshManagementAsync();
        FillComponents(await _query.GetAllAsync());
        await RefreshBomAsync();
        Status = $"{ids.Count} komponent silindi.";
    }

    [RelayCommand]
    private async Task DeleteManagedOfferAsync()
    {
        if (SelectedManagedOffer == null) { Status = "Önce bir teklif seç."; return; }

        int? compId = SelectedManagedComponent?.Id;
        await _mgmt.DeleteOfferAsync(SelectedManagedOffer.OfferId);
        await LoadManagedOffersAsync(compId);
        FillComponents(await _query.GetAllAsync());
        await RefreshBomAsync();
        Status = "Teklif silindi.";
    }

    public async Task ExportBomAsync(string path)
    {
        try
        {
            var csv = await _bom.ExportCsvAsync(_bomListId, EffectivePreferred());
            // UTF-8 + BOM: Excel Türkçe karakterleri doğru okusun.
            await System.IO.File.WriteAllTextAsync(path, csv, new System.Text.UTF8Encoding(true));
            Status = $"CSV dışa aktarıldı: {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            Status = $"Dışa aktarma hatası: {ex.Message}";
        }
    }

    private void FillComponents(List<ComponentSummaryDto> list)
    {
        SelectedRow = null;
        Components.Clear();
        int no = 1;
        foreach (var c in list)
        {
            c.RowNo = no++;
            Components.Add(c);
        }
        ResultCountText = $"{Components.Count} sonuç";
    }
}