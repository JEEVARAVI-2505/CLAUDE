using System.Windows;
using BuildingStudio.App.Wpf.ViewModels;
using BuildingStudio.Studio.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingStudio.App.Wpf;

/// <summary>Composition root: engines are registered once and injected into view models.</summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();
        services.AddSingleton(_ => StudioService.Create());
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "AI Building Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
        Services.GetRequiredService<MainWindow>().Show();
    }
}
