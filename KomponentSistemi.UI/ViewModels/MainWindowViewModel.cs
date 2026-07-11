using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    // Tablonun veri kaynağı
    public ObservableCollection<ComponentSummaryDto> Components { get; } = new();

    // ComboBox'ın veri kaynağı (Kondansatör, Direnç, ...)
    public ObservableCollection<ComponentTypeDto> Types { get; } = new();

    // --- Filtre alanları: ekrandaki kutularla iki yönlü bağlı ---
    [ObservableProperty] private ComponentTypeDto? _selectedType;
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private string? _minPrimaryText;
    [ObservableProperty] private string? _maxPrimaryText;
    [ObservableProperty] private string? _minSecondaryText;
    [ObservableProperty] private string? _maxSecondaryText;

    [ObservableProperty] private string _status = "Yükleniyor...";

    public MainWindowViewModel(ComponentQueryService query, SearchService search)
    {
        _query = query;
        _search = search;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        foreach (var t in await _query.GetTypesAsync())
            Types.Add(t);

        FillComponents(await _query.GetAllAsync());
        Status = $"{Components.Count} komponent yüklendi.";
    }

    // Ara butonu: [RelayCommand], SearchAsync'ten "SearchCommand" üretir.
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

    // Temizle butonu: filtreleri sıfırla, tüm listeyi geri getir
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

    // Listeyi tabloya döker, satır numaralarını basar (1, 2, 3...)
    private void FillComponents(List<ComponentSummaryDto> list)
    {
        Components.Clear();
        int no = 1;
        foreach (var c in list)
        {
            c.RowNo = no++;
            Components.Add(c);
        }
    }
}