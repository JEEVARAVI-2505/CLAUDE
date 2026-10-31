using System.Windows;
using System.Windows.Controls;
using CRM_Master.App.ViewModels;

namespace CRM_Master.App.Views;

public partial class ContactsView : UserControl
{
    public ContactsView() => InitializeComponent();

    // Sorting is done by the database (so it works across all pages), not by the DataGrid.
    private void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (DataContext is ContactsViewModel vm && e.Column.SortMemberPath is { Length: > 0 } col) vm.SortCommand.Execute(col);
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.ContextMenu is null) return;
        b.ContextMenu.DataContext = DataContext; b.ContextMenu.PlacementTarget = b;
        b.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom; b.ContextMenu.IsOpen = true;
    }
}
