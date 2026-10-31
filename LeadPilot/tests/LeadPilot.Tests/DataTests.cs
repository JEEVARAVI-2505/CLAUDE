using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Data;
using LeadPilot.Domain;
using LeadPilot.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LeadPilot.Tests;

public class DataTests
{
    [Fact]
    public void Migration_creates_every_required_table_and_is_idempotent()
    {
        using var env = new TestEnv();
        env.Db.Migrate();
        env.Db.SchemaVersion().Should().Be(1);
        using var c = env.Db.Open();
        var tables = SqliteDb.Query(c, "SELECT name FROM sqlite_master WHERE type='table'", r => r.GetString(0));
        tables.Should().Contain(new[] { "Businesses", "Contacts", "Leads", "LeadSources", "LeadFieldsEvidence", "DuplicateMatches", "MergeHistory", "LeadActivities", "FollowUpTasks", "Campaigns", "SalesUsers", "Settings", "ImportHistory", "AuditLogs", "ApiUsage" });
    }

    [Fact]
    public void Lead_ids_are_sequential_and_formatted()
    {
        using var env = new TestEnv();
        var ids = Enumerable.Range(1, 3).Select(i => env.Add("Company " + i + " Works")).ToList();
        env.Leads.GetLead(ids[0])!.LeadCode.Should().Be("LD-000001");
        env.Leads.GetLead(ids[1])!.LeadCode.Should().Be("LD-000002");
        env.Leads.GetLead(ids[2])!.LeadCode.Should().Be("LD-000003");
    }

    [Fact]
    public void Foreign_keys_are_enforced()
    {
        using var env = new TestEnv();
        using var c = env.Db.Open();
        var act = () => SqliteDb.Exec(c, "INSERT INTO LeadSources(LeadId,SourceName,SourceType,RetrievedAt) VALUES(999,'x','y','2026-01-01')");
        act.Should().Throw<SqliteException>().WithMessage("*FOREIGN KEY*");
    }

    [Fact]
    public void Check_constraints_reject_bad_scores()
    {
        using var env = new TestEnv();
        var id = env.Add("Alpha Works");
        using var c = env.Db.Open();
        var act = () => SqliteDb.Exec(c, "UPDATE Leads SET Score=101 WHERE Id=@i", ("@i", id));
        act.Should().Throw<SqliteException>().WithMessage("*CHECK*");
    }

    [Fact]
    public void Insert_is_atomic_when_a_child_row_fails()
    {
        using var env = new TestEnv();
        var ins = new LeadInsert { Business = new Business { Name = "Atomic Co" }, Lead = new Lead { SourceName = "t" }, Contact = new Contact { FullName = "A" } };
        ins.Evidence.Add(new FieldEvidence { FieldName = null!, Value = "v", SourceName = "s", ExtractedAt = env.Clock.UtcNow }); // NOT NULL violation
        var act = () => env.Leads.Insert(ins);
        act.Should().Throw<SqliteException>();
        env.Leads.Count(true).Should().Be(0);
        using var c = env.Db.Open();
        SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM Businesses").Should().Be(0, "the whole transaction must roll back");
    }

    [Fact]
    public void Validation_rejects_blank_company_name()
    {
        using var env = new TestEnv();
        var act = () => env.Leads.Insert(new LeadInsert { Business = new Business { Name = "  " }, Lead = new Lead() });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Sql_injection_text_is_stored_and_searched_literally()
    {
        using var env = new TestEnv();
        var evil = "Robert'); DROP TABLE Leads;--";
        env.Add(evil);
        env.Leads.Search(new LeadQuery { Text = "'); DROP" }).Total.Should().Be(1);
        env.Leads.Search(new LeadQuery { Text = "%" }).Total.Should().Be(0, "% must be escaped, not treated as a wildcard");
        env.Leads.Count().Should().Be(1);
    }

    [Fact]
    public void Search_filters_sort_and_paginate()
    {
        using var env = new TestEnv();
        for (var i = 1; i <= 25; i++)
            env.Add($"Plant {i:D2} Works", c => { c.Industry = "Manufacturing"; c.Phone = i % 2 == 0 ? "+91 98765 4321" + (i % 10) : null; c.Email = i % 5 == 0 ? $"p{i}@plant{i}.com" : null; if (i > 20) c.City = "Madurai"; });
        var p1 = env.Leads.Search(new LeadQuery { PageSize = 10, Sort = LeadSort.Company, Descending = false });
        p1.Total.Should().Be(25); p1.PageCount.Should().Be(3); p1.Rows.First().CompanyName.Should().Be("Plant 01 Works");
        env.Leads.Search(new LeadQuery { PageSize = 10, Page = 3, Sort = LeadSort.Company, Descending = false }).Rows.Should().HaveCount(5);
        env.Leads.Search(new LeadQuery { City = "Madurai" }).Total.Should().Be(5);
        env.Leads.Search(new LeadQuery { Text = "plant 07" }).Rows.Should().ContainSingle();
        env.Leads.Search(new LeadQuery { OnlyMissingContact = true }).Total.Should().Be(env.Leads.Search(new LeadQuery { PageSize = 100 }).Rows.Count(r => r.Phone == null && r.Email == null));
        env.Leads.GetFilterOptions().Cities.Should().BeEquivalentTo("Chennai", "Madurai");
    }

    [Fact]
    public void Bulk_status_assign_and_delete()
    {
        using var env = new TestEnv();
        var ids = new[] { env.Add("A One Works"), env.Add("B Two Works"), env.Add("C Three Works") };
        env.Leads.BulkSetStatus(ids.Take(2).ToList(), LeadStatus.Qualified);
        env.Leads.BulkAssign(ids, env.Admin.Id);
        var rows = env.Leads.Search(new LeadQuery { Sort = LeadSort.LeadId, Descending = false }).Rows;
        rows.Select(r => r.Status).Should().Equal(LeadStatus.Qualified, LeadStatus.Qualified, LeadStatus.New);
        rows.Should().OnlyContain(r => r.AssignedTo == "Asha Admin");
        env.Leads.GetDetail(ids[0])!.Activities.Should().Contain(a => a.Type == ActivityType.StatusChange);
        env.Leads.Delete(new[] { ids[2] });
        env.Leads.Count().Should().Be(2);
        using var c = env.Db.Open();
        SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM Businesses").Should().Be(2, "orphaned businesses are removed");
    }

    [Fact]
    public void Backup_is_a_consistent_openable_copy()
    {
        using var env = new TestEnv();
        env.Add("Backup Works");
        var dest = Path.Combine(env.Folder, "copy.db");
        env.Db.BackupTo(dest);
        var copy = new SqliteDb(dest);
        using var c = copy.Open();
        SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM Leads").Should().Be(1);
        SqliteDb.ReleaseFiles();
    }

    [Fact]
    public void Dashboard_numbers_match_the_data()
    {
        using var env = new TestEnv();
        var a = env.Add("Hot Plant Works", c => { c.Industry = "Manufacturing"; c.Phone = "+919876543210"; c.Email = "a@hot.com"; c.Website = "https://hot.com"; c.ContactName = "Ravi"; c.Description = "solar panel installers"; c.IntentEvidence = "enquiry form"; c.Address = "1 Main Rd"; });
        var b = env.Add("Plain Works");
        var d = env.Add("Won Works", c => { c.Phone = "+919876500001"; c.Email = "w@won.com"; c.Website = "https://won.com"; });
        env.Leads.BulkSetStatus(new[] { d }, LeadStatus.Won);
        env.FollowUps.Schedule(a, "Call", env.Clock.UtcNow.AddDays(-1));
        env.FollowUps.Schedule(b, "Email", env.Clock.UtcNow.AddDays(2));
        env.Sys.AddApiUsage(new ApiUsageRecord { Provider = "osm", Operation = "search", Requests = 1, Success = true, ResultCount = 40 });
        env.Sys.AddApiUsage(new ApiUsageRecord { Provider = "osm", Operation = "search", Requests = 1, Success = false, ResultCount = 0, ErrorMessage = "x" });

        var s = env.Stats.GetDashboard(env.Clock.UtcNow);
        s.TotalLeads.Should().Be(3);
        s.NewLeads.Should().Be(2);
        s.ConvertedLeads.Should().Be(1);
        s.QualifiedLeads.Should().Be(1);
        s.PendingFollowUps.Should().Be(2);
        s.OverdueFollowUps.Should().Be(1);
        s.IncompleteRecords.Should().Be(1, "only 'Plain Works' lacks phone/e-mail/website");
        s.TotalDiscovered.Should().Be(40, "failed calls do not count");
        s.HighPriorityLeads.Should().Be(1);
        s.ByLocation.Single().Should().Be(new KeyCount("Chennai", 3));
        s.Pipeline.Single(p => p.Key == "Won").Count.Should().Be(1);
        s.MonthlyDiscovery.Should().HaveCount(12);
        s.MonthlyDiscovery.Last().Should().Be(new KeyCount("Mar 26", 3));
    }

    [Fact]
    public void Followup_updates_next_followup_and_lead_row()
    {
        using var env = new TestEnv();
        var id = env.Add("Task Works");
        var t1 = env.FollowUps.Schedule(id, "Call", env.Clock.UtcNow.AddDays(3));
        env.FollowUps.Schedule(id, "Email", env.Clock.UtcNow.AddDays(1));
        env.Leads.GetLead(id)!.NextFollowUpAt.Should().Be(env.Clock.UtcNow.AddDays(1));
        env.FollowUps.Complete(t1, "left voicemail");
        env.FollowUps.Open().Should().HaveCount(1);
        env.FollowUps.Overdue().Should().BeEmpty();
        env.Clock.UtcNow = env.Clock.UtcNow.AddDays(2);
        env.FollowUps.Overdue().Should().HaveCount(1);
        env.Leads.Search(new LeadQuery { OnlyNeedsFollowUp = true }).Total.Should().Be(1);
    }

    [Fact]
    public void Logging_a_call_marks_lead_contacted_and_sets_last_contacted()
    {
        using var env = new TestEnv();
        var id = env.Add("Call Works");
        env.FollowUps.LogInteraction(id, ActivityType.Call, "Intro call", "Spoke to reception");
        var lead = env.Leads.GetLead(id)!;
        lead.Status.Should().Be(LeadStatus.Contacted);
        lead.LastContactedAt.Should().NotBeNull();
    }

    [Fact]
    public void Retention_removes_only_old_closed_leads()
    {
        using var env = new TestEnv();
        var old = env.Add("Old Lost Works"); var fresh = env.Add("Active Works");
        env.Leads.BulkSetStatus(new[] { old }, LeadStatus.Lost);
        var s = env.Settings.Current; s.RetentionDays = 30; env.Settings.Save(s);
        var gov = new DataGovernanceService(env.Leads, env.Sys, env.Settings, env.Users, env.Clock);
        gov.ApplyRetention().Should().Be(0, "not old enough yet");
        env.Clock.UtcNow = env.Clock.UtcNow.AddDays(40);
        gov.ApplyRetention().Should().Be(1);
        env.Leads.GetLead(old).Should().BeNull(); env.Leads.GetLead(fresh).Should().NotBeNull();
    }

    [Fact]
    public void Delete_all_data_requires_admin_and_is_audited()
    {
        using var env = new TestEnv();
        env.Add("Gone Works");
        var gov = new DataGovernanceService(env.Leads, env.Sys, env.Settings, env.Users, env.Clock);
        var sales = env.Users.Create("Sam Sales", null, UserRole.Sales, "1234");
        env.Users.SignIn(sales, "1234").Should().BeTrue();
        ((Action)gov.DeleteAllData).Should().Throw<UnauthorizedAccessException>();
        env.Users.SignIn(env.Admin, null);
        gov.DeleteAllData();
        env.Leads.Count(true).Should().Be(0);
        env.Sys.GetAudit(10).Should().Contain(a => a.Action == "data.delete_all");
    }

    [Fact]
    public void Pin_must_match_and_failures_are_audited()
    {
        using var env = new TestEnv();
        var u = env.Users.Create("Pin User", null, UserRole.Manager, "4321");
        u.PinHash.Should().NotContain("4321");
        env.Users.SignIn(u, "0000").Should().BeFalse();
        env.Users.SignIn(u, null).Should().BeFalse();
        env.Users.SignIn(u, "4321").Should().BeTrue();
        env.Sys.GetAudit(20).Count(a => a.Action == "signin.failed").Should().Be(2);
        env.Users.Can(Application.Abstractions.Permission.Export).Should().BeTrue();
        env.Users.Can(Application.Abstractions.Permission.ManageSettings).Should().BeFalse();
    }
}
