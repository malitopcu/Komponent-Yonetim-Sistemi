using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KomponentSistemi.UI.ViewModels;
using KomponentSistemi.UI.Views;
using KomponentSistemi.Services;
using KomponentSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Veritabanı yoksa oluştur, bekleyen migration'ları uygula, tohum veriyi yükle.
        // Böylece klonla-çalıştır tek adımda olur; ayrı "dotnet ef database update" gerekmez.
        // Zaten uygulanmış migration'lar atlanır, mevcut veri bozulmaz.
        using (var db = new AppDbContext())
            db.Database.Migrate();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    ServiceFactory.CreateComponentQueryService(),
                    ServiceFactory.CreateSearchService(),
                    ServiceFactory.CreateBomService(),
                    ServiceFactory.CreateDataManagementService()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

}