using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;
using System.Threading.Tasks;
using KomponentSistemi.UI.ViewModels;

namespace KomponentSistemi.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Sürükle-bırak olaylarını pencereye bağla
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // Tip seçilince "Değer"/"2. Değer" sütun başlıklarını dinamik güncelle.
        // (DataGrid sütunları görsel ağaçta olmadığı için binding yerine kod-arkası.)
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
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
            }
        };
    }

    // "CSV Seç" butonu ya da sürükle-bırak alanına tıklama → dosya seçici.
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

        if (DataContext is MainWindowViewModel vm)
            vm.StagedFilePath = path;   // içe aktarma değil, sadece seç (staged)
    }

    // "CSV Dışa Aktar" butonu: kaydetme yeri sordur, yolu ViewModel'e teslim et.
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

    // Sürüklenen şey CSV mi? Değilse "bırakılamaz" imleci göster.
    // (Avalonia 12 API'si: e.DataTransfer + TryGetFiles)
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
            vm.StagedFilePath = path;   // içe aktarma değil, sadece seç (staged)
    }
}