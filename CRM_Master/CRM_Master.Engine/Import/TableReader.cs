using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

namespace CRM_Master.Engine.Import;

/// <summary>Reads .csv / .txt / .tsv / .xlsx / .xlsm files into an <see cref="ImportTable"/> with friendly errors. Macros are never executed.</summary>
public static class TableReader
{
    public const long MaxFileBytes = 300L * 1024 * 1024;
    public static readonly string[] SupportedExtensions = { ".csv", ".txt", ".tsv", ".xlsx", ".xlsm" };

    public static bool IsSupported(string path) => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    /// <summary>Loads the file. <paramref name="maxRows"/> limits data rows (for previews).</summary>
    public static Task<ImportTable> ReadAsync(string path, int maxRows = int.MaxValue, string? sheet = null, CancellationToken ct = default) =>
        Task.Run(() => Read(path, maxRows, sheet, ct), ct);

    public static ImportTable Read(string path, int maxRows = int.MaxValue, string? sheet = null, CancellationToken ct = default)
    {
        if (!File.Exists(path)) throw new ImportException($"The file '{Path.GetFileName(path)}' was not found. It may have been moved or deleted.");
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".xls") throw new ImportException("Legacy .xls files are not supported. Open the file in Excel and use Save As -> .xlsx (or .csv), then import that file.");
        if (!SupportedExtensions.Contains(ext)) throw new ImportException($"'{ext}' files are not supported. Use .xlsx, .xlsm or .csv.");
        var info = new FileInfo(path);
        if (info.Length == 0) throw new ImportException("The file is empty (0 bytes).");
        if (info.Length > MaxFileBytes) throw new ImportException($"The file is larger than {MaxFileBytes / 1024 / 1024} MB. Split it into smaller files.");

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var table = ext is ".xlsx" or ".xlsm" ? ReadXlsx(stream, path, maxRows, sheet, ct) : ReadCsv(stream, path, maxRows, ct);
            if (table.Headers.Count == 0) throw new ImportException("No data was found in the file. The first row should contain column headings.");
            return table;
        }
        catch (ImportException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException ex) { throw new ImportException("Access to the file was denied. Check the file permissions.", ex); }
        catch (IOException ex) when (!IsCorruption(ex)) { throw new ImportException("The file could not be read. If it is open in Excel, close it and try again.", ex); }
        catch (Exception ex)
        {
            throw new ImportException(ext is ".xlsx" or ".xlsm"
                ? "The file appears to be corrupted or is not a valid Excel workbook. Re-save it from Excel and try again."
                : "The file could not be read as CSV text. It may be corrupted or in an unsupported format.", ex);
        }
    }

    private static bool IsCorruption(IOException ex) => ex is EndOfStreamException || ex.GetType().Name.Contains("Zip", StringComparison.Ordinal);

    /// <summary>Names of the worksheets in an Excel file (empty for CSV).</summary>
    public static IReadOnlyList<string> SheetNames(string path)
    {
        if (Path.GetExtension(path).ToLowerInvariant() is not (".xlsx" or ".xlsm")) return Array.Empty<string>();
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var wb = new XLWorkbook(stream);
            return wb.Worksheets.Select(w => w.Name).ToList();
        }
        catch (Exception ex) { throw new ImportException("The workbook could not be opened. It may be corrupted or password protected.", ex); }
    }

    // ---------- Excel ----------
    private static ImportTable ReadXlsx(Stream stream, string path, int maxRows, string? sheet, CancellationToken ct)
    {
        using var wb = new XLWorkbook(stream);
        var ws = sheet is not null ? wb.Worksheets.FirstOrDefault(w => w.Name == sheet)
                                   : wb.Worksheets.FirstOrDefault(w => w.RangeUsed() is not null);
        if (ws is null) throw new ImportException(sheet is null ? "The workbook has no data." : $"Worksheet '{sheet}' was not found.");
        var used = ws.RangeUsed();
        if (used is null) throw new ImportException("The worksheet is empty.");

        var firstRow = used.FirstRow().RowNumber(); var lastRow = used.LastRow().RowNumber();
        var firstCol = used.FirstColumn().ColumnNumber(); var lastCol = used.LastColumn().ColumnNumber();

        // header = first row that has any content
        var headerRow = firstRow;
        while (headerRow <= lastRow && Enumerable.Range(firstCol, lastCol - firstCol + 1).All(c => string.IsNullOrWhiteSpace(CellText(ws.Cell(headerRow, c))))) headerRow++;
        if (headerRow > lastRow) throw new ImportException("The worksheet is empty.");

        var headers = Enumerable.Range(firstCol, lastCol - firstCol + 1).Select(c => CellText(ws.Cell(headerRow, c))).ToList();
        var table = NewTable(path, ".xlsx", ws.Name, headers, out var width);
        var truncated = false;
        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            if ((r & 0x3FF) == 0) ct.ThrowIfCancellationRequested();
            var cells = new string?[width];
            var any = false;
            for (var i = 0; i < width; i++) { var v = CellText(ws.Cell(r, firstCol + i)); cells[i] = string.IsNullOrWhiteSpace(v) ? null : v; any |= cells[i] is not null; }
            if (!any) continue;
            if (table.Rows.Count >= maxRows) { truncated = true; break; }
            table.Rows.Add(cells);
        }
        return Finish(table, truncated);
    }

    /// <summary>Cell text without Excel display quirks: integers stay integers (phone numbers!), dates become yyyy-MM-dd.</summary>
    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        switch (cell.DataType)
        {
            case XLDataType.Number:
                var d = cell.GetDouble();
                return d == Math.Floor(d) && Math.Abs(d) < 1e15 ? ((long)d).ToString(CultureInfo.InvariantCulture) : d.ToString("R", CultureInfo.InvariantCulture);
            case XLDataType.DateTime: return cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            case XLDataType.Boolean: return cell.GetBoolean() ? "TRUE" : "FALSE";
            case XLDataType.Error: return "";
            default:
                if (cell.HasFormula) { try { return cell.CachedValue.ToString() ?? ""; } catch (Exception) { return ""; } }
                return cell.GetString();
        }
    }

    // ---------- CSV ----------
    private static ImportTable ReadCsv(Stream stream, string path, int maxRows, CancellationToken ct)
    {
        var bytes = new byte[stream.Length]; stream.ReadExactly(bytes);
        if (bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B) throw new ImportException("This looks like an Excel file saved with a .csv extension. Rename it to .xlsx and try again.");
        var text = Decode(bytes);
        var delimiter = DetectDelimiter(text);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false, Delimiter = delimiter, BadDataFound = null, MissingFieldFound = null, TrimOptions = TrimOptions.None,
            DetectDelimiter = false, IgnoreBlankLines = true, Mode = CsvMode.RFC4180
        };
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, config);

        List<string>? headers = null; ImportTable? table = null; var width = 0; var truncated = false; var n = 0;
        while (csv.Read())
        {
            if ((++n & 0x3FF) == 0) ct.ThrowIfCancellationRequested();
            var fields = Enumerable.Range(0, csv.Parser.Count).Select(i => csv.GetField(i) ?? "").ToList();
            if (fields.All(string.IsNullOrWhiteSpace)) continue;
            if (headers is null) { headers = fields; table = NewTable(path, Path.GetExtension(path), null, headers, out width); continue; }
            if (table!.Rows.Count >= maxRows) { truncated = true; break; }
            var cells = new string?[width];
            for (var i = 0; i < width && i < fields.Count; i++) cells[i] = string.IsNullOrWhiteSpace(fields[i]) ? null : fields[i];
            table.Rows.Add(cells);
        }
        if (table is null) throw new ImportException("No data was found in the file.");
        return Finish(table, truncated);
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // Excel "CSV (comma delimited)" on Windows is Windows-1252
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    /// <summary>Picks the delimiter (, ; tab |) that appears most consistently in the first lines outside quotes.</summary>
    internal static string DetectDelimiter(string text)
    {
        var lines = text.Split('\n').Where(l => l.Trim().Length > 0).Take(10).ToList();
        string best = ","; var bestScore = -1.0;
        foreach (var d in new[] { ",", ";", "\t", "|" })
        {
            var counts = lines.Select(l => CountOutsideQuotes(l, d[0])).ToList();
            if (counts.Count == 0 || counts[0] == 0) continue;
            var consistent = counts.Count(c => c == counts[0]);
            var score = counts[0] * (double)consistent / counts.Count;
            if (score > bestScore) { best = d; bestScore = score; }
        }
        return best;
    }

    private static int CountOutsideQuotes(string line, char d)
    {
        var inQ = false; var n = 0;
        foreach (var ch in line) { if (ch == '"') inQ = !inQ; else if (ch == d && !inQ) n++; }
        return n;
    }

    // ---------- shared ----------
    private static ImportTable NewTable(string path, string type, string? sheet, List<string> rawHeaders, out int width)
    {
        // drop trailing empty header cells, name blanks, de-duplicate
        var h = rawHeaders.ToList();
        while (h.Count > 0 && string.IsNullOrWhiteSpace(h[^1])) h.RemoveAt(h.Count - 1);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < h.Count; i++)
        {
            var name = string.IsNullOrWhiteSpace(h[i]) ? $"Column {i + 1}" : Core.BusinessRules.TextCleaner.Clean(h[i])!;
            if (seen.TryGetValue(name, out var count)) { seen[name] = count + 1; name = $"{name} ({count + 1})"; } else seen[name] = 1;
            h[i] = name;
        }
        width = h.Count;
        return new ImportTable { FileName = Path.GetFileName(path), FileType = type.TrimStart('.'), SheetName = sheet, Headers = h };
    }

    private static ImportTable Finish(ImportTable t, bool truncated) =>
        new() { FileName = t.FileName, FileType = t.FileType, SheetName = t.SheetName, Headers = t.Headers, Rows = t.Rows, IsTruncated = truncated };
}
