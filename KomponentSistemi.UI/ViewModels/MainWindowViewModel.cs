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

    // Aktif (varsayılan) projenin kimliği — LoadAsync'te çözülür.
    private int _bomListId;

    // --- KiCad footprint (Adım 6) ---
    [ObservableProperty] private string? _schematicPath;
    [ObservableProperty] private string _kiCadStatus = "";
    public ObservableCollection<KiCadMatchRow> KiCadRows { get; } = new();

    public ObservableCollection<ComponentSummaryDto> Components { get; } = new();
    public ObservableCollection<ComponentTypeDto> Types { get; } = new();
    public ObservableCollection<ComponentTypeDto> FilterTypes { get; } = new();  // "Tümü" + tipler (arama filtresi)

    // Tipe göre sıcak sütun başlıkları ("Direnç (Ω)", "Güç (W)")
    private readonly Dictionary<int, (string primary, string secondary)> _typeHeaders = new();
    [ObservableProperty] private string _primaryHeader = "Değer";
    [ObservableProperty] private string _secondaryHeader = "2. Değer";

    // Seçili tip hakkında kısa bilgi (arama ekranı alt paneli)
    [ObservableProperty] private bool _hasTypeInfo;
    [ObservableProperty] private string _typeInfoTitle = "";
    [ObservableProperty] private string _typeInfoBody = "";

    [ObservableProperty] private ComponentTypeDto? _selectedType;
    [ObservableProperty] private string? _searchText;
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

    // --- İçe aktarma ---
    [ObservableProperty] private ComponentTypeDto? _importType;
    [ObservableProperty] private string? _importSource;
    [ObservableProperty] private string? _importCurrency;
    [ObservableProperty] private string? _importSubtype;
    [ObservableProperty] private string? _importNote;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImportReport))]
    private string? _importReport;

    public bool HasImportReport => !string.IsNullOrEmpty(ImportReport);

    // Seçilen ama henüz içe aktarılmamış dosya (staged)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStagedFile))]
    [NotifyPropertyChangedFor(nameof(StagedFileName))]
    private string? _stagedFilePath;

    public bool HasStagedFile => !string.IsNullOrEmpty(StagedFilePath);
    public string StagedFileName => string.IsNullOrEmpty(StagedFilePath) ? "" : System.IO.Path.GetFileName(StagedFilePath);

    // --- BOM (proje) ---
    public ObservableCollection<BomRowDto> BomRows { get; } = new();

    // Projeler (çoklu BOM)
    public ObservableCollection<BomListDto> BomLists { get; } = new();
    [ObservableProperty] private BomListDto? _selectedBomList;
    [ObservableProperty] private string? _bomNameInput;      // seçili projenin adı (yeniden adlandırma)
    [ObservableProperty] private string? _newProjectName;    // yeni proje oluşturma kutusu

    // --- Yönetim (silme + içe aktarma geri alma) ---
    public ObservableCollection<ImportBatchDto> Batches { get; } = new();
    [ObservableProperty] private ImportBatchDto? _selectedBatch;

    public ObservableCollection<ManagedComponentRow> ManagedComponents { get; } = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasManagedComponent))]
    private ManagedComponentRow? _selectedManagedComponent;
    public bool HasManagedComponent => SelectedManagedComponent != null;
    [ObservableProperty] private string? _manageSearchText;

    // Silmek için işaretli komponent sayısı + onay penceresi durumu
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedManageCountText))]
    private int _selectedManageCount;
    public string SelectedManageCountText =>
        SelectedManageCount == 0 ? "Silmek için işaretle" : $"{SelectedManageCount} komponent seçili";

    [ObservableProperty] private bool _isConfirmingDelete;
    [ObservableProperty] private string _deleteConfirmMessage = "";

    public ObservableCollection<ManagedOfferDto> ManagedOffers { get; } = new();
    [ObservableProperty] private ManagedOfferDto? _selectedManagedOffer;

    [ObservableProperty] private string _bomName = "";
    [ObservableProperty] private string _bomTotalsDisplay = "—";
    [ObservableProperty] private string _bomPricelessNote = "";

    // BOM tablosunda seçili satır (çıkarma için)
    [ObservableProperty] private BomRowDto? _selectedBomRow;

    // "Otomatik": temel para birimini BOM'un kendi çoğunluğundan bul.
    private const string AutoCurrency = "Otomatik (çoğunluk)";

    // Fiyat para birimi tercihi (Otomatik + gerçek para birimleri)
    public ObservableCollection<string> Currencies { get; } = new();
    [ObservableProperty] private string? _preferredCurrency;

    // İlk yükleme sırasında tercih atanınca gereksiz/çakışan yenileme olmasın diye.
    private bool _bomReady;

    // "Projeye ekle" için adet + referans girişleri
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

    // Başlıkları verilen tipe göre güncelle — seçimde DEĞİL, arama/temizle sonrası çağrılır.
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

    // Tipe göre kısa bilgi — seçimde DEĞİL, arama/temizle sonrası çağrılır.
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

    private async Task LoadAsync()
    {
        var types = await _query.GetTypesAsync();
        foreach (var t in types)
            Types.Add(t);

        // Arama filtresi: başa "Tümü" (Id=0), sonra tipler.
        FilterTypes.Add(new ComponentTypeDto { Id = 0, Name = "Tümü" });
        foreach (var t in types)
            FilterTypes.Add(t);

        // Tipe göre sıcak sütun başlıklarını hazırla.
        foreach (var h in await _query.GetTypeHeadersAsync())
            _typeHeaders[h.TypeId] = (h.PrimaryHeader, h.SecondaryHeader);

        SelectedType = FilterTypes[0];   // varsayılan: Tümü

        FillComponents(await _query.GetAllAsync());
        Status = $"{Components.Count} komponent yüklendi.";

        // Projeleri + para birimlerini yükle, aktif projeyi seç, BOM tablosunu doldur.
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

        FillComponents(await _search.SearchAsync(criteria));
        UpdateHeaders(typeId);   // başlıklar sonuçlarla birlikte (Ara'ya basınca) değişsin
        UpdateTypeInfo(typeId);  // tip bilgisi de arama sonrası çıksın
        Status = $"{Components.Count} sonuç bulundu.";
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        SelectedType = FilterTypes.FirstOrDefault();
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

    // "İçe Aktar" butonu: seçili (staged) dosyayı gerçekten içe aktar.
    [RelayCommand]
    private async Task ImportStagedAsync()
    {
        if (string.IsNullOrEmpty(StagedFilePath)) return;
        await ImportFileAsync(StagedFilePath);
        StagedFilePath = null;   // aktarıldı, seçimi temizle
    }

    // "Vazgeç": seçili dosyayı içe aktarmadan bırak.
    [RelayCommand]
    private void ClearStaged() => StagedFilePath = null;

    // Asıl içe aktarma işi (ImportStagedAsync çağırır).
    public async Task ImportFileAsync(string path)
    {
        try
        {
            var import = ServiceFactory.CreateImportService();

            // Tip seçilmemişse: başlıklara bakıp öner (otonomi!)
            if (ImportType == null)
            {
                var suggested = await import.SuggestTypeAsync(path);
                if (suggested == null)
                {
                    Status = "Tip önerilemedi — lütfen içe aktarma tipini elle seçin.";
                    return;
                }
                // Öneriyi ComboBox'ta da göster (Types içindeki aynı Id'li nesneyi seç)
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
            await RefreshManagementAsync();   // Yönetim sekmesi anında güncellensin
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

    // --- BOM komutları ---

    // Seçili komponenti aktif projeye ekle (adet + referans ile).
    [RelayCommand]
    private async Task AddToBomAsync()
    {
        if (SelectedRow == null)
        {
            Status = "Önce listeden bir komponent seç.";
            return;
        }

        await _bom.AddItemAsync(_bomListId, SelectedRow.Id, AddQuantity, AddReferences);
        await RefreshBomAsync();

        Status = $"Projeye eklendi: {SelectedRow.Mpn} ×{AddQuantity}";
        AddReferences = null;
        AddQuantity = 1;
    }

    // Bir satırı projeden çıkar (butondan CommandParameter ile gelir).
    [RelayCommand]
    private async Task RemoveFromBomAsync(BomRowDto? row)
    {
        if (row == null) return;

        await _bom.RemoveItemAsync(row.BomItemId);
        await RefreshBomAsync();
        Status = $"Projeden çıkarıldı: {row.Mpn}";
    }

    // BOM tablosunu ve toplamları tazele.
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

    // Kullanıcı fiyat para birimini değiştirince toplamları yeniden hesapla.
    partial void OnPreferredCurrencyChanged(string? value)
    {
        if (_bomReady) _ = RefreshBomAsync();
    }

    // "Otomatik (çoğunluk)" seçiliyse servise null geçeriz (servis çoğunluğu bulur).
    private string? EffectivePreferred()
        => PreferredCurrency == AutoCurrency ? null : PreferredCurrency;

    // Kullanıcı bir satırda başka bir teklife (fiyata) tıklayınca onu aktif yap.
    [RelayCommand]
    private async Task SetRowOfferAsync(OfferOptionDto? opt)
    {
        if (opt == null) return;

        await _bom.SetSelectedOfferAsync(opt.BomItemId, opt.OfferId);
        await RefreshBomAsync();
    }

    // --- Çoklu proje ---

    // Proje seçilince: aktif projeyi değiştir, adı kutuya yaz, tabloyu tazele.
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

    [RelayCommand]
    private async Task DeleteBomAsync()
    {
        if (SelectedBomList == null) return;
        if (BomLists.Count <= 1) { Status = "Son proje silinemez."; return; }

        await _bom.DeleteListAsync(SelectedBomList.Id);
        await RefreshBomListsAsync();
        SelectBomListById(BomLists.FirstOrDefault()?.Id ?? 0);
        Status = "Proje silindi.";
    }

    // --- Yönetim komutları ---

    // Seçili komponent değişince tekliflerini yükle.
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
        foreach (var c in comps)
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
        FillComponents(await _query.GetAllAsync());   // Arama listesini tazele
        await RefreshBomAsync();                       // BOM etkilenmiş olabilir
        Status = "İçe aktarma geri alındı.";
    }

    // "Seçili komponentleri sil" → önce onay penceresi göster.
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

    // Onaylanınca: işaretli tüm komponentleri sil.
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
        await LoadManagedOffersAsync(compId);   // teklif panelini tazele (komponent seçili kalır)
        FillComponents(await _query.GetAllAsync());
        await RefreshBomAsync();
        Status = "Teklif silindi.";
    }

    // Kaydetme diyaloğundan gelen yola BOM'u CSV olarak yazar (code-behind çağırır).
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
    }
}