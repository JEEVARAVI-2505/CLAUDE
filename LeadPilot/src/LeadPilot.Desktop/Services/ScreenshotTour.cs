using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LeadPilot.Application.ViewModels;

namespace LeadPilot.Desktop.Services;

/// <summary>
/// Diagnostics for release testing (<c>LeadPilotAI.exe --screenshot-dir DIR</c>): finishes the setup wizard with test values on a
/// scratch data folder, visits every screen, saves a PNG of each and exits. Never used in normal operation.
/// </summary>
public static class ScreenshotTour
{
    public static async Task RunAsync(Window win, MainViewModel main, string dir)
    {
        Directory.CreateDirectory(dir);
        await Settle(); Save(win, Path.Combine(dir, "00-setup-welcome.png"));
        if (main.CurrentPage is SetupViewModel setup)
        {
            setup.NextCommand.Execute(null); setup.OwnerName = "Release Tester"; setup.CompanyName = "Acme Solar";
            setup.NextCommand.Execute(null); await Settle(); Save(win, Path.Combine(dir, "01-setup-sources.png"));
            setup.FinishCommand.Execute(null); await Settle(2000);
        }
        var i = 2;
        foreach (var key in new[] { PageKey.Dashboard, PageKey.LeadFinder, PageKey.LeadDatabase, PageKey.Duplicates, PageKey.Enrichment, PageKey.Qualification, PageKey.FollowUps, PageKey.Campaigns,
                                    PageKey.Analytics, PageKey.Assistant, PageKey.Import, PageKey.Export, PageKey.Integrations, PageKey.Settings, PageKey.Backup, PageKey.About })
        {
            main.Navigate(key); await Settle(1800);
            Save(win, Path.Combine(dir, $"{i++:00}-{key}.png"));
        }
        main.ToggleThemeCommand.Execute(null); main.Navigate(PageKey.Dashboard); await Settle(1800);
        Save(win, Path.Combine(dir, $"{i:00}-Dashboard-dark.png"));
        System.Windows.Application.Current.Shutdown();
    }

    private static Task Settle(int ms = 1800) => Task.Delay(ms);

    private static void Save(Window w, string path)
    {
        w.UpdateLayout();
        var width = (int)Math.Max(1, w.ActualWidth); var height = (int)Math.Max(1, w.ActualHeight);
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(w);
        var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path); enc.Save(fs);
    }
}
