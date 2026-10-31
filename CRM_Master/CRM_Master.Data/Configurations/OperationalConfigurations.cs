using CRM_Master.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM_Master.Data.Configurations;

internal sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> b)
    {
        b.ToTable("ImportJobs", t => t.HasCheckConstraint("CK_ImportJobs_Counts",
            "TotalRows >= 0 AND ImportedRows >= 0 AND UpdatedRows >= 0 AND SkippedRows >= 0 AND DuplicateRows >= 0 AND FailedRows >= 0"));
        b.Property(j => j.FileName).HasMaxLength(260).IsRequired();
        b.Property(j => j.FilePath).HasMaxLength(1000);
        b.Property(j => j.FileType).HasMaxLength(10);
        b.Property(j => j.ErrorMessage).HasMaxLength(2000);
        b.HasIndex(j => j.StartedAt);
        b.HasMany(j => j.Records).WithOne(r => r.ImportJob).HasForeignKey(r => r.ImportJobId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ImportRecordConfiguration : IEntityTypeConfiguration<ImportRecord>
{
    public void Configure(EntityTypeBuilder<ImportRecord> b)
    {
        b.ToTable("ImportRecords");
        b.Property(r => r.Message).HasMaxLength(1000);
        b.HasIndex(r => new { r.ImportJobId, r.Status });
        b.HasIndex(r => r.ContactId);
        b.HasOne<Contact>().WithMany().HasForeignKey(r => r.ContactId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class DuplicateGroupConfiguration : IEntityTypeConfiguration<DuplicateGroup>
{
    public void Configure(EntityTypeBuilder<DuplicateGroup> b)
    {
        b.ToTable("DuplicateGroups");
        b.HasIndex(g => g.Status);
        b.HasOne<Contact>().WithMany().HasForeignKey(g => g.MasterContactId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(g => g.Matches).WithOne(m => m.Group).HasForeignKey(m => m.DuplicateGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DuplicateMatchConfiguration : IEntityTypeConfiguration<DuplicateMatch>
{
    public void Configure(EntityTypeBuilder<DuplicateMatch> b)
    {
        b.ToTable("DuplicateMatches", t =>
        {
            t.HasCheckConstraint("CK_DuplicateMatches_Score", "Score >= 0 AND Score <= 1");
            t.HasCheckConstraint("CK_DuplicateMatches_Order", "ContactAId < ContactBId");
        });
        b.Property(m => m.MatchedOn).HasMaxLength(200);
        b.HasIndex(m => new { m.ContactAId, m.ContactBId }).IsUnique();
        b.HasIndex(m => m.ContactBId);
        b.HasOne<Contact>().WithMany().HasForeignKey(m => m.ContactAId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Contact>().WithMany().HasForeignKey(m => m.ContactBId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> b)
    {
        b.ToTable("AutomationRules", t => t.HasCheckConstraint("CK_AutomationRules_Name", "length(trim(Name)) > 0"));
        b.Property(r => r.Name).HasMaxLength(150).IsRequired();
        b.Property(r => r.Description).HasMaxLength(1000);
        b.HasIndex(r => new { r.IsEnabled, r.Trigger });
    }
}

internal sealed class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> b)
    {
        b.ToTable("AutomationExecutions");
        b.Property(e => e.Message).HasMaxLength(1000);
        b.HasIndex(e => e.ExecutedAt);
        b.HasOne(e => e.Rule).WithMany().HasForeignKey(e => e.AutomationRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        b.Property(a => a.BatchId).HasMaxLength(50);
        b.Property(a => a.Summary).HasMaxLength(1000);
        b.HasIndex(a => a.Timestamp);
        b.HasIndex(a => a.Operation);
        b.HasIndex(a => a.Status);
        b.HasIndex(a => new { a.EntityType, a.RecordId });
        b.HasIndex(a => a.BatchId);
    }
}

internal sealed class ApplicationSettingConfiguration : IEntityTypeConfiguration<ApplicationSetting>
{
    public void Configure(EntityTypeBuilder<ApplicationSetting> b)
    {
        b.ToTable("ApplicationSettings");
        b.HasKey(s => s.Key);
        b.Property(s => s.Key).HasMaxLength(100);
    }
}

internal sealed class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> b)
    {
        b.ToTable("Backups", t => t.HasCheckConstraint("CK_Backups_Size", "SizeBytes >= 0"));
        b.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(300);
        b.HasIndex(x => x.CreatedAt);
    }
}
