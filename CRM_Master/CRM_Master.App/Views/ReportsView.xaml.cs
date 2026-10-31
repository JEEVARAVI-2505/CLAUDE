using System.Data;
using System.Windows.Controls;
using CRM_Master.App.ViewModels;

namespace CRM_Master.App.Views;

public partial class ReportsView : UserControl
{
    public ReportsView() => InitializeComponent();

    // Columns are generated from the DataTable; show the real report column titles (DataColumn.Caption).
    private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (PreviewGrid.ItemsSource is DataView view && view.Table is { } t && t.Columns.Contains(e.PropertyName))
            e.Column.Header = t.Columns[e.PropertyName]!.Caption;
        e.Column.MaxWidth = 380;
    }
}
