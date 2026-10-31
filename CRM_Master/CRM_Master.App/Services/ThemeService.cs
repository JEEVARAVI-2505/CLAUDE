using System.Windows;

namespace CRM_Master.App.Services;

/// <summary>Switches between the Light and Dark colour dictionaries at runtime.</summary>
public sealed class ThemeService
{
    public const string Light = "Light", Dark = "Dark";
    public string Current { get; private set; } = Light;
    public event Action<string>? ThemeChanged;

    public void Apply(string theme)
    {
        theme = theme == Dark ? Dark : Light;
        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d => d.Source?.OriginalString.EndsWith("Themes/Light.xaml") == true || d.Source?.OriginalString.EndsWith("Themes/Dark.xaml") == true);
        var fresh = new ResourceDictionary { Source = new Uri($"pack://application:,,,/CRM_Master;component/Themes/{theme}.xaml", UriKind.Absolute) };
        if (existing is null) dicts.Insert(0, fresh); else dicts[dicts.IndexOf(existing)] = fresh;
        Current = theme;
        ThemeChanged?.Invoke(theme);
    }

    public void Toggle() => Apply(Current == Light ? Dark : Light);
}
