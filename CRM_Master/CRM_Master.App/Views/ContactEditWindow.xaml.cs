using System.Windows;
using CRM_Master.App.ViewModels;

namespace CRM_Master.App.Views;

public partial class ContactEditWindow : Window
{
    public ContactEditWindow(ContactEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose += () => Close();
    }
}
