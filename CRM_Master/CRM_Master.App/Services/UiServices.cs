using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CRM_Master.Core.Interfaces;

namespace CRM_Master.App.Services;

public enum NotificationLevel { Info, Success, Warning, Error }

public sealed record NotificationItem(DateTime Time, NotificationLevel Level, string Title, string Message)
{
    public string TimeText => Time.ToString("HH:mm");
}

/// <summary>In-app notification list shown by the bell icon.</summary>
public interface INotificationCenter : INotifyPropertyChanged
{
    ObservableCollection<NotificationItem> Items { get; }
    int UnreadCount { get; }
    void Add(NotificationLevel level, string title, string message);
    void MarkAllRead();
    void Clear();
}

public sealed class NotificationCenter : INotificationCenter
{
    private int _unread;
    public ObservableCollection<NotificationItem> Items { get; } = new();
    public int UnreadCount { get => _unread; private set { if (_unread != value) { _unread = value; OnChanged(); } } }
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Add(NotificationLevel level, string title, string message)
    {
        void Do() { Items.Insert(0, new NotificationItem(DateTime.Now, level, title, message)); while (Items.Count > 50) Items.RemoveAt(Items.Count - 1); UnreadCount++; }
        if (System.Windows.Application.Current?.Dispatcher is { } d && !d.CheckAccess()) d.Invoke(Do); else Do();
    }
    public void MarkAllRead() => UnreadCount = 0;
    public void Clear() { Items.Clear(); UnreadCount = 0; }
    private void OnChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/// <summary>Tracks "local processing" state for the top bar.</summary>
public interface IBusyService : INotifyPropertyChanged
{
    bool IsBusy { get; }
    string StatusText { get; }
    IDisposable Begin(string text);
}

public sealed class BusyService : IBusyService
{
    private readonly List<string> _active = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsBusy => _active.Count > 0;
    public string StatusText => _active.Count == 0 ? "Idle" : _active[^1];

    public IDisposable Begin(string text)
    {
        Update(() => _active.Add(text));
        return new Scope(() => Update(() => _active.Remove(text)));
    }

    private void Update(Action change)
    {
        void Do() { change(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText))); }
        if (System.Windows.Application.Current?.Dispatcher is { } d && !d.CheckAccess()) d.Invoke(Do); else Do();
    }
    private sealed class Scope : IDisposable { private Action? _a; public Scope(Action a) => _a = a; public void Dispose() { _a?.Invoke(); _a = null; } }
}

/// <summary>Modules the sidebar can navigate to.</summary>
public enum Module { Dashboard, Import, Cleaning, Duplicates, Contacts, BulkUpdate, Validation, Automation, Reports, Activity, Settings, About }

public interface INavigationService
{
    event Action<Module>? NavigationRequested;
    void Navigate(Module module);
}

public sealed class NavigationService : INavigationService
{
    public event Action<Module>? NavigationRequested;
    public void Navigate(Module module) => NavigationRequested?.Invoke(module);
}

/// <summary>Everything a view-model needs from the shell, in one injectable bundle.</summary>
public sealed class UiServices
{
    public UiServices(IDialogService dialogs, IBusyService busy, INotificationCenter notifications, INavigationService navigation, IAuditService audit, ISettingsService settings)
    { Dialogs = dialogs; Busy = busy; Notifications = notifications; Navigation = navigation; Audit = audit; Settings = settings; }
    public IDialogService Dialogs { get; }
    public IBusyService Busy { get; }
    public INotificationCenter Notifications { get; }
    public INavigationService Navigation { get; }
    public IAuditService Audit { get; }
    public ISettingsService Settings { get; }
}
