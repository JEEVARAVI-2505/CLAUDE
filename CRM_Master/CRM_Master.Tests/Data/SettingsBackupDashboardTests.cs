using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using CRM_Master.Tests.Support;
using Microsoft.Data.Sqlite;

namespace CRM_Master.Tests.Data;

public class SettingsBackupDashboardTests
{
    [Fact]
    public async Task Settings_round_trip_through_the_database()
    {
        using var db = await TestDb.CreateAsync();
        var s = db.Settings.Current.Clone();
        s.Theme = "Dark"; s.DefaultCountryCode = "44"; s.ImportDuplicatePolicy = ImportDuplicatePolicy.ProposeUpdate; s.RequiredFields = new() { "Email", "Phone" };
        await db.Settings.SaveAsync(s);

        var fresh = new SettingsService(db.Factory); await fresh.LoadAsync();
        fresh.Current.Theme.Should().Be("Dark");
        fresh.Current.DefaultCountryCode.Should().Be("44");
        fresh.Current.ImportDuplicatePolicy.Should().Be(ImportDuplicatePolicy.ProposeUpdate);
        fresh.Current.RequiredFields.Should().Equal("Email", "Phone");
    }

    [Theory]
    [InlineData("DuplicateLowThreshold", "0.95")]   // low above high
    [InlineData("DefaultCountryCode", "+91")]
    [InlineData("PageSize", "5")]
    public async Task Invalid_settings_are_rejected(string prop, string value)
    {
        using var db = await TestDb.CreateAsync();
        var s = db.Settings.Current.Clone();
        var p = typeof(AppSettings).GetProperty(prop)!;
        p.SetValue(s, p.PropertyType == typeof(double) ? double.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : p.PropertyType == typeof(int) ? int.Parse(value) : value);
        await ((Func<Task>)(() => db.Settings.SaveAsync(s))).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Backup_then_restore_brings_back_deleted_data()
    {
        using var db = await TestDb.CreateAsync();
        for (var i = 0; i < 20; i++) await db.Contacts.AddAsync(TestDb.Person($"Person {i}", $"p{i}@x.com"));
        var backup = await db.Backups.CreateAsync(BackupKind.Manual, "test");
        File.Exists(backup.FilePath).Should().BeTrue();
        backup.ContactCount.Should().Be(20);

        await db.Contacts.DeleteAsync((await db.Contacts.QueryIdsAsync(new())).ToArray());
        (await db.Contacts.CountAsync()).Should().Be(0);

        await db.Backups.RestoreAsync(backup.FilePath);
        (await db.Contacts.CountAsync()).Should().Be(20);
        (await db.Audit.QueryAsync(new(Operation: AuditOperation.Restore))).TotalCount.Should().Be(1);
        (await db.Backups.ListAsync()).Should().Contain(b => b.Kind == BackupKind.PreRestore);   // safety copy of the pre-restore state
    }

    [Fact]
    public async Task Restore_rejects_corrupt_and_foreign_files_without_touching_data()
    {
        using var db = await TestDb.CreateAsync();
        await db.Contacts.AddAsync(TestDb.Person("Keep me"));

        var garbage = db.PathIn("garbage.db"); await File.WriteAllBytesAsync(garbage, Enumerable.Range(0, 5000).Select(i => (byte)(i * 7)).ToArray());
        await ((Func<Task>)(() => db.Backups.RestoreAsync(garbage))).Should().ThrowAsync<InvalidDataException>();

        var foreign = db.PathIn("foreign.db");
        await using (var c = new SqliteConnection($"Data Source={foreign};Pooling=False"))
        { await c.OpenAsync(); var cmd = c.CreateCommand(); cmd.CommandText = "CREATE TABLE other(x int)"; await cmd.ExecuteNonQueryAsync(); }
        await ((Func<Task>)(() => db.Backups.RestoreAsync(foreign))).Should().ThrowAsync<InvalidDataException>();

        await ((Func<Task>)(() => db.Backups.RestoreAsync(db.PathIn("missing.db")))).Should().ThrowAsync<FileNotFoundException>();
        (await db.Contacts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Automatic_backups_are_pruned_to_the_configured_count()
    {
        using var db = await TestDb.CreateAsync();
        var s = db.Settings.Current.Clone(); s.BackupsToKeep = 2; await db.Settings.SaveAsync(s);
        for (var i = 0; i < 4; i++) { await db.Backups.CreateAsync(BackupKind.PreOperation, "t" + i); await Task.Delay(5); }
        (await db.Backups.ListAsync()).Count(b => b.Kind == BackupKind.PreOperation).Should().Be(2);
        Directory.GetFiles(db.Settings.Current.BackupFolder, "*.db").Length.Should().Be(2);
    }

    [Fact]
    public async Task Dashboard_is_empty_on_a_fresh_database()
    {
        using var db = await TestDb.CreateAsync();
        var stats = await new DashboardService(db.Factory).GetStatsAsync();
        stats.IsEmpty.Should().BeTrue();
        stats.Growth.Should().HaveCount(12).And.OnlyContain(p => p.Value == 0);
        stats.RecentImports.Should().BeEmpty();
    }

    [Fact]
    public async Task Dashboard_counts_come_from_stored_data()
    {
        using var db = await TestDb.CreateAsync();
        await db.Contacts.AddAsync(TestDb.Person("Good", "good@acme.com", "9812345670"));
        await db.Contacts.AddAsync(TestDb.Person("Bad Mail", "nope", "9812345671"));
        await db.Contacts.AddAsync(TestDb.Person("Bad Phone", "ok@acme.com", "123"));
        await db.Contacts.AddAsync(new Contact { FullName = "No Email" });
        await db.Audit.WriteAsync(Audit.Entry(AuditOperation.Import, "Import", 1, status: OperationStatus.Failed, error: "boom"));
        await using (var ctx = db.Factory.CreateDbContext())
        { ctx.ImportJobs.Add(new ImportJob { FileName = "a.csv", ImportedRows = 4, Status = ImportJobStatus.Completed }); await ctx.SaveChangesAsync(); }

        var s = await new DashboardService(db.Factory).GetStatsAsync();
        s.TotalContacts.Should().Be(4);
        s.InvalidEmails.Should().Be(1);
        s.InvalidPhones.Should().Be(1);
        s.IncompleteRecords.Should().Be(1);
        s.CleanRecords.Should().Be(1);
        s.NewContactsImported.Should().Be(4);
        s.FailedOperations.Should().Be(1);
        s.RecentErrors.Should().ContainSingle();
        s.Growth.Last().Value.Should().Be(4);
        s.UpdatedRecords.Should().Be(0);
    }

    [Fact]
    public async Task Audit_filters_and_retention()
    {
        using var db = await TestDb.CreateAsync();
        await db.Audit.WriteAsync(Audit.Entry(AuditOperation.Export, "Report", summary: "Exported duplicates.xlsx"));
        await db.Audit.WriteAsync(Audit.Entry(AuditOperation.Import, "Import", summary: "Imported leads.csv", status: OperationStatus.Warning));
        (await db.Audit.QueryAsync(new(Search: "leads"))).TotalCount.Should().Be(1);
        (await db.Audit.QueryAsync(new(Status: OperationStatus.Warning))).TotalCount.Should().Be(1);
        (await db.Audit.QueryAsync(new(Operation: AuditOperation.Export))).TotalCount.Should().Be(1);
        (await db.Audit.QueryAsync(new(FromUtc: DateTime.UtcNow.AddMinutes(1)))).TotalCount.Should().Be(0);
        (await db.Audit.PurgeOlderThanAsync(365)).Should().Be(0);
    }
}
