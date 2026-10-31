using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Engine.Automation;

namespace CRM_Master.App.ViewModels;

public sealed partial class CustomFieldEntry : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string? _value;
}

/// <summary>Contact profile: personal / company / contact / lead details, notes, custom fields and activity history.</summary>
public sealed partial class ContactEditViewModel : ObservableObject
{
    private readonly UiServices _ui;
    private readonly IContactRepository _repo;
    private readonly AutomationService _automation;

    public ContactEditViewModel(UiServices ui, IContactRepository repo, AutomationService automation, Contact? existing, bool readOnly)
    {
        _ui = ui; _repo = repo; _automation = automation;
        IsNew = existing is null; IsReadOnly = readOnly && existing is not null;
        Model = existing is null ? new Contact() : Clone(existing);
        _createdDateText = Model.Get(ContactField.CreatedDate) ?? "";
        StatusOptions = new List<string> { "Lead", "Prospect", "Customer", "Inactive", "Lost" };
        if (!StatusOptions.Contains(Model.Status)) StatusOptions.Add(Model.Status);
    }

    private static Contact Clone(Contact c)
    {
        var copy = c.Snapshot(); copy.Id = c.Id; copy.CreatedAt = c.CreatedAt; copy.UpdatedAt = c.UpdatedAt; copy.ImportJobId = c.ImportJobId;
        copy.EmailState = c.EmailState; copy.PhoneState = c.PhoneState; copy.NeedsReview = c.NeedsReview; copy.IsDuplicateFlagged = c.IsDuplicateFlagged; copy.IsIncomplete = c.IsIncomplete;
        return copy;
    }

    public Contact Model { get; }
    public List<string> StatusOptions { get; }
    public ObservableCollection<CustomFieldEntry> CustomFields { get; } = new();
    public ObservableCollection<AuditLog> History { get; } = new();
    public bool IsNew { get; }
    public bool WasSaved { get; private set; }
    public event Action? RequestClose;

    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private string _createdDateText;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _importInfo;
    [ObservableProperty] private string _newFieldName = "";

    public string Title => IsNew ? "New contact" : Model.DisplayName;
    public string CreatedText => IsNew ? "Not saved yet" : Model.CreatedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
    public string UpdatedText => IsNew ? "-" : Model.UpdatedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
    public bool CanEdit => !IsReadOnly;
    partial void OnIsReadOnlyChanged(bool value) => OnPropertyChanged(nameof(CanEdit));

    public async Task LoadAsync()
    {
        if (IsNew) return;
        foreach (var kv in await _repo.GetCustomValuesAsync(Model.Id)) CustomFields.Add(new CustomFieldEntry { Name = kv.Key, Value = kv.Value });
        var history = await _repo.GetHistoryAsync(Model.Id);
        foreach (var h in history.Entries) History.Add(h);
        ImportInfo = history.SourceImport is { } j ? $"Imported from {j.FileName} on {j.StartedAt.ToLocalTime():dd MMM yyyy}" : null;
    }

    [RelayCommand] private void StartEdit() => IsReadOnly = false;
    [RelayCommand] private void Close() => RequestClose?.Invoke();

    [RelayCommand]
    private void AddCustomField()
    {
        var name = TextCleaner.Clean(NewFieldName);
        if (name is null) return;
        if (CustomFields.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))) { Error = $"A custom field named '{name}' already exists."; return; }
        CustomFields.Add(new CustomFieldEntry { Name = name }); NewFieldName = ""; Error = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Error = null;
        if (!Model.Set(ContactField.CreatedDate, string.IsNullOrWhiteSpace(CreatedDateText) ? null : CreatedDateText.Trim()))
        { Error = "Created date is not a valid date. Use a format like 2024-03-31 or 31/03/2024."; return; }
        // trim obvious whitespace so lookups and duplicate checks work
        foreach (var f in Contact.Fields) if (f.Field != ContactField.CreatedDate && Model.Get(f.Field) is { } v) Model.Set(f.Field, f.IsMultiline ? TextCleaner.CleanMultiline(v) : TextCleaner.Clean(v));
        if (!ContactQuality.HasName(Model) && Model.Email is null && Model.Phone is null) { Error = "Enter at least a name, an email address or a phone number."; return; }

        var problems = new List<string>();
        var cc = _ui.Settings.Current.DefaultCountryCode;
        if (Model.Email is { } em && !EmailRules.Check(em).IsValid) problems.Add($"The email '{em}' does not look valid.");
        if (Model.Phone is { } ph && !PhoneNumberParser.Parse(ph, cc).IsValid) problems.Add($"The phone number '{ph}' does not look valid.");
        if (problems.Count > 0 && !_ui.Dialogs.Confirm("Save with warnings?", string.Join("\n", problems) + "\n\nThe contact can still be saved and will be flagged in data-quality reports.", "Save anyway")) return;

        try
        {
            if (IsNew)
            {
                var dup = await FindDuplicateAsync();
                if (dup is not null && !_ui.Dialogs.Confirm("Possible duplicate", $"A contact with the same email or phone already exists:\n\n{dup.DisplayName} (#{dup.Id})\n\nCreate a new contact anyway?", "Create anyway")) return;
                var values = CustomFields.ToDictionary(f => f.Name, f => f.Value);
                var saved = await _repo.AddAsync(Model, values);
                await _automation.ApplyToSavedContactAsync(saved.Id, AutomationTrigger.ContactCreated, _repo);
            }
            else
            {
                await _repo.UpdateAsync(Model, CustomFields.ToDictionary(f => f.Name, f => f.Value));
                await _automation.ApplyToSavedContactAsync(Model.Id, AutomationTrigger.ContactUpdated, _repo);
            }
            WasSaved = true;
            _ui.Notifications.Add(NotificationLevel.Success, IsNew ? "Contact created" : "Contact updated", Model.DisplayName);
            RequestClose?.Invoke();
        }
        catch (InvalidOperationException ex) { Error = ex.Message; }
        catch (Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException or IOException)
        { Serilog.Log.Error(ex, "Saving contact failed"); Error = "The contact could not be saved: " + (ex.InnerException?.Message ?? ex.Message); }
    }

    private async Task<Contact?> FindDuplicateAsync()
    {
        var cc = _ui.Settings.Current.DefaultCountryCode;
        var emailKey = ContactKeys.EmailKey(Model.Email); var phoneKey = ContactKeys.PhoneKey(Model.Phone, cc);
        if (emailKey is null && phoneKey is null) return null;
        var term = emailKey ?? new string(Model.Phone!.Where(char.IsDigit).ToArray());
        var hits = await _repo.QueryAsync(new(Search: term, PageSize: 25));
        return hits.Items.FirstOrDefault(h => (emailKey is not null && h.EmailKey == emailKey) || (phoneKey is not null && (h.PhoneKey == phoneKey || h.AlternatePhoneKey == phoneKey)));
    }
}
