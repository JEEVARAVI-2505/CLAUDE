using System.Diagnostics;
using System.Windows;
using CRM_Master.App.Views;
using Microsoft.Win32;

namespace CRM_Master.App.Services;

public interface IDialogService
{
    bool Confirm(string title, string message, string confirmText = "OK", bool danger = false);
    void Info(string title, string message);
    void Error(string title, string message);
    string? PickOpenFile(string filter, string? title = null);
    string? PickSaveFile(string suggestedName, string filter, string? initialFolder = null);
    string? PickFolder(string? title = null);
    bool? ShowWindow(Window window);
    void OpenInShell(string pathOrUrl);
}

/// <summary>Styled message dialogs and the standard file pickers. Must be used on the UI thread.</summary>
public sealed class DialogService : IDialogService
{
    private static Window? Owner => Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

    public bool Confirm(string title, string message, string confirmText = "OK", bool danger = false) =>
        new MessageWindow(title, message, confirmText, "Cancel", danger ? MessageKind.Danger : MessageKind.Question) { Owner = Owner }.ShowDialog() == true;

    public void Info(string title, string message) =>
        new MessageWindow(title, message, "OK", null, MessageKind.Info) { Owner = Owner }.ShowDialog();

    public void Error(string title, string message) =>
        new MessageWindow(title, message, "OK", null, MessageKind.Error) { Owner = Owner }.ShowDialog();

    public string? PickOpenFile(string filter, string? title = null)
    {
        var d = new OpenFileDialog { Filter = filter, Title = title ?? "Select a file", CheckFileExists = true };
        return d.ShowDialog(Owner) == true ? d.FileName : null;
    }

    public string? PickSaveFile(string suggestedName, string filter, string? initialFolder = null)
    {
        var d = new SaveFileDialog { Filter = filter, FileName = suggestedName, OverwritePrompt = true, AddExtension = true };
        if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder)) d.InitialDirectory = initialFolder;
        return d.ShowDialog(Owner) == true ? d.FileName : null;
    }

    public string? PickFolder(string? title = null)
    {
        var d = new OpenFolderDialog { Title = title ?? "Select a folder" };
        return d.ShowDialog(Owner) == true ? d.FolderName : null;
    }

    public bool? ShowWindow(Window window) { window.Owner ??= Owner; return window.ShowDialog(); }

    public void OpenInShell(string pathOrUrl)
    {
        try { Process.Start(new ProcessStartInfo(pathOrUrl) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or FileNotFoundException)
        { Error("Cannot open", "Windows could not open it: " + ex.Message); }
    }
}
