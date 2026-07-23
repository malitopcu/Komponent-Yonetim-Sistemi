using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using KomponentSistemi.Services;
using System.Linq;
using System.Threading.Tasks;
using KomponentSistemi.UI.ViewModels;

namespace KomponentSistemi.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // Değer sütunlarının başlığı seçili tipe göre değişiyor. DataGrid sütunları
        // görsel ağaçta olmadığı için binding çalışmıyor, kod-arkasından set ediyoruz.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainWindowViewModel vm) return;

            void ApplyHeaders()
            {
                if (ResultsGrid.Columns.Count > 5)
                {
                    ResultsGrid.Columns[4].Header = vm.PrimaryHeader;
                    ResultsGrid.Columns[5].Header = vm.SecondaryHeader;
                }
            }

            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.PrimaryHeader) ||
                    e.PropertyName == nameof(MainWindowViewModel.SecondaryHeader))
                    ApplyHeaders();
            };

            ApplyHeaders();
        };
    }

    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
        => await OpenCsvPickerAsync();

    private async void ImportZone_PointerPressed(object? sender, PointerPressedEventArgs e)
        => await OpenCsvPickerAsync();

    private async Task OpenCsvPickerAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "İçe aktarılacak CSV dosyasını seçin",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("CSV dosyaları") { Patterns = new[] { "*.csv" } }
            }
        });

        if (files.Count == 0) return;

        var path = files[0].TryGetLocalPath();
        if (path == null) return;

        // Sadece seçiyoruz; içe aktarma kullanıcı butona basınca.
        if (DataContext is MainWindowViewModel vm)
            vm.StagedFilePath = path;
    }

    private async void KiCadBrowse_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "KiCad şeması seçin",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("KiCad şema") { Patterns = new[] { "*.kicad_sch" } }
            }
        });

        if (files.Count == 0) return;

        var path = files[0].TryGetLocalPath();
        if (path != null && DataContext is MainWindowViewModel vm)
            vm.SchematicPath = path;
    }

    // Şemadan BOM bölümünün kendi şema seçici (footprint bölümünden bağımsız).
    private async void SchematicBomBrowse_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Şemadan BOM için KiCad şeması seçin",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("KiCad şema") { Patterns = new[] { "*.kicad_sch" } }
            }
        });

        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path != null && DataContext is MainWindowViewModel vm)
            vm.SchematicBomPath = path;
    }

    private void QuickAdd_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is ComponentSummaryDto row &&
            DataContext is MainWindowViewModel vm)
            _ = vm.QuickAddToBomAsync(row);
    }

    private void QuickRemove_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is BomRowDto row &&
            DataContext is MainWindowViewModel vm)
            _ = vm.QuickRemoveFromBomAsync(row);
    }

    // BOM tablosunda adet − / + adım butonları.
    private void BomQtyMinus_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is BomRowDto row && DataContext is MainWindowViewModel vm)
            _ = vm.ChangeBomQuantityAsync(row, -1);
    }

    private void BomQtyPlus_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is BomRowDto row && DataContext is MainWindowViewModel vm)
            _ = vm.ChangeBomQuantityAsync(row, +1);
    }

    // Adet kutusuna elle yazıp Enter'a basınca ya da odaktan çıkınca uygula.
    private void BomQty_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb)
            CommitBomQty(tb);
    }

    private void BomQty_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            CommitBomQty(tb);
    }

    private void CommitBomQty(TextBox tb)
    {
        if (tb.DataContext is not BomRowDto row || DataContext is not MainWindowViewModel vm) return;

        if (int.TryParse(tb.Text, out int q))
            _ = vm.SetBomQuantityAsync(row, q);
        else
            tb.Text = row.Quantity.ToString();   // geçersiz giriş → eski değere dön
    }

    private void ColumnsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(8) };
        foreach (var col in ResultsGrid.Columns)
        {
            var header = col.Header?.ToString();
            if (string.IsNullOrWhiteSpace(header)) continue;

            var c = col;
            var cb = new CheckBox { Content = header, IsChecked = c.IsVisible };
            cb.IsCheckedChanged += (_, _) => c.IsVisible = cb.IsChecked == true;
            panel.Children.Add(cb);
        }

        new Flyout { Content = panel }.ShowAt(btn);
    }

    private void ThemeToggle_Click(object? sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app is null) return;

        bool goingDark = app.ActualThemeVariant != ThemeVariant.Dark;
        app.RequestedThemeVariant = goingDark ? ThemeVariant.Dark : ThemeVariant.Light;

        ThemeKnob.HorizontalAlignment = goingDark ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        ThemeKnob.Margin = goingDark ? new Thickness(0, 0, 3, 0) : new Thickness(3, 0, 0, 0);
        DayScene.IsVisible = !goingDark;
        NightScene.IsVisible = goingDark;
    }

    private async void ExportBomButton_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "BOM'u CSV olarak kaydet",
            SuggestedFileName = "bom.csv",
            DefaultExtension = "csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV dosyaları") { Patterns = new[] { "*.csv" } }
            }
        });

        if (file == null) return;

        var path = file.TryGetLocalPath();
        if (path == null) return;

        if (DataContext is MainWindowViewModel vm)
            await vm.ExportBomAsync(path);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var isCsv = e.DataTransfer.Formats.Contains(DataFormat.File)
                 && e.DataTransfer.TryGetFiles()
                        ?.Any(f => f.Name.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase)) == true;

        e.DragEffects = isCsv ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Formats.Contains(DataFormat.File)) return;

        var file = e.DataTransfer.TryGetFiles()
            ?.FirstOrDefault(f => f.Name.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase));

        if (file == null) return;

        var path = file.TryGetLocalPath();
        if (path == null) return;

        if (DataContext is MainWindowViewModel vm)
            vm.StagedFilePath = path;
    }
}
