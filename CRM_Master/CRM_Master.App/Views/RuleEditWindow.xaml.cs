using System.Windows;
using CRM_Master.App.ViewModels;

namespace CRM_Master.App.Views;

public partial class RuleEditWindow : Window
{
    public RuleEditWindow(RuleEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose += () => { DialogResult = vm.Saved; };
    }
}
