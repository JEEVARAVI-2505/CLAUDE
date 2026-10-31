using System.Windows;

namespace CRM_Master.App.Views;

public enum MessageKind { Info, Question, Danger, Error }

/// <summary>Themed replacement for MessageBox (confirmations, errors, information).</summary>
public partial class MessageWindow : Window
{
    public MessageWindow(string title, string message, string okText, string? cancelText, MessageKind kind)
    {
        InitializeComponent();
        TitleText.Text = title; MessageText.Text = message; OkButton.Content = okText;
        if (cancelText is null) CancelButton.Visibility = Visibility.Collapsed; else CancelButton.Content = cancelText;
        var (bg, fg, icon) = kind switch
        {
            MessageKind.Error => ("Brush.ErrorBg", "Brush.Error", "Icon.Warning"),
            MessageKind.Danger => ("Brush.ErrorBg", "Brush.Error", "Icon.Trash"),
            MessageKind.Question => ("Brush.InfoBg", "Brush.Info", "Icon.About"),
            _ => ("Brush.InfoBg", "Brush.Info", "Icon.Check")
        };
        IconHost.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, bg);
        IconPath.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, fg);
        IconPath.SetResourceReference(System.Windows.Shapes.Path.DataProperty, icon);
        if (kind == MessageKind.Danger) OkButton.Style = (Style)FindResource("Button.Danger");
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
