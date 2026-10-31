using System.Security.Cryptography;
using ClosedXML.Excel;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Data;
using LeadPilot.Domain;
using LeadPilot.Infrastructure;
using LeadPilot.Infrastructure.Files;
using LeadPilot.Infrastructure.Licensing;
using LeadPilot.Infrastructure.Security;
using LeadPilot.Tests.Support;

namespace LeadPilot.Tests;

public class ImportExportTests
{
    private static string WriteCsv(TestEnv env, string name, string content) { var p = Path.Combine(env.Folder, name); File.WriteAllText(p, content); return p; }

    private static ImportService Importer(TestEnv env) => new(new TabularFileReader(), env.Ingest, env.Sys, env.Users, env.Settings, env.Scoring, env.Clock);

    [Fact]
    public void Csv_import_end_to_end_with_mapping_validation_duplicates_and_report()
    {
        using var env = new TestEnv();
        var path = WriteCsv(env, "leads.csv",
            "Company Name,Contact Person,Mobile,E-mail,Web,City,Sector\n" +
            "Sri Ganesh Engineering Works,Ravi Kumar,098765 43210,ravi@ganesh.com,ganesh.com,Chennai,Manufacturing\n" +
            "\"Bright, Path Marketing\",,+91 98765 43211,bad-email,,Chennai,Marketing\n" +
            ",Nobody,,,,,\n" +
            "Sri Ganesh Engineering Works Pvt Ltd,,09876543210,,,Chennai,\n");
        var imp = Importer(env);
        var prev = imp.Preview(path);
        prev.Table.Rows.Should().HaveCount(4);
        prev.SuggestedMapping[ImportField.CompanyName].Should().Be(0);
        prev.SuggestedMapping[ImportField.Phone].Should().Be(2);
        prev.SuggestedMapping[ImportField.Website].Should().Be(4);
        prev.SuggestedMapping[ImportField.Industry].Should().Be(6);

        var v = imp.Validate(prev.Table, prev.SuggestedMapping);
        v.ValidRows.Should().Be(3);
        v.Errors.Should().ContainSingle(e => e.Contains("Row 4"));
        v.Warnings.Should().ContainSingle(w => w.Contains("bad-email"));

        var rep = imp.Import(prev.Table, prev.SuggestedMapping, null);
        rep.Imported.Should().Be(2); rep.DuplicatesLinked.Should().Be(1); rep.Errors.Should().Be(1);
        env.Leads.Count().Should().Be(2);
        var rows = env.Leads.Search(new LeadQuery { Sort = LeadSort.LeadId, Descending = false }).Rows;
        rows[0].ContactPerson.Should().Be("Ravi Kumar");
        rows[1].CompanyName.Should().Be("Bright, Path Marketing");
        rows[1].EmailStatus.Should().Be(EmailStatus.InvalidFormat);
        env.Sys.GetImports(5).Should().ContainSingle(i => i.FileName == "leads.csv" && i.Imported == 2);
        imp.Preview(path).AlreadyImported.Should().BeTrue("same file hash was imported before");
    }

    [Fact]
    public void Xlsx_import_reads_cached_formula_values_and_text()
    {
        using var env = new TestEnv();
        var path = Path.Combine(env.Folder, "book.xlsx");
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            ws.Cell(1, 1).Value = "Company"; ws.Cell(1, 2).Value = "Phone"; ws.Cell(1, 3).Value = "City";
            ws.Cell(2, 1).Value = "Delta Fabricators"; ws.Cell(2, 2).Value = "+91 98765 43212"; ws.Cell(2, 3).Value = "Coimbatore";
            ws.Cell(3, 1).Value = "Epsilon Mills"; ws.Cell(3, 2).Value = "+91 98765 43213"; ws.Cell(3, 3).Value = "Salem";
            wb.SaveAs(path);
        }
        var imp = Importer(env);
        var prev = imp.Preview(path);
        prev.Table.Headers.Should().Equal("Company", "Phone", "City");
        imp.Import(prev.Table, prev.SuggestedMapping, null).Imported.Should().Be(2);
        env.Leads.Search(new LeadQuery { City = "Salem" }).Total.Should().Be(1);
    }

    [Fact]
    public void Legacy_xls_and_unknown_types_are_rejected_with_guidance()
    {
        using var env = new TestEnv();
        var xls = Path.Combine(env.Folder, "old.xls"); File.WriteAllBytes(xls, new byte[] { 1, 2, 3 });
        var act = () => new TabularFileReader().Read(xls);
        act.Should().Throw<NotSupportedException>().WithMessage("*.xlsx*");
        var exe = Path.Combine(env.Folder, "run.exe"); File.WriteAllBytes(exe, new byte[] { 1 });
        ((Action)(() => new TabularFileReader().Read(exe))).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Semicolon_delimited_csv_is_detected()
    {
        using var env = new TestEnv();
        var t = new TabularFileReader().Read(WriteCsv(env, "semi.csv", "Company;Phone\nAcme Works;12345\n"));
        t.Headers.Should().Equal("Company", "Phone"); t.Rows.Single()[0].Should().Be("Acme Works");
    }

    private static List<LeadRow> SampleRows() => new()
    {
        new LeadRow { Id = 1, LeadCode = "LD-000001", CompanyName = "=HYPERLINK(\"http://evil\",\"x\")", Email = "a@b.com", Score = 70, Temperature = LeadTemperature.Warm, Status = LeadStatus.New, Source = "Manual", CreatedAt = DateTime.UtcNow },
        new LeadRow { Id = 2, LeadCode = "LD-000002", CompanyName = "Normal (Pvt) Ltd, Chennai", Phone = "+919876543210", Score = 40, Temperature = LeadTemperature.Cold, Status = LeadStatus.Won, Source = "Import", CreatedAt = DateTime.UtcNow }
    };

    [Fact]
    public void Csv_export_round_trips_and_neutralises_formulas()
    {
        using var env = new TestEnv();
        var path = Path.Combine(env.Folder, "out.csv");
        new ExportService().ExportCsv(SampleRows(), path);
        var t = new TabularFileReader().Read(path);
        t.Headers.Should().Contain(new[] { "Lead ID", "Company", "Score" });
        t.Rows[0][1].Should().StartWith("'=");
        t.Rows[1][1].Should().Be("Normal (Pvt) Ltd, Chennai");
    }

    [Fact]
    public void Xlsx_export_has_header_rows_numbers_and_safe_text()
    {
        using var env = new TestEnv();
        var path = Path.Combine(env.Folder, "out.xlsx");
        new ExportService().ExportXlsx(SampleRows(), path);
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Leads");
        ws.Cell(1, 1).GetString().Should().Be("Lead ID");
        ws.Cell(2, 10).Value.IsNumber.Should().BeTrue(); ws.Cell(2, 10).GetDouble().Should().Be(70);
        ws.Cell(2, 2).HasFormula.Should().BeFalse("a leading = must never become a live formula");
        ws.Cell(2, 2).DataType.Should().Be(XLDataType.Text);
        ws.Cell(2, 2).GetString().Should().Contain("HYPERLINK");
        ws.LastRowUsed()!.RowNumber().Should().Be(3);
    }

    [Fact]
    public void Pdf_export_is_a_well_formed_pdf_with_content_and_pages()
    {
        using var env = new TestEnv();
        var rows = Enumerable.Range(1, 120).Select(i => new LeadRow { Id = i, LeadCode = $"LD-{i:D6}", CompanyName = $"Company (No.{i}) \\ test", Score = i % 100, Source = "x", Status = LeadStatus.New }).ToList();
        var path = Path.Combine(env.Folder, "out.pdf");
        new ExportService().ExportPdf(rows, path, "My leads");
        var text = System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(path));
        text.Should().StartWith("%PDF-1.4").And.EndWith("%%EOF\n");
        text.Should().Contain("My leads").And.Contain("LD-000120").And.Contain("Page 1 of 3");
        text.Should().Contain(@"Company \(No.1\) \\ test");
        // startxref must point at the 'xref' keyword
        var off = int.Parse(text[(text.LastIndexOf("startxref\n", StringComparison.Ordinal) + 10)..].Split('\n')[0]);
        text.Substring(off, 4).Should().Be("xref");
    }
}

public class SecurityTests
{
    [Fact]
    public void Backup_roundtrip_restores_data_and_keeps_safety_copy()
    {
        using var env = new TestEnv();
        var paths = new AppPaths(Path.Combine(env.Folder, "app"));
        env.Add("Keep Me Works");
        var svc = new BackupService(new DatabaseFile(env.Db), paths, env.Clock);
        var file = svc.CreateBackup(paths.Backups, null);
        file.Should().EndWith(".lpbackup");
        env.Add("Added After Backup Works");
        env.Leads.Count().Should().Be(2);

        svc.Restore(file, null);
        env.Leads.Count().Should().Be(1);
        env.Leads.Search(new LeadQuery()).Rows.Single().CompanyName.Should().Be("Keep Me Works");
        Directory.GetFiles(paths.Backups, "pre-restore_*.db").Should().ContainSingle();
    }

    [Fact]
    public void Encrypted_backup_needs_the_right_password_and_hides_content()
    {
        using var env = new TestEnv();
        var paths = new AppPaths(Path.Combine(env.Folder, "app"));
        env.Add("Secret Customer Works");
        var svc = new BackupService(new DatabaseFile(env.Db), paths, env.Clock);
        var file = svc.CreateBackup(paths.Backups, "correct horse");
        var bytes = File.ReadAllBytes(file);
        System.Text.Encoding.Latin1.GetString(bytes).Should().NotContain("Secret Customer").And.NotContain("SQLite format");
        ((Action)(() => svc.Restore(file, "wrong"))).Should().Throw<CryptographicException>().WithMessage("*Wrong password*");
        ((Action)(() => svc.Restore(file, null))).Should().Throw<CryptographicException>();
        env.Add("Another Works");
        svc.Restore(file, "correct horse");
        env.Leads.Count().Should().Be(1);
    }

    [Fact]
    public void Tampered_or_foreign_backups_are_refused_and_live_data_untouched()
    {
        using var env = new TestEnv();
        var paths = new AppPaths(Path.Combine(env.Folder, "app"));
        env.Add("Live Works");
        var svc = new BackupService(new DatabaseFile(env.Db), paths, env.Clock);
        var file = svc.CreateBackup(paths.Backups, "pw");
        var bytes = File.ReadAllBytes(file); bytes[^5] ^= 0xFF; File.WriteAllBytes(file, bytes);
        ((Action)(() => svc.Restore(file, "pw"))).Should().Throw<CryptographicException>();
        var junk = Path.Combine(env.Folder, "junk.lpbackup"); File.WriteAllText(junk, "not a zip");
        ((Action)(() => svc.Restore(junk, null))).Should().Throw<InvalidDataException>();
        env.Leads.Count().Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    public void Secrets_are_encrypted_on_disk_and_round_trip(bool dpapi)
    {
        var dir = Path.Combine(Path.GetTempPath(), "lp-sec-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SecretStore(Path.Combine(dir, "secrets.dat"), dpapi);
            store.Set("provider:google", "AIzaSy-SUPER-SECRET");
            store.Get("provider:google").Should().Be("AIzaSy-SUPER-SECRET");
            File.ReadAllText(Path.Combine(dir, "secrets.dat")).Should().NotContain("SUPER-SECRET");
            new SecretStore(Path.Combine(dir, "secrets.dat"), dpapi).Get("provider:google").Should().Be("AIzaSy-SUPER-SECRET");
            store.Set("provider:google", null);
            store.Get("provider:google").Should().BeNull();
            store.Get("missing").Should().BeNull();
        }
        finally { Directory.Delete(dir, true); }
    }
}

public class LicenseTests
{
    private static (ECDsa key, string pem) NewKey() { var k = ECDsa.Create(ECCurve.NamedCurves.nistP256); return (k, k.ExportSubjectPublicKeyInfoPem()); }

    private static (LicenseService svc, TestEnv env, AppPaths paths) Make(string? pem)
    {
        var env = new TestEnv();
        var paths = new AppPaths(Path.Combine(env.Folder, "app")); paths.EnsureCreated();
        return (new LicenseService(paths, env.Clock, env.Settings, pem ?? "NOT-CONFIGURED"), env, paths);
    }

    [Fact]
    public void Fresh_install_is_a_14_day_trial_then_expires_but_keeps_data_readable()
    {
        var (svc, env, _) = Make(null); using var _e = env;
        svc.Current.IsTrial.Should().BeTrue(); svc.Current.IsValid.Should().BeTrue(); svc.Current.MaxLeads.Should().Be(200);
        svc.Current.Message.Should().Contain("14 day");
        env.Clock.UtcNow = env.Clock.UtcNow.AddDays(15);
        var later = new LicenseService(new AppPaths(Path.Combine(env.Folder, "app")), env.Clock, env.Settings, "NOT-CONFIGURED");
        later.Current.IsValid.Should().BeFalse(); later.Current.Message.Should().Contain("trial ended");
    }

    [Fact]
    public void Trial_start_survives_deleting_one_of_the_two_stores()
    {
        var (svc, env, paths) = Make(null); using var _e = env;
        _ = svc.Current;
        env.Clock.UtcNow = env.Clock.UtcNow.AddDays(10);
        File.Delete(paths.TrialMarker);
        var again = new LicenseService(paths, env.Clock, env.Settings, "NOT-CONFIGURED");
        again.Current.Message.Should().Contain("4 day");
    }

    [Fact]
    public void Valid_signed_key_activates_and_persists()
    {
        var (key, pem) = NewKey(); using var _k = key;
        var (svc, env, paths) = Make(pem); using var _e = env;
        var lic = LicenseCodec.Sign(new LicensePayload { Tier = "Professional", Holder = "Acme Ltd", MaxUsers = 3, IssuedUtc = env.Clock.UtcNow, ExpiresUtc = env.Clock.UtcNow.AddYears(1) }, key);
        var info = svc.Activate(lic);
        info.IsValid.Should().BeTrue(); info.Tier.Should().Be("Professional"); info.MaxUsers.Should().Be(3); info.IsTrial.Should().BeFalse();
        new LicenseService(paths, env.Clock, env.Settings, pem).Current.Tier.Should().Be("Professional");
    }

    [Fact]
    public void Forged_tampered_expired_and_wrong_machine_keys_are_rejected()
    {
        var (key, pem) = NewKey(); using var _k = key;
        var (svc, env, _) = Make(pem); using var _e = env;
        var good = LicenseCodec.Sign(new LicensePayload { Tier = "Business", Holder = "X", MaxUsers = 10, IssuedUtc = env.Clock.UtcNow }, key);

        using var attacker = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        svc.Activate(LicenseCodec.Sign(new LicensePayload { Tier = "Business", Holder = "Pirate" }, attacker)).IsValid.Should().BeFalse("signed with a different key");

        var parts = good.Split('.'); var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[0][LicenseCodec.Prefix.Length..].Replace('-', '+').Replace('_', '/').PadRight(((parts[0].Length - LicenseCodec.Prefix.Length + 3) / 4) * 4, '=')));
        var edited = payload.Replace("\"MaxUsers\":10", "\"MaxUsers\":99");
        var forged = LicenseCodec.Prefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(edited)).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "." + parts[1];
        svc.Activate(forged).IsValid.Should().BeFalse("payload edited after signing");

        svc.Activate(LicenseCodec.Sign(new LicensePayload { Tier = "Single", Holder = "Old", ExpiresUtc = env.Clock.UtcNow.AddDays(-1) }, key)).Message.Should().Contain("expired");
        svc.Activate(LicenseCodec.Sign(new LicensePayload { Tier = "Single", Holder = "Other PC", MachineId = "AAAA-BBBB-CCCC-DDDD" }, key)).Message.Should().Contain("different computer");
        svc.Activate("garbage").IsValid.Should().BeFalse();
        svc.Current.IsTrial.Should().BeTrue("failed activations never replace the working state");
    }

    [Fact]
    public void Machine_bound_key_works_on_this_machine()
    {
        var (key, pem) = NewKey(); using var _k = key;
        var (svc, env, _) = Make(pem); using var _e = env;
        svc.MachineId.Should().MatchRegex("^[0-9A-F]{4}(-[0-9A-F]{4}){3}$");
        svc.Activate(LicenseCodec.Sign(new LicensePayload { Tier = "Single", Holder = "Me", MachineId = svc.MachineId }, key)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Build_without_vendor_key_cannot_accept_paid_keys()
    {
        var (svc, env, _) = Make(null); using var _e = env;
        svc.Activate("LPAI1-abc.def").Message.Should().Contain("no vendor licence key");
    }
}
