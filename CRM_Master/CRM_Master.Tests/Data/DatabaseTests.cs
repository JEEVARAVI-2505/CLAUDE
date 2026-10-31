using CRM_Master.Core.Entities;
using CRM_Master.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Tests.Data;

public class DatabaseTests
{
    [Fact]
    public async Task Migration_creates_all_thirteen_required_tables()
    {
        using var db = await TestDb.CreateAsync();
        await using var ctx = db.Factory.CreateDbContext();
        var conn = (SqliteConnection)ctx.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '\\_\\_%' ESCAPE '\\'";
        var tables = new List<string>();
        await using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) tables.Add(r.GetString(0));
        tables.Should().BeEquivalentTo("Contacts", "Companies", "CustomFields", "ContactCustomFieldValues", "ImportJobs", "ImportRecords",
            "DuplicateGroups", "DuplicateMatches", "AutomationRules", "AutomationExecutions", "AuditLogs", "ApplicationSettings", "Backups");
    }

    [Fact]
    public async Task Foreign_keys_are_enforced()
    {
        using var db = await TestDb.CreateAsync();
        await using var ctx = db.Factory.CreateDbContext();
        ctx.ImportRecords.Add(new ImportRecord { ImportJobId = 999, RowNumber = 1, Status = ImportRecordStatus.Imported });
        var act = () => ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Check_constraint_rejects_unordered_duplicate_pair_and_bad_score()
    {
        using var db = await TestDb.CreateAsync();
        var a = await db.Contacts.AddAsync(TestDb.Person("A", "a@x.com"));
        var b = await db.Contacts.AddAsync(TestDb.Person("B", "b@x.com"));
        await using var ctx = db.Factory.CreateDbContext();
        var g = new DuplicateGroup(); ctx.DuplicateGroups.Add(g); await ctx.SaveChangesAsync();
        ctx.DuplicateMatches.Add(new DuplicateMatch { DuplicateGroupId = g.Id, ContactAId = b.Id, ContactBId = a.Id, Score = 0.9 });
        await ((Func<Task>)(() => ctx.SaveChangesAsync())).Should().ThrowAsync<DbUpdateException>();
        ctx.ChangeTracker.Clear();
        ctx.DuplicateMatches.Add(new DuplicateMatch { DuplicateGroupId = g.Id, ContactAId = a.Id, ContactBId = b.Id, Score = 1.5 });
        await ((Func<Task>)(() => ctx.SaveChangesAsync())).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Transaction_rolls_back_everything_on_failure()
    {
        using var db = await TestDb.CreateAsync();
        await using (var ctx = db.Factory.CreateDbContext())
        {
            await using var tx = await ctx.Database.BeginTransactionAsync();
            ctx.Contacts.Add(new Contact { FullName = "Will vanish" });
            await ctx.SaveChangesAsync();
            ctx.Contacts.Add(new Contact { FullName = "Bad", Status = "  " }); // violates CK_Contacts_Status
            await ((Func<Task>)(() => ctx.SaveChangesAsync())).Should().ThrowAsync<DbUpdateException>();
            await tx.RollbackAsync();
        }
        (await db.Contacts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Update_of_missing_contact_throws_and_writes_nothing()
    {
        using var db = await TestDb.CreateAsync();
        var act = () => db.Contacts.UpdateAsync(new Contact { Id = 42, FullName = "Ghost" });
        await act.Should().ThrowAsync<InvalidOperationException>();
        (await db.Audit.QueryAsync(new(Operation: AuditOperation.ContactUpdated))).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Parameterised_search_is_immune_to_sql_injection()
    {
        using var db = await TestDb.CreateAsync();
        await db.Contacts.AddAsync(TestDb.Person("Alice", "alice@x.com"));
        var res = await db.Contacts.QueryAsync(new(Search: "'; DROP TABLE Contacts; --"));
        res.TotalCount.Should().Be(0);
        (await db.Contacts.CountAsync()).Should().Be(1);
        (await db.Contacts.QueryAsync(new(Search: "100%_"))).TotalCount.Should().Be(0); // wildcards are escaped, not interpreted
        (await db.Contacts.QueryAsync(new(SortBy: "Id; DROP TABLE Contacts"))).TotalCount.Should().Be(1); // sort column is whitelisted
    }
}
