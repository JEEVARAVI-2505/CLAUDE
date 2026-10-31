using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using CRM_Master.App.ViewModels;

namespace CRM_Master.App.Views;

public partial class MainWindow : Window
{
    private const double ExpandedWidth = 248, CollapsedWidth = 68;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.PropertyChanged += OnVmChanged;
        Loaded += async (_, _) => await vm.NavigateAsync(Services.Module.Dashboard);
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsSidebarCollapsed) && sender is MainViewModel vm)
        {
            var anim = new DoubleAnimation(vm.IsSidebarCollapsed ? CollapsedWidth : ExpandedWidth, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() };
            Sidebar.BeginAnimation(WidthProperty, anim);
            BrandText.Visibility = vm.IsSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }
        else if (e.PropertyName == nameof(MainViewModel.Current))
        {
            ModuleHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(160)));   // subtle fade between modules
        }
    }

    private void OnProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.ContextMenu is null) return;
        b.ContextMenu.DataContext = DataContext;
        b.ContextMenu.PlacementTarget = b; b.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom; b.ContextMenu.IsOpen = true;
    }
}
