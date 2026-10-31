using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;

namespace CRM_Master.Core.Interfaces;

/// <summary>Persistence for contacts. Every mutating call writes an audit entry in the same transaction.</summary>
public interface IContactRepository
{
    Task<PagedResult<Contact>> QueryAsync(ContactQuery query, CancellationToken ct = default);
    Task<List<int>> QueryIdsAsync(ContactQuery query, CancellationToken ct = default);
    Task<Contact?> GetAsync(int id, CancellationToken ct = default);
    Task<List<Contact>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<List<Contact>> GetAllAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<Contact> AddAsync(Contact contact, IDictionary<string, string?>? customValues = null, CancellationToken ct = default);
    Task UpdateAsync(Contact updated, IDictionary<string, string?>? customValues = null, CancellationToken ct = default);
    Task<int> DeleteAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<ContactHistory> GetHistoryAsync(int contactId, CancellationToken ct = default);
    Task<List<string>> GetDistinctAsync(ContactField field, CancellationToken ct = default);
    Task<Dictionary<string, string?>> GetCustomValuesAsync(int contactId, CancellationToken ct = default);
    Task<List<CustomField>> GetCustomFieldsAsync(CancellationToken ct = default);
    Task<int> ReevaluateQualityAsync(CancellationToken ct = default);
}

/// <summary>Reads and writes the audit trail.</summary>
public interface IAuditService
{
    Task WriteAsync(AuditLog entry, CancellationToken ct = default);
    Task<PagedResult<AuditLog>> QueryAsync(AuditQuery query, CancellationToken ct = default);
    Task<List<AuditLog>> QueryAllAsync(AuditQuery query, int maxRows, CancellationToken ct = default);
    Task<int> PurgeOlderThanAsync(int retentionDays, CancellationToken ct = default);
}

/// <summary>Typed application settings backed by the ApplicationSettings table.</summary>
public interface ISettingsService
{
    AppSettings Current { get; }
    event EventHandler? Changed;
    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}

/// <summary>Consistent database backup / restore.</summary>
public interface IBackupService
{
    Task<BackupRecord> CreateAsync(BackupKind kind, string? reason, CancellationToken ct = default);
    Task<BackupRecord> CreateAtAsync(string destinationFile, string? reason, CancellationToken ct = default);
    Task<List<BackupRecord>> ListAsync(CancellationToken ct = default);
    Task RestoreAsync(string backupFile, CancellationToken ct = default);
}

/// <summary>Computes dashboard statistics from the live database.</summary>
public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync(CancellationToken ct = default);
}
