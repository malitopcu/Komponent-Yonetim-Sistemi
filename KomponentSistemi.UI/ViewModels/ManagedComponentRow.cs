using CommunityToolkit.Mvvm.ComponentModel;

namespace KomponentSistemi.UI.ViewModels;

// Yönetim ekranında checkbox ile işaretlenebilir komponent satırı.
public partial class ManagedComponentRow : ObservableObject
{
    public int Id { get; init; }
    public string Mpn { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string TypeDisplay { get; init; } = "";
    public int OfferCount { get; init; }

    [ObservableProperty] private bool _isSelected;
}
