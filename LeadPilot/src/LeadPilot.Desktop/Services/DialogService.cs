using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using LeadPilot.Application.Abstractions;
using Microsoft.Win32;

namespace LeadPilot.Desktop.Services;

/// <summary>WPF implementation of the dialogs the view-models need.</summary>
public sealed class DialogService : IDialogService
{
    private static Window? Owner => System.Windows.Application.Current?.MainWindow;

    public string? PickOpenFile(string title, string filter)
    {
        var d = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
        return d.ShowDialog(Owner) == true ? d.FileName : null;
    }

    public string? PickSaveFile(string title, string filter, string defaultName)
    {
        var d = new SaveFileDialog { Title = title, Filter = filter, FileName = defaultName, OverwritePrompt = true };
        return d.ShowDialog(Owner) == true ? d.FileName : null;
    }

    public string? PickFolder(string title)
    {
        var d = new OpenFolderDialog { Title = title };
        return d.ShowDialog(Owner) == true ? d.FolderName : null;
    }

    public bool Confirm(string title, string message) => MessageBox.Show(Owner!, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    public void Info(string title, string message) => MessageBox.Show(Owner!, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    public void Error(string title, string message) => MessageBox.Show(Owner!, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public string? PromptPassword(string title, string message, bool confirm) => Prompt(title, message, true, null);
    public string? PromptText(string title, string message, string? defaultValue = null) => Prompt(title, message, false, defaultValue);

    private static string? Prompt(string title, string message, bool password, string? initial)
    {
        var text = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
        Control input = password ? new PasswordBox { MinWidth = 320, Padding = new Thickness(6) } : new TextBox { MinWidth = 320, Padding = new Thickness(6), Text = initial ?? "" };
        var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80 };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        buttons.Children.Add(ok); buttons.Children.Add(cancel);
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(text); panel.Children.Add(input); panel.Children.Add(buttons);
        var win = new Window
        {
            Title = title, Content = panel, SizeToContent = SizeToContent.WidthAndHeight, MaxWidth = 520, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Owner, ShowInTaskbar = false
        };
        win.SetResourceReference(Window.BackgroundProperty, "BgBrush");
        win.SetResourceReference(TextElement.ForegroundProperty, "TextBrush");
        ok.Click += (_, _) => win.DialogResult = true;
        win.Loaded += (_, _) => input.Focus();
        return win.ShowDialog() == true ? (input is PasswordBox pb ? pb.Password : ((TextBox)input).Text) : null;
    }

    /// <summary>Opens only web links, mail/WhatsApp hand-off links and local folders.</summary>
    public void OpenUrl(string url)
    {
        var ok = Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme is "http" or "https" or "mailto") || Directory.Exists(url);
        if (!ok) { Error("Cannot open", "This link type is not allowed."); return; }
        try { System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { Error("Cannot open", ex.Message); }
    }

    public void CopyToClipboard(string text)
    {
        try { Clipboard.SetText(text); }
        catch (System.Runtime.InteropServices.COMException) { Error("Clipboard", "The clipboard is busy. Try again."); }
    }

    public void SetTheme(string theme) => ThemeManager.Apply(theme);
}

/// <summary>Switches the colour dictionary at runtime (all brushes are DynamicResource).</summary>
public static class ThemeManager
{
    public static void Apply(string theme)
    {
        var app = System.Windows.Application.Current;
        var uri = new Uri(theme == "Dark" ? "Themes/Colors.Dark.xaml" : "Themes/Colors.Light.xaml", UriKind.Relative);
        var merged = app.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Colors.", StringComparison.Ordinal));
        var index = existing == null ? 0 : merged.IndexOf(existing);
        if (existing != null) merged.Remove(existing);
        merged.Insert(index, new ResourceDictionary { Source = uri });
    }
}
