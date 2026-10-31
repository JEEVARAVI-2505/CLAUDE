using System.Globalization;
using System.Security.Cryptography;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using LeadPilot.Application.Abstractions;

namespace LeadPilot.Infrastructure.Files;

/// <summary>Reads CSV and XLSX files with size/row limits. Formulas are read as their cached values and never evaluated; macros and external links are ignored.</summary>
public sealed class TabularFileReader : IImportReader
{
    public const long MaxBytes = 50L * 1024 * 1024;
    public const int MaxRows = 100_000;
    public const int MaxColumns = 200;

    public ImportTable Read(string path)
    {
        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("The file was not found.", path);
        if (fi.Length > MaxBytes) throw new InvalidDataException($"The file is larger than {MaxBytes / 1024 / 1024} MB.");
        var ext = fi.Extension.ToLowerInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var (headers, rows) = ext switch
        {
            ".csv" or ".txt" => ReadCsv(path),
            ".xlsx" or ".xlsm" => ReadXlsx(path),
            ".xls" => throw new NotSupportedException("Legacy .xls files are not supported. Open the file in Excel and use Save As → Excel Workbook (.xlsx) or CSV, then import that."),
            _ => throw new NotSupportedException($"Files of type '{ext}' cannot be imported. Use .xlsx or .csv.")
        };
        return new ImportTable { Headers = headers, Rows = rows, FileName = fi.Name, FileHash = hash };
    }

    private static (List<string>, List<string[]>) ReadCsv(string path)
    {
        var firstLine = File.ReadLines(path).FirstOrDefault() ?? "";
        var delimiter = new[] { ',', ';', '\t', '|' }.OrderByDescending(d => firstLine.Count(ch => ch == d)).First();
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString(), BadDataFound = null, MissingFieldFound = null, HasHeaderRecord = false, DetectDelimiter = false };
        using var reader = new StreamReader(path, System.Text.Encoding.UTF8, true);
        using var csv = new CsvReader(reader, cfg);
        var headers = new List<string>(); var rows = new List<string[]>();
        while (csv.Read())
        {
            var rec = new string[Math.Min(csv.Parser.Count, MaxColumns)];
            for (var i = 0; i < rec.Length; i++) rec[i] = csv.GetField(i) ?? "";
            if (headers.Count == 0) { headers.AddRange(rec.Select(h => h.Trim().TrimStart('﻿'))); continue; }
            if (rec.All(string.IsNullOrWhiteSpace)) continue;
            if (rows.Count >= MaxRows) throw new InvalidDataException($"The file has more than {MaxRows:N0} rows. Split it and import in parts.");
            rows.Add(rec);
        }
        if (headers.Count == 0) throw new InvalidDataException("The file is empty.");
        return (headers, rows);
    }

    private static (List<string>, List<string[]>) ReadXlsx(string path)
    {
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheets.FirstOrDefault(w => w.Visibility == XLWorksheetVisibility.Visible && !w.IsEmpty()) ?? throw new InvalidDataException("The workbook has no data.");
        var range = ws.RangeUsed() ?? throw new InvalidDataException("The worksheet is empty.");
        var first = range.FirstRow().RowNumber(); var last = range.LastRow().RowNumber();
        var c0 = range.FirstColumn().ColumnNumber(); var c1 = Math.Min(range.LastColumn().ColumnNumber(), c0 + MaxColumns - 1);
        if (last - first > MaxRows) throw new InvalidDataException($"The sheet has more than {MaxRows:N0} rows. Split it and import in parts.");
        var headers = Enumerable.Range(c0, c1 - c0 + 1).Select(c => Cell(ws.Cell(first, c))).ToList();
        var rows = new List<string[]>();
        for (var r = first + 1; r <= last; r++)
        {
            var rec = Enumerable.Range(c0, c1 - c0 + 1).Select(c => Cell(ws.Cell(r, c))).ToArray();
            if (rec.Any(x => !string.IsNullOrWhiteSpace(x))) rows.Add(rec);
        }
        return (headers, rows);
    }

    private static string Cell(IXLCell c)
    {
        try { return c.HasFormula ? c.CachedValue.ToString() ?? "" : c.GetFormattedString().Trim(); }
        catch (Exception e) when (e is InvalidOperationException or FormatException) { return ""; }
    }
}
