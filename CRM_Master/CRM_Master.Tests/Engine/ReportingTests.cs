using System.Text;
using ClosedXML.Excel;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.BulkUpdate;
using CRM_Master.Engine.DuplicateDetection;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Reporting;
using CRM_Master.Tests.Support;
using CsvHelper;

namespace CRM_Master.Tests.Engine;

public class ReportExporterTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "crm-master-tests", Guid.NewGuid().ToString("N"));
    public ReportExporterTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    private static ReportData Sample(int rows = 3)
    {
        var r = new ReportData { Title = "Sample: [Report]", Subtitle = "Filters - none", Footer = "CRM MASTER" };
        r.Summary.Add(("Total", rows.ToString())); r.Columns.AddRange(new[] { "ID", "Name", "Phone", "Created", "Score" });
        for (var i = 1; i <= rows; i++) r.Rows.Add(new object?[] { i, $"Name {i}", "+91 98123 45670", new DateTime(2024, 1, 2, 3, 4, 0), 0.5 * i });
        return r;
    }

    [Fact]
    public void Excel_file_has_title_summary_header_rows_and_keeps_phone_as_text()
    {
        var p = Path.Combine(_dir, "r.xlsx"); ReportExporter.ToExcel(Sample(), p);
        using var wb = new XLWorkbook(p); var ws = wb.Worksheet(1);
        ws.Name.Should().Be("Sample Report");                       // illegal sheet-name characters removed
        ws.Cell(1, 1).GetString().Should().Be("Sample: [Report]");
        var header = ws.CellsUsed().First(c => c.GetString() == "ID").Address.RowNumber;
        ws.Cell(header, 2).GetString().Should().Be("Name");
        ws.Cell(header + 1, 3).GetString().Should().Be("+91 98123 45670");
        ws.Cell(header + 2, 1).GetDouble().Should().Be(2);
        ws.Cell(header + 3, 5).GetDouble().Should().Be(1.5);
    }

    [Fact]
    public void Csv_round_trips_with_quotes_commas_and_utf8()
    {
        var r = new ReportData { Title = "T" }; r.Columns.AddRange(new[] { "A", "B" });
        r.Rows.Add(new object?[] { "he said \"hi\", ok", "José\nline2" });
        var p = Path.Combine(_dir, "r.csv"); ReportExporter.ToCsv(r, p);
        using var csv = new CsvReader(new StreamReader(p), System.Globalization.CultureInfo.InvariantCulture);
        csv.Read(); csv.ReadHeader(); csv.Read();
        csv.GetField(0).Should().Be("he said \"hi\", ok"); csv.GetField(1).Should().Be("José\nline2");
        File.ReadAllBytes(p)[..3].Should().Equal(0xEF, 0xBB, 0xBF);   // BOM for Excel
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://evil\")", "'=HYPERLINK(\"http://evil\")")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("-cmd|' /C calc'!A0", "'-cmd|' /C calc'!A0")]
    [InlineData("+91 98123 45670", "+91 98123 45670")]       // real phone numbers untouched
    [InlineData("-5", "-5")]
    [InlineData("Normal text", "Normal text")]
    public void Spreadsheet_formula_injection_is_neutralised(string input, string expected) =>
        ReportExporter.SafeCell(input).Should().Be(expected);

    [Fact]
    public void Pdf_is_structurally_valid_and_paginates()
    {
        var p = Path.Combine(_dir, "r.pdf"); ReportExporter.Export(Sample(200), ExportFormat.Pdf, p);
        var bytes = File.ReadAllBytes(p); var text = Encoding.Latin1.GetString(bytes);
        text.Should().StartWith("%PDF-1.4"); text.TrimEnd().Should().EndWith("%%EOF");
        var pages = System.Text.RegularExpressions.Regex.Matches(text, @"/Type /Page /Parent").Count;
        pages.Should().BeGreaterThan(2);
        text.Should().Contain($"/Count {pages}").And.Contain("Page 1 of " + pages).And.Contain("Name 200");
        // xref offsets must point at "N 0 obj"
        var xrefPos = int.Parse(System.Text.RegularExpressions.Regex.Match(text, @"startxref\n(\d+)").Groups[1].Value);
        text[xrefPos..].Should().StartWith("xref");
        var offs = System.Text.RegularExpressions.Regex.Matches(text[xrefPos..], @"(\d{10}) 00000 n").Select(m => int.Parse(m.Groups[1].Value)).ToList();
        for (var i = 0; i < offs.Count; i++) text[offs[i]..].Should().StartWith($"{i + 1} 0 obj");
    }

    [Fact]
    public void Pdf_escapes_special_characters_and_survives_unicode_and_empty_data()
    {
        var r = new ReportData { Title = "Rs ₹ (test) \\ report", Footer = "f" }; r.Columns.Add("Col");
        r.Rows.Add(new object?[] { "a) b ( c \\ d हिन्दी é" });
        var text = Encoding.Latin1.GetString(MiniPdfWriter.Render(r));
        text.Should().Contain("Rs Rs \\(test\\) \\\\ report").And.Contain("a\\) b \\( c \\\\ d ?????? é");
        var empty = Encoding.Latin1.GetString(MiniPdfWriter.Render(new ReportData { Title = "Empty" }));
        empty.Should().Contain("No records match").And.EndWith("%%EOF\n");
    }

    [Fact]
    public void Unwritable_path_is_a_friendly_error_not_a_crash()
    {
        var asDirectory = Path.Combine(_dir, "folder.xlsx"); Directory.CreateDirectory(asDirectory);   // cannot be written as a file on any OS
        foreach (var f in Enum.GetValues<ExportFormat>())
        {
            var act = () => ReportExporter.Export(Sample(), f, asDirectory);
            var ex = Record.Exception(act);
            (ex is IOException || ex is UnauthorizedAccessException).Should().BeTrue($"{f} export to an unwritable path should fail with an IO error, got {ex?.GetType().Name}");   // the service wraps these into a friendly error
        }
    }
}

public class ReportServiceTests : IAsyncLifetime
{
    private TestDb _db = null!; private ReportService _svc = null!; private ImportService _import = null!;
    public async Task InitializeAsync()
    {
        _db = await TestDb.CreateAsync();
        _svc = new ReportService(_db.Factory, _db.Settings);
        _import = new ImportService(_db.Factory, _db.Settings, _db.Backups, new AutomationService(_db.Factory, _db.Settings), _svc);
    }
    public Task DisposeAsync() { _db.Dispose(); return Task.CompletedTask; }

    private async Task<ImportResult> ImportSample()
    {
        var path = Files.Csv(_db.Folder, "leads.csv", new[] { new[] { "Name", "Email", "Phone", "Status" },
            new[] { "Good One", "good@x.com", "9812345670", "Lead" }, new[] { "Bad Mail", "bad", "9812345671", "Customer" }, new[] { "Dup Two", "good@x.com", "", "Lead" }, new[] { "", "", "", "" }, new[] { "No Contact", "", "", "Lead" } });
        return await _import.ImportAsync(path, _import.DefaultOptions(ColumnMapper.Suggest(await TableReader.ReadAsync(path))));
    }

    [Fact]
    public async Task Import_summary_lists_jobs_with_totals_and_honours_filters()
    {
        var r1 = await ImportSample();
        await _import.ImportAsync(Path.Combine(_db.Folder, "missing.csv"), _import.DefaultOptions(new List<ColumnMapping>()));   // a failed one
        var all = await _svc.BuildAsync(ReportType.ImportSummary, new());
        all.RowCount.Should().Be(2);
        all.Summary.Should().Contain(("Contacts imported", "3"));
        (await _svc.BuildAsync(ReportType.ImportSummary, new(ProcessingStatus: OperationStatus.Failed))).RowCount.Should().Be(1);
        (await _svc.BuildAsync(ReportType.ImportSummary, new(ProcessingStatus: OperationStatus.Warning))).Rows.Single()[0].Should().Be(r1.JobId);
        (await _svc.BuildAsync(ReportType.ImportSummary, new(FromUtc: DateTime.UtcNow.AddDays(1)))).RowCount.Should().Be(0);
    }

    [Fact]
    public async Task Single_import_report_is_saved_as_excel_and_lists_every_rejected_row()
    {
        var r = await ImportSample();
        var path = await _svc.GenerateImportSummaryAsync(r.JobId);
        File.Exists(path!).Should().BeTrue();
        using var wb = new XLWorkbook(path!);
        var text = string.Join("|", wb.Worksheet(1).CellsUsed().Select(c => c.GetString()));
        text.Should().Contain("Duplicate").And.Contain("Same email/phone as row 2");
    }

    [Fact]
    public async Task Import_completed_rule_generates_the_report_automatically()
    {
        var auto = new AutomationService(_db.Factory, _db.Settings);
        await auto.SeedDefaultsAsync();
        var rule = (await auto.ListAsync()).Single(r => r.Name == "Report after import");
        await auto.SetEnabledAsync(rule.Id, true);
        var res = await ImportSample();
        res.ReportPath.Should().NotBeNull(); File.Exists(res.ReportPath!).Should().BeTrue();
        (await auto.HistoryAsync(rule.Id)).Single().Message.Should().Contain("report saved");
    }

    [Fact]
    public async Task Duplicate_report_has_pairs_with_both_contacts_and_category_filter()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "Ravi Kumar", Email = "r@x.com", Status = "Lead" });
        await _db.Contacts.AddAsync(new Contact { FullName = "Ravi Kumar", Email = "r@x.com", Status = "Lead" });
        await new DuplicateService(_db.Factory, _db.Settings, _db.Backups, _db.Contacts).ScanAsync(new DuplicateOptions());
        var r = await _svc.BuildAsync(ReportType.Duplicates, new());
        r.RowCount.Should().Be(1);
        r.Rows[0][1].Should().Be("Exact");
        r.Rows[0][6].Should().Be("Ravi Kumar");
        (await _svc.BuildAsync(ReportType.Duplicates, new(ContactCategory: "Customer"))).RowCount.Should().Be(0);
        (await _svc.BuildAsync(ReportType.Duplicates, new(ContactCategory: "Lead"))).RowCount.Should().Be(1);
    }

    [Fact]
    public async Task Data_quality_report_counts_statuses_completeness_and_validation_findings()
    {
        await ImportSample();
        var r = await _svc.BuildAsync(ReportType.DataQuality, new());
        long Count(string section, string metric) => Convert.ToInt64(r.Rows.Single(x => (string)x[0]! == section && (string)x[1]! == metric)[2]);
        Count("Status", "Invalid email").Should().Be(1);
        Count("Status", "Missing email").Should().Be(1);
        Count("Completeness (filled)", "Email").Should().Be(2);
        r.Rows.Should().Contain(x => (string)x[0]! == "Validation findings" && (string)x[1]! == "EMAIL_INVALID");
        r.Summary.Should().Contain(s => s.Label == "Note" && s.Value.Contains("syntax-only"));
        (await _svc.BuildAsync(ReportType.DataQuality, new(ContactCategory: "Customer"))).Summary.First().Value.Should().Be("1");
    }

    [Fact]
    public async Task Invalid_record_report_explains_problems_and_suggests_fixes()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "Typo Person", Email = "typo@gmial.com", Phone = "123" });
        await _db.Contacts.AddAsync(new Contact { FullName = "Fine Person", Email = "fine@x.com", Phone = "9812345670" });
        var r = await _svc.BuildAsync(ReportType.InvalidRecords, new());
        r.RowCount.Should().Be(1);
        r.Rows[0][6]!.ToString().Should().Contain("Phone is not valid");
        r.Rows[0][7].Should().Be("Email: typo@gmail.com");
    }

    [Fact]
    public async Task Bulk_update_report_shows_previous_and_new_values_per_field()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "Ravi", Email = "r@x.com", City = "Pune" });
        var path = Files.Csv(_db.Folder, "u.csv", new[] { new[] { "Email", "City" }, new[] { "r@x.com", "Mumbai" } });
        var bulk = new BulkUpdateService(_db.Factory, _db.Settings, _db.Backups, _db.Contacts);
        var plan = await bulk.PlanFromFileAsync(path, ColumnMapper.Suggest(await TableReader.ReadAsync(path)), BulkUpdatePolicy.AllSelectedFields, new[] { ContactField.City });
        var res = await bulk.ApplyAsync(plan);
        var r = await _svc.BuildAsync(ReportType.BulkUpdate, new(BatchId: res.BatchId));
        r.RowCount.Should().Be(1);
        r.Rows[0].Skip(3).Should().Equal("City", "Pune", "Mumbai");
        r.Summary.Should().Contain(("Contacts updated", "1"));
    }

    [Fact]
    public async Task Automation_and_audit_reports()
    {
        var auto = new AutomationService(_db.Factory, _db.Settings); await auto.SeedDefaultsAsync();
        await ImportSample();
        var a = await _svc.BuildAsync(ReportType.AutomationExecution, new());
        a.RowCount.Should().BeGreaterThan(0);
        a.Rows.Should().Contain(x => (string)x[1]! == "Normalize phone on import");
        var audit = await _svc.BuildAsync(ReportType.Audit, new());
        audit.Rows.Should().Contain(x => (string)x[1]! == "Import");
        (await _svc.BuildAsync(ReportType.Audit, new(ProcessingStatus: OperationStatus.Failed))).RowCount.Should().Be(0);
    }

    [Fact]
    public async Task Exports_are_audited_and_failures_are_logged_with_a_friendly_error()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "A", Email = "a@x.com" });
        var report = await _svc.BuildAsync(ReportType.DataQuality, new());
        var path = Path.Combine(_db.Folder, "out", "q.pdf");
        await _svc.ExportAsync(report, ExportFormat.Pdf, path);
        File.Exists(path).Should().BeTrue();
        (await _db.Audit.QueryAsync(new(Operation: AuditOperation.Export))).Items.Single().Summary.Should().Contain("Pdf");

        var locked = Path.Combine(_db.Folder, "locked.xlsx"); Directory.CreateDirectory(locked);   // an existing folder: not writable as a file on any OS
        await ((Func<Task>)(() => _svc.ExportAsync(report, ExportFormat.Excel, locked))).Should().ThrowAsync<ImportException>().WithMessage("*could not be saved*");
        (await _db.Audit.QueryAsync(new(Operation: AuditOperation.Export, Status: OperationStatus.Failed))).TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Contact_export_includes_every_field_in_all_three_formats()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "Ravi", Email = "r@x.com", Phone = "+91 98123 45670", City = "Pune" });
        var list = await _db.Contacts.GetAllAsync();
        foreach (var f in Enum.GetValues<ExportFormat>())
        {
            var p = Path.Combine(_db.Folder, "contacts" + ReportExporter.Extension(f));
            await _svc.ExportContactsAsync(list, f, p);
            new FileInfo(p).Length.Should().BeGreaterThan(200);
        }
        using var wb = new XLWorkbook(Path.Combine(_db.Folder, "contacts.xlsx"));
        wb.Worksheet(1).CellsUsed().Select(c => c.GetString()).Should().Contain(new[] { "Alternate Phone", "Lead Source", "r@x.com", "+91 98123 45670" });
    }
}
