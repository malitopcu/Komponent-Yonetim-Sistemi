using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;
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
    }

    // "CSV Seç" butonu: dosya seçtir, yolu ViewModel'e teslim et.
    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
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
            await vm.ImportFileAsync(path);
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

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Formats.Contains(DataFormat.File)) return;

        var file = e.DataTransfer.TryGetFiles()
            ?.FirstOrDefault(f => f.Name.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase));

        if (file == null) return;

        var path = file.TryGetLocalPath();
        if (path == null) return;

        if (DataContext is MainWindowViewModel vm)
            await vm.ImportFileAsync(path);
    }
}