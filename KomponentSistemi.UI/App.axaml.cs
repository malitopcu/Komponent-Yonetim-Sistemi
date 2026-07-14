using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KomponentSistemi.UI.ViewModels;
using KomponentSistemi.UI.Views;
using KomponentSistemi.Services;

namespace KomponentSistemi.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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