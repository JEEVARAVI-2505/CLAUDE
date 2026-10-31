namespace CRM_Master.Engine.Import;

/// <summary>A spreadsheet / CSV loaded into memory: one header row plus data rows (all cells as text).</summary>
public sealed class ImportTable
{
    public string FileName { get; init; } = "";
    public string FileType { get; init; } = "";
    public string? SheetName { get; init; }
    public List<string> Headers { get; init; } = new();
    public List<string?[]> Rows { get; init; } = new();
    /// <summary>True when only the first rows were read (preview).</summary>
    public bool IsTruncated { get; init; }
    public int ColumnCount => Headers.Count;
}

/// <summary>A user-facing problem with an input file (corrupt, locked, empty, unsupported ...).</summary>
public sealed class ImportException : Exception
{
    public ImportException(string message, Exception? inner = null) : base(message, inner) { }
}
