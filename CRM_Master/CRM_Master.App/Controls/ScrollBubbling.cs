using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CRM_Master.App.Controls;

/// <summary>
/// A DataGrid inside a scrolling page normally swallows the mouse wheel even when it has nothing left to scroll,
/// "trapping" the page. With this behaviour the wheel passes to the parent once the grid is at its limit.
/// </summary>
public static class ScrollBubbling
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(ScrollBubbling), new PropertyMetadata(false, OnChanged));
    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool v) => d.SetValue(EnabledProperty, v);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement el) return;
        el.PreviewMouseWheel -= OnWheel;
        if (e.NewValue is true) el.PreviewMouseWheel += OnWheel;
    }

    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not UIElement el) return;
        var inner = FindScrollViewer(el);
        if (inner is { ScrollableHeight: > 0 } && ((e.Delta > 0 && inner.VerticalOffset > 0) || (e.Delta < 0 && inner.VerticalOffset < inner.ScrollableHeight))) return;   // grid can still scroll itself

        e.Handled = true;
        var parent = (el as FrameworkElement)?.Parent as UIElement ?? VisualTreeHelper.GetParent(el) as UIElement;
        parent?.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent, Source = sender });
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject d)
    {
        if (d is ScrollViewer sv) return sv;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            if (FindScrollViewer(VisualTreeHelper.GetChild(d, i)) is { } found) return found;
        return null;
    }
}
