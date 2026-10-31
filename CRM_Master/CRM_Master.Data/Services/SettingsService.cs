using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.Services;

/// <summary>Stores <see cref="AppSettings"/> as one JSON value per property in the ApplicationSettings table.</summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };
    private static readonly PropertyInfo[] Props = typeof(AppSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).ToArray();
    private readonly IDbContextFactory<CrmDbContext> _factory;

    public SettingsService(IDbContextFactory<CrmDbContext> factory) { _factory = factory; Current = WithDefaults(new AppSettings()); }

    public AppSettings Current { get; private set; }
    public event EventHandler? Changed;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var rows = await ctx.ApplicationSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        var s = new AppSettings();
        foreach (var p in Props)
        {
            if (!rows.TryGetValue(p.Name, out var json)) continue;
            try { var v = JsonSerializer.Deserialize(json, p.PropertyType, Json); if (v is not null) p.SetValue(s, v); }
            catch (JsonException) { /* corrupted value: keep default */ }
        }
        Current = WithDefaults(s);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        Validate(settings);
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var existing = await ctx.ApplicationSettings.ToDictionaryAsync(s => s.Key, ct);
        var changed = new List<string>();
        foreach (var p in Props)
        {
            var json = JsonSerializer.Serialize(p.GetValue(settings), p.PropertyType, Json);
            if (existing.TryGetValue(p.Name, out var row)) { if (row.Value != json) { row.Value = json; changed.Add(p.Name); } }
            else { ctx.ApplicationSettings.Add(new ApplicationSetting { Key = p.Name, Value = json }); changed.Add(p.Name); }
        }
        if (changed.Count > 0)
            ctx.AuditLogs.Add(new AuditLog { Operation = AuditOperation.SettingsChanged, EntityType = "Settings", Summary = "Changed: " + string.Join(", ", changed.Where(c => c != nameof(AppSettings.FirstRunUtc))) });
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        Current = WithDefaults(settings.Clone());
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Throws <see cref="ArgumentException"/> with a user-readable message for unusable values.</summary>
    public static void Validate(AppSettings s)
    {
        if (s.DuplicateLowThreshold is < 0 or > 1 || s.DuplicatePossibleThreshold is < 0 or > 1 || s.DuplicateHighThreshold is < 0 or > 1)
            throw new ArgumentException("Duplicate thresholds must be between 0 and 1.");
        if (!(s.DuplicateLowThreshold < s.DuplicatePossibleThreshold && s.DuplicatePossibleThreshold < s.DuplicateHighThreshold))
            throw new ArgumentException("Thresholds must increase: low < possible < high.");
        if (s.DefaultCountryCode.Length is 0 or > 3 || !s.DefaultCountryCode.All(char.IsDigit))
            throw new ArgumentException("Phone country code must be 1-3 digits without '+' (for example 91).");
        if (s.LogRetentionDays < 1) throw new ArgumentException("Log retention must be at least 1 day.");
        if (s.BackupsToKeep < 1) throw new ArgumentException("Keep at least 1 automatic backup.");
        if (s.PageSize is < 10 or > 1000) throw new ArgumentException("Page size must be between 10 and 1000.");
        if (s.RequiredFields.Count == 0) throw new ArgumentException("Select at least one required field.");
    }

    private static AppSettings WithDefaults(AppSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.BackupFolder)) s.BackupFolder = DatabasePaths.DefaultBackupFolder;
        if (string.IsNullOrWhiteSpace(s.ExportFolder)) s.ExportFolder = DatabasePaths.DefaultExportFolder;
        return s;
    }
}
