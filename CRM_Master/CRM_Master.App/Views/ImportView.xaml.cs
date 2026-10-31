using System.Windows;
using System.Windows.Controls;
using CRM_Master.App.ViewModels;

namespace CRM_Master.App.Views;

public partial class ImportView : UserControl
{
    public ImportView() => InitializeComponent();

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        DropBorder.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, e.Effects == DragDropEffects.Copy ? "Brush.Secondary" : "Brush.Error");
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e) => DropBorder.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "Brush.BorderStrong");

    private async void OnDrop(object sender, DragEventArgs e)
    {
        DropBorder.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "Brush.BorderStrong");
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files && DataContext is ImportViewModel vm)
            await vm.LoadDroppedCommand.ExecuteAsync(files[0]);
    }
}
