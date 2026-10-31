namespace CRM_Master.Core.DTOs;

/// <summary>What to do when an incoming row matches an existing contact during import.</summary>
public enum ImportDuplicatePolicy { Skip, ImportAndFlag, ProposeUpdate }

/// <summary>User-configurable application settings, persisted one JSON value per property in ApplicationSettings.</summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "Light";
    public string DisplayName { get; set; } = "Local User";
    public string DefaultCountryCode { get; set; } = "91";
    public string BackupFolder { get; set; } = "";
    public string ExportFolder { get; set; } = "";
    public double DuplicateHighThreshold { get; set; } = 0.90;
    public double DuplicatePossibleThreshold { get; set; } = 0.80;
    public double DuplicateLowThreshold { get; set; } = 0.70;
    public List<string> RequiredFields { get; set; } = new() { "FullName", "Email" };
    public bool ValidateWebsite { get; set; } = true;
    public bool ValidateCountry { get; set; } = true;
    public bool CleanOnImport { get; set; } = true;
    public ImportDuplicatePolicy ImportDuplicatePolicy { get; set; } = ImportDuplicatePolicy.Skip;
    public bool AutomationEnabled { get; set; } = true;
    public int LogRetentionDays { get; set; } = 365;
    public bool AutoBackupBeforeBulk { get; set; } = true;
    public int BackupsToKeep { get; set; } = 10;
    public int PageSize { get; set; } = 100;
    public bool FirstRunCompleted { get; set; }
    public DateTime? FirstRunUtc { get; set; }
}
