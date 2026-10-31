using CommunityToolkit.Mvvm.ComponentModel;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Import;

namespace CRM_Master.App.ViewModels;

/// <summary>An entry of the "maps to" drop-down.</summary>
public sealed record TargetOption(string Label, ContactField? Field, bool IsCustom)
{
    public static readonly TargetOption Ignore = new("(ignore this column)", null, false);
    public static readonly TargetOption Custom = new("Custom field...", null, true);
    public static IReadOnlyList<TargetOption> All { get; } =
        new[] { Ignore }.Concat(Contact.Fields.Select(f => new TargetOption(f.Label, f.Field, false))).Append(Custom).ToList();
    public static TargetOption For(ColumnMapping m) =>
        m.Target is { } t ? All.First(o => o.Field == t) : m.CustomFieldName is not null ? Custom : Ignore;
}

/// <summary>Editable mapping of one source column (shared by Import and Bulk Update).</summary>
public sealed partial class MappingRow : ObservableObject
{
    public MappingRow(ColumnMapping m, ImportTable table)
    {
        SourceIndex = m.SourceIndex; Header = m.SourceHeader; Confidence = m.Confidence; Reason = m.Reason;
        Samples = string.Join("  |  ", table.Rows.Select(r => m.SourceIndex < r.Length ? r[m.SourceIndex] : null).Where(v => !string.IsNullOrWhiteSpace(v)).Take(3).Select(v => v!.Length > 28 ? v[..28] + "..." : v));
        _selected = TargetOption.For(m);
        _customName = m.CustomFieldName ?? Header;
    }

    public int SourceIndex { get; }
    public string Header { get; }
    public string Samples { get; }
    public double Confidence { get; private set; }
    public string Reason { get; private set; }
    public IReadOnlyList<TargetOption> Options => TargetOption.All;

    [ObservableProperty] private TargetOption _selected;
    [ObservableProperty] private string _customName;

    public bool IsCustom => Selected.IsCustom;
    public string ConfidenceText => Selected.IsCustom || Selected.Field is null ? "" : Confidence >= 1 ? "Exact match" : Confidence > 0 ? $"Suggested ({Confidence:P0})" : "Manual";
    partial void OnSelectedChanged(TargetOption value) { OnPropertyChanged(nameof(IsCustom)); OnPropertyChanged(nameof(ConfidenceText)); }

    public ColumnMapping ToMapping() => new()
    {
        SourceIndex = SourceIndex, SourceHeader = Header, Confidence = Confidence, Reason = Reason,
        Target = Selected.Field, CustomFieldName = Selected.IsCustom ? (TextCleaner.Clean(CustomName) ?? Header) : null
    };
}
