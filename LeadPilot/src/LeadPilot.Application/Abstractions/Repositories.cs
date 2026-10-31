using LeadPilot.Application.Duplicates;
using LeadPilot.Application.Models;
using LeadPilot.Domain;

namespace LeadPilot.Application.Abstractions;

/// <summary>Leads, businesses, contacts, sources and evidence.</summary>
public interface ILeadRepository
{
    LeadPage Search(LeadQuery q);
    /// <summary>All rows matching the query (unpaged), optionally restricted to specific ids. Used by export.</summary>
    IReadOnlyList<LeadRow> Export(LeadQuery q, IReadOnlyCollection<int>? ids = null);
    LeadDetail? GetDetail(int leadId);
    Lead? GetLead(int leadId);
    Business? GetBusiness(int businessId);
    /// <summary>Inserts business, contact, lead (assigning LeadCode), source, evidence and activity in one transaction.</summary>
    int Insert(LeadInsert insert);
    void UpdateBusiness(Business b);
    void UpdateLead(Lead l);
    int AddContact(Contact c);
    void UpdateContact(Contact c);
    void AddSource(LeadSourceRecord s);
    void AddEvidence(FieldEvidence e);
    void BulkSetStatus(IReadOnlyCollection<int> ids, LeadStatus status);
    void BulkAssign(IReadOnlyCollection<int> ids, int? userId);
    void Delete(IReadOnlyCollection<int> ids);
    IReadOnlyList<MatchProfile> GetMatchProfiles();
    MatchProfile? GetMatchProfile(int leadId);
    FilterOptions GetFilterOptions();
    int Count(bool includeArchived = false);
    /// <summary>Deletes archived/closed leads last updated before the cutoff (data-retention policy). Returns rows removed.</summary>
    int PurgeClosedBefore(DateTime cutoffUtc);
}

/// <summary>Activities, follow-up tasks and campaigns.</summary>
public interface IWorkRepository
{
    void AddActivity(LeadActivity a);
    IReadOnlyList<LeadActivity> GetActivities(int leadId);
    int AddTask(FollowUpTask t);
    void UpdateTask(FollowUpTask t);
    FollowUpTask? GetTask(int id);
    IReadOnlyList<TaskRow> GetTasks(TaskQuery q, DateTime nowUtc);
    /// <summary>Recomputes Lead.NextFollowUpAt from open tasks.</summary>
    void RefreshNextFollowUp(int leadId);
    int AddCampaign(Campaign c);
    void UpdateCampaign(Campaign c);
    Campaign? GetCampaign(int id);
    IReadOnlyList<Campaign> GetCampaigns();
    void DeleteCampaign(int id);
    int LeadCountForCampaign(int campaignId);
}

/// <summary>Duplicate candidates and merge history.</summary>
public interface IDuplicateRepository
{
    /// <summary>Stores new matches; pairs already decided are left untouched. Returns the number inserted.</summary>
    int UpsertMatches(IEnumerable<DuplicateResult> matches);
    IReadOnlyList<DuplicateView> GetMatches(DuplicateDecision decision);
    int CountPending();
    void SetDecision(int matchId, DuplicateDecision decision, string? by);
    int ApplyMerge(MergePlan plan);
    /// <summary>Reverses a merge using the stored snapshots. Throws if already undone.</summary>
    void UndoMerge(int mergeId, string? by);
    IReadOnlyList<MergeRecord> GetMerges();
}

/// <summary>Settings, users, audit log, import history, API usage and destructive data operations.</summary>
public interface ISystemRepository
{
    string? GetSetting(string key);
    void SetSetting(string key, string value);
    int AddUser(SalesUser u);
    void UpdateUser(SalesUser u);
    IReadOnlyList<SalesUser> GetUsers();
    SalesUser? GetUser(int id);
    void AddAudit(AuditEntry e);
    IReadOnlyList<AuditEntry> GetAudit(int limit);
    int AddImport(ImportRecord r);
    IReadOnlyList<ImportRecord> GetImports(int limit);
    void AddApiUsage(ApiUsageRecord r);
    IReadOnlyList<ApiUsageRecord> GetApiUsage(int limit);
    /// <summary>Deletes every lead, business, contact, task, activity, campaign, import and match. Users, settings and the audit log are kept.</summary>
    void DeleteAllBusinessData();
}

/// <summary>Aggregations for dashboard and analytics.</summary>
public interface IStatsRepository
{
    DashboardStats GetDashboard(DateTime nowUtc);
    IReadOnlyList<SourcePerformance> GetSourcePerformance();
}
