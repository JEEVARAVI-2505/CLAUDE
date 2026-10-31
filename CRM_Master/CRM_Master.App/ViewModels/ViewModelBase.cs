using CommunityToolkit.Mvvm.ComponentModel;
using CRM_Master.App.Services;
using CRM_Master.Core.Entities;
using CRM_Master.Data.Services;
using CRM_Master.Engine.Import;
using Serilog;

namespace CRM_Master.App.ViewModels;

/// <summary>Base for all module view-models: busy state plus one place that turns exceptions into friendly messages.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    protected ViewModelBase(UiServices ui) => Ui = ui;
    protected UiServices Ui { get; }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    /// <summary>Called every time the user opens the module - refresh data here.</summary>
    public virtual Task ActivateAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs <paramref name="work"/> with busy indicators. Known problems (bad file, invalid input) become clear messages; anything
    /// unexpected is logged (type and stack only - never contact data) and recorded as a failed operation in the audit log.
    /// </summary>
    protected async Task<bool> RunAsync(string busyText, Func<Task> work, AuditOperation? auditAs = null)
    {
        IsBusy = true; StatusMessage = busyText + "...";
        using var scope = Ui.Busy.Begin(busyText);
        try { await work(); StatusMessage = null; return true; }
        catch (OperationCanceledException) { StatusMessage = "Cancelled."; return false; }
        catch (ImportException ex) { StatusMessage = null; Ui.Notifications.Add(NotificationLevel.Error, busyText, ex.Message); Ui.Dialogs.Error("Could not complete: " + busyText, ex.Message); return false; }
        catch (ArgumentException ex) { StatusMessage = null; Ui.Dialogs.Error("Please check your input", ex.Message); return false; }
        catch (InvalidDataException ex) { StatusMessage = null; Ui.Dialogs.Error("Invalid file", ex.Message); return false; }
        catch (FileNotFoundException ex) { StatusMessage = null; Ui.Dialogs.Error("File not found", ex.Message); return false; }
        catch (InvalidOperationException ex) { StatusMessage = null; Ui.Dialogs.Error("Could not complete: " + busyText, ex.Message); return false; }
        catch (Exception ex)
        {
            StatusMessage = null;
            Log.Error(ex, "Operation failed: {Operation}", busyText);
            try { await Ui.Audit.WriteAsync(Audit.Entry(auditAs ?? AuditOperation.Validation, "Application", null, status: OperationStatus.Failed, summary: busyText + " failed", error: ex.GetType().Name + ": " + ex.Message)); }
            catch (Exception logEx) { Log.Warning(logEx, "Could not write failure to audit log"); }
            Ui.Notifications.Add(NotificationLevel.Error, busyText, "The operation failed. See Activity Logs.");
            Ui.Dialogs.Error("Something went wrong", $"\"{busyText}\" could not be completed.\n\n{ex.GetType().Name}: {ex.Message}\n\nThe problem was recorded in Activity Logs and the log file.");
            return false;
        }
        finally { IsBusy = false; }
    }
}
