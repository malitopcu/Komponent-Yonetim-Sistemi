using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using KomponentSistemi.Services;

namespace KomponentSistemi.UI.ViewModels;

// Şemadan BOM önizlemesinde tek satır: bir sembol + aday listesi + seçim.
// Include işaretliyse "BOM'a Ekle" bu satırı seçili adayla ekler.
public partial class SchematicBomRow : ObservableObject
{
    public string Reference { get; init; } = "";
    public string TypeName { get; init; } = "";
    public string ValueLabel { get; init; } = "";
    public string StatusLabel { get; init; } = "";

    public List<SuggestedCandidate> Candidates { get; init; } = new();
    public bool HasCandidates => Candidates.Count > 0;

    [ObservableProperty] private SuggestedCandidate? _selectedCandidate;
    [ObservableProperty] private bool _include;
}
