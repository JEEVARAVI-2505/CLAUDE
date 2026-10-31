using CRM_Master.Engine.Import;
using CRM_Master.Tests.Support;

namespace CRM_Master.Tests.Engine;

public class TableReaderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "crm-master-tests", Guid.NewGuid().ToString("N"));
    public TableReaderTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [Fact]
    public void Reads_csv_with_header_and_rows()
    {
        var p = Files.Csv(_dir, "a.csv", new[] { Files.StandardHeader, new[] { "Ravi", "Kumar", "r@x.com", "9812345670", "Acme", "Pune" }, new[] { "Asha", "Rao", "a@x.com", "9812345671", "Beta", "Delhi" } });
        var t = TableReader.Read(p);
        t.Headers.Should().Equal(Files.StandardHeader);
        t.Rows.Should().HaveCount(2);
        t.Rows[1][2].Should().Be("a@x.com");
        t.FileType.Should().Be("csv");
    }

    [Theory]
    [InlineData(";")]
    [InlineData("\t")]
    [InlineData("|")]
    public void Detects_other_delimiters(string delimiter)
    {
        var p = Files.Csv(_dir, "d.csv", new[] { new[] { "Name", "Email" }, new[] { "A, B", "a@x.com" } }, delimiter);
        var t = TableReader.Read(p);
        t.Headers.Should().Equal("Name", "Email");
        t.Rows.Single()[0].Should().Be("A, B");
    }

    [Fact]
    public void Handles_utf8_bom_quotes_newlines_and_unicode()
    {
        var p = Files.Csv(_dir, "u.csv", new[] { new[] { "Name", "Notes" }, new[] { "José Ñandú", "line1\nline2, \"quoted\"" } }, bom: true);
        var t = TableReader.Read(p);
        t.Headers[0].Should().Be("Name");
        t.Rows[0][0].Should().Be("José Ñandú");
        t.Rows[0][1].Should().Be("line1\nline2, \"quoted\"");
    }

    [Fact]
    public void Falls_back_to_windows_1252_when_not_utf8()
    {
        var p = Path.Combine(_dir, "w.csv");
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        File.WriteAllBytes(p, System.Text.Encoding.GetEncoding(1252).GetBytes("Name,City\nRené,Zürich\n"));
        TableReader.Read(p).Rows[0][0].Should().Be("René");
    }

    [Fact]
    public void Skips_blank_lines_pads_short_rows_and_names_blank_and_duplicate_headers()
    {
        var p = Path.Combine(_dir, "g.csv");
        File.WriteAllText(p, "Name,,Name,Email\n\nA,x\n\n,,,\nB,y,z,b@x.com\n");
        var t = TableReader.Read(p);
        t.Headers.Should().Equal("Name", "Column 2", "Name (2)", "Email");
        t.Rows.Should().HaveCount(2);
        t.Rows[0].Should().Equal("A", "x", null, null);
    }

    [Fact]
    public void Preview_limits_rows_and_flags_truncation()
    {
        var rows = new List<string[]> { new[] { "Name" } }; rows.AddRange(Enumerable.Range(1, 50).Select(i => new[] { "P" + i }));
        var t = TableReader.Read(Files.Csv(_dir, "big.csv", rows), maxRows: 10);
        t.Rows.Should().HaveCount(10); t.IsTruncated.Should().BeTrue();
    }

    [Fact]
    public void Reads_xlsx_keeping_phone_numbers_as_integers_and_dates_as_iso()
    {
        var p = Files.Xlsx(_dir, "a.xlsx", new object?[][] { new object?[] { "Name", "Mobile", "Created" }, new object?[] { "Ravi", 9876543210L, new DateTime(2024, 3, 5) } });
        var t = TableReader.Read(p);
        t.SheetName.Should().Be("Leads");
        t.Rows.Single().Should().Equal("Ravi", "9876543210", "2024-03-05");
    }

    [Fact]
    public void Xlsm_extension_is_accepted_for_ooxml_packages()
    {
        var src = Files.Xlsx(_dir, "m.xlsx", new object?[][] { new object?[] { "Name" }, new object?[] { "A" } });
        var dst = Path.Combine(_dir, "m.xlsm"); File.Copy(src, dst);
        TableReader.Read(dst).Rows.Should().HaveCount(1);
    }

    [Fact]
    public void Excel_sheet_selection_and_names()
    {
        var p = Files.Xlsx(_dir, "s.xlsx", new object?[][] { new object?[] { "Name" }, new object?[] { "A" } }, sheet: "Second");
        TableReader.SheetNames(p).Should().Equal("Second");
        TableReader.Read(p, sheet: "Second").Rows.Should().HaveCount(1);
        ((Action)(() => TableReader.Read(p, sheet: "Nope"))).Should().Throw<ImportException>().WithMessage("*Nope*");
    }

    // ---- error handling ----
    [Fact] public void Missing_file_gives_friendly_error() =>
        ((Action)(() => TableReader.Read(Path.Combine(_dir, "nope.csv")))).Should().Throw<ImportException>().WithMessage("*not found*");

    [Theory]
    [InlineData("empty.csv")]
    [InlineData("empty.xlsx")]
    public void Zero_byte_files_are_rejected(string name)
    {
        var p = Path.Combine(_dir, name); File.WriteAllBytes(p, Array.Empty<byte>());
        ((Action)(() => TableReader.Read(p))).Should().Throw<ImportException>().WithMessage("*empty*");
    }

    [Fact]
    public void Whitespace_only_csv_has_no_data()
    {
        var p = Path.Combine(_dir, "ws.csv"); File.WriteAllText(p, "  \n\n  \n");
        ((Action)(() => TableReader.Read(p))).Should().Throw<ImportException>().WithMessage("*No data*");
    }

    [Fact]
    public void Empty_workbook_is_rejected()
    {
        var p = Path.Combine(_dir, "e.xlsx");
        using (var wb = new ClosedXML.Excel.XLWorkbook()) { wb.AddWorksheet("Blank"); wb.SaveAs(p); }
        ((Action)(() => TableReader.Read(p))).Should().Throw<ImportException>();
    }

    [Fact]
    public void Corrupted_xlsx_gives_friendly_error_not_a_crash()
    {
        var p = Path.Combine(_dir, "bad.xlsx");
        File.WriteAllBytes(p, Enumerable.Range(0, 4000).Select(i => (byte)(i * 31 % 251)).ToArray());
        ((Action)(() => TableReader.Read(p))).Should().Throw<ImportException>().WithMessage("*corrupted*");
    }

    [Fact]
    public void Truncated_xlsx_is_reported_as_corrupted()
    {
        var good = Files.Xlsx(_dir, "t.xlsx", new object?[][] { new object?[] { "Name" }, new object?[] { "A" } });
        var bytes = File.ReadAllBytes(good); var p = Path.Combine(_dir, "trunc.xlsx"); File.WriteAllBytes(p, bytes[..(bytes.Length / 2)]);
        ((Action)(() => TableReader.Read(p))).Should().Throw<ImportException>().WithMessage("*corrupted*");
    }

    [Fact]
    public void Excel_file_renamed_to_csv_is_detected()
    {
        var good = Files.Xlsx(_dir, "r.xlsx", new object?[][] { new object?[] { "Name" }, new object?[] { "A" } });
        var p = Path.Combine(_dir, "renamed.csv"); File.Copy(good, p);
        ((Action)(() => TableReader.Read(p))).Should().Throw<ImportException>().WithMessage("*Excel file*");
    }

    [Theory]
    [InlineData("x.xls", "*xlsx*")]
    [InlineData("x.pdf", "*not supported*")]
    public void Unsupported_types_explain_what_to_do(string name, string message)
    {
        var p = Path.Combine(_dir, name); File.WriteAllText(p, "data");
        ((Action)(() => TableReader.Read(p))).Should().Throw<ImportException>().WithMessage(message);
    }

    [Fact]
    public void File_open_in_another_program_for_reading_can_still_be_read()
    {
        var p = Files.Csv(_dir, "open.csv", new[] { new[] { "Name" }, new[] { "A" } });
        using var other = new FileStream(p, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        TableReader.Read(p).Rows.Should().HaveCount(1);
    }
}
