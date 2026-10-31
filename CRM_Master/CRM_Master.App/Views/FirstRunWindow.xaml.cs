using System.Windows;
using CRM_Master.App.ViewModels;

namespace CRM_Master.App.Views;

public partial class FirstRunWindow : Window
{
    private readonly FirstRunViewModel _vm;
    public FirstRunWindow(FirstRunViewModel vm) { InitializeComponent(); DataContext = _vm = vm; }
    private void OnStart(object sender, RoutedEventArgs e) { if (_vm.Validate()) DialogResult = true; }
}
