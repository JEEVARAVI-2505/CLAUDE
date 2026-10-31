using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Services;
using LeadPilot.Data;
using LeadPilot.Domain;

namespace LeadPilot.Tests.Support;

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
}

public sealed class FakeLicense : ILicenseService
{
    public LicenseInfo Current { get; set; } = new("Test", "Tester", int.MaxValue, 100, null, true, "ok", false);
    public string MachineId => "TEST-MACHINE";
    public LicenseInfo Activate(string licenseKey) => Current;
}

/// <summary>A real SQLite database in a temp folder plus fully wired services. Disposed after each test.</summary>
public sealed class TestEnv : IDisposable
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "leadpilot-tests-" + Guid.NewGuid().ToString("N"));
    public FakeClock Clock { get; } = new();
    public FakeLicense License { get; } = new();
    public SqliteDb Db { get; }
    public LeadRepository Leads { get; }
    public WorkRepository Work { get; }
    public DuplicateRepository Dups { get; }
    public SystemRepository Sys { get; }
    public StatsRepository Stats { get; }
    public SettingsService Settings { get; }
    public UserService Users { get; }
    public LeadIngestService Ingest { get; }
    public ScoringService Scoring { get; }
    public DuplicateService Duplicates { get; }
    public FollowUpService FollowUps { get; }
    public SalesUser Admin { get; }

    public TestEnv()
    {
        Directory.CreateDirectory(Folder);
        Db = new SqliteDb(Path.Combine(Folder, "test.db"));
        Db.Migrate();
        Leads = new LeadRepository(Db, Clock); Work = new WorkRepository(Db, Clock); Dups = new DuplicateRepository(Db, Clock);
        Sys = new SystemRepository(Db, Clock); Stats = new StatsRepository(Db);
        Settings = new SettingsService(Sys);
        Users = new UserService(Sys, Clock);
        Admin = Users.Create("Asha Admin", "asha@example.com", UserRole.Admin, null);
        Users.SignIn(Admin, null);
        Scoring = new ScoringService(Leads, Work, Sys, Settings);
        Ingest = new LeadIngestService(Leads, Dups, Settings, License, Clock);
        Duplicates = new DuplicateService(Leads, Dups, Sys, Users, Clock);
        FollowUps = new FollowUpService(Leads, Work, Sys, Users, Clock);
    }

    public static ScoringProfile ChennaiManufacturing => new() { Product = "Commercial Solar Panels", TargetCustomer = "Manufacturing Companies", Location = "Chennai" };

    public int Add(string name, Action<ProspectCandidate>? tweak = null, IngestContext? ctx = null, bool allowDuplicate = true)
    {
        var c = new ProspectCandidate { Name = name, City = "Chennai", Country = "India", SourceName = "Manual", SourceType = "Manual", RetrievedAt = Clock.UtcNow };
        tweak?.Invoke(c);
        var res = Ingest.Ingest(c, ctx ?? new IngestContext { Profile = ChennaiManufacturing, SkipHighConfidenceDuplicates = !allowDuplicate });
        return res.LeadId ?? throw new InvalidOperationException(res.Message);
    }

    public void Dispose()
    {
        SqliteDb.ReleaseFiles();
        try { Directory.Delete(Folder, true); } catch (IOException) { /* temp cleanup best effort */ }
    }
}
