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
    private readonly ValueNormalizer _normalizer = new();

    public ObservableCollection<ComponentSummaryDto> Components { get; } = new();
    public ObservableCollection<ComponentTypeDto> Types { get; } = new();

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImportReport))]
    private string? _importReport;

    public bool HasImportReport => !string.IsNullOrEmpty(ImportReport);

    public MainWindowViewModel(ComponentQueryService query, SearchService search)
    {
        _query = query;
        _search = search;
        _ = LoadAsync();
    }

    partial void OnSelectedRowChanged(ComponentSummaryDto? value)
    {
        _ = LoadDetailAsync(value);
    }

    private async Task LoadDetailAsync(ComponentSummaryDto? row)
    {
        Detail = row == null ? null : await _query.GetDetailAsync(row.Id);
    }

    private async Task LoadAsync()
    {
        foreach (var t in await _query.GetTypesAsync())
            Types.Add(t);

        FillComponents(await _query.GetAllAsync());
        Status = $"{Components.Count} komponent yüklendi.";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var criteria = new SearchCriteria
        {
            ComponentTypeId = SelectedType?.Id,
            Text = SearchText,
            MinPrimary = _normalizer.NormalizeNumeric(MinPrimaryText),
            MaxPrimary = _normalizer.NormalizeNumeric(MaxPrimaryText),
            MinSecondary = _normalizer.NormalizeNumeric(MinSecondaryText),
            MaxSecondary = _normalizer.NormalizeNumeric(MaxSecondaryText),
        };

        FillComponents(await _search.SearchAsync(criteria));
        Status = $"{Components.Count} sonuç bulundu.";
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        SelectedType = null;
        SearchText = null;
        MinPrimaryText = null;
        MaxPrimaryText = null;
        MinSecondaryText = null;
        MaxSecondaryText = null;

        FillComponents(await _query.GetAllAsync());
        Status = $"{Components.Count} komponent yüklendi.";
    }

    // Dosya seçici VEYA sürükle-bırak buraya çıkar.
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
            var result = await import.ImportAsync(path, ImportType!.Id, source, currency, fixedParams);

            FillComponents(await _query.GetAllAsync());
            Status = $"İçe aktarma bitti: {result.Added} eklendi, {result.Updated} güncellendi, {result.Errors.Count} hata.";

            ImportReport =
                $"Dosya: {System.IO.Path.GetFileName(path)}\n" +
                $"Tip: {ImportType.Name}   Kaynak: {source}   Para birimi: {currency ?? "(hücreden)"}\n" +
                $"Eklenen: {result.Added}   Güncellenen: {result.Updated}   Hata: {result.Errors.Count}\n" +
                (result.UnmappedHeaders.Count > 0
                    ? $"Tanınmayan başlıklar ({result.UnmappedHeaders.Count}): {string.Join(", ", result.UnmappedHeaders)}"
                    : "Tüm başlıklar tanındı.");
        }
        catch (Exception ex)
        {
            Status = $"İçe aktarma hatası: {ex.Message}";
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