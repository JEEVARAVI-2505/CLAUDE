using System.Globalization;
using System.Text;

namespace LeadPilot.Infrastructure.Files;

/// <summary>Minimal dependency-free PDF writer (A4 landscape, built-in Helvetica) for tables and text reports.</summary>
public sealed class SimplePdfWriter
{
    private const double W = 842, H = 595, Margin = 36;
    private readonly List<StringBuilder> _pages = new();
    private StringBuilder _cur = new();
    private double _y;
    private readonly string _footer;

    public SimplePdfWriter(string footer) { _footer = footer; NewPage(); }

    private void NewPage() { _cur = new StringBuilder(); _pages.Add(_cur); _y = H - Margin; }

    /// <summary>Appends a text line, wrapping nothing; call with already-short text.</summary>
    public void Line(string text, double size = 10, bool bold = false, double gap = 4)
    {
        if (_y - size < Margin + 20) NewPage();
        _y -= size;
        Text(Margin, _y, text, size, bold);
        _y -= gap;
    }

    /// <summary>Appends a table row; each cell is truncated to its column width.</summary>
    public void Row(IReadOnlyList<string> cells, IReadOnlyList<double> widths, double size = 8, bool bold = false)
    {
        if (_y - size < Margin + 20) NewPage();
        _y -= size;
        var x = Margin;
        for (var i = 0; i < cells.Count; i++)
        {
            Text(x, _y, Fit(cells[i], widths[i], size), size, bold);
            x += widths[i];
        }
        _y -= 4;
    }

    public void Space(double h) => _y -= h;

    private static string Fit(string s, double width, double size)
    {
        var max = Math.Max(1, (int)(width / (size * 0.5)) - 1);
        s = (s ?? "").Replace('\n', ' ').Replace('\r', ' ');
        return s.Length <= max ? s : s[..Math.Max(1, max - 1)] + "~";
    }

    private void Text(double x, double y, string text, double size, bool bold) =>
        _cur.Append(CultureInfo.InvariantCulture, $"BT /{(bold ? "F2" : "F1")} {size:0.##} Tf {x:0.##} {y:0.##} Td ({Escape(text)}) Tj ET\n");

    private static string Escape(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (ch is '(' or ')' or '\\') sb.Append('\\').Append(ch);
            else if (ch < 32) sb.Append(' ');
            else if (ch > 255) sb.Append('?');
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>Serialises the document.</summary>
    public byte[] ToBytes()
    {
        var n = _pages.Count;
        for (var i = 0; i < n; i++)
        {
            _pages[i].Append(CultureInfo.InvariantCulture, $"BT /F1 7 Tf {Margin} 20 Td ({Escape(_footer)}  -  Page {i + 1} of {n}) Tj ET\n");
        }
        var objs = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [" + string.Join(' ', Enumerable.Range(0, n).Select(i => $"{5 + i * 2} 0 R")) + $"] /Count {n} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
        };
        for (var i = 0; i < n; i++)
        {
            objs.Add(string.Create(CultureInfo.InvariantCulture, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {W} {H}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {6 + i * 2} 0 R >>"));
            var content = _pages[i].ToString();
            objs.Add($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}endstream");
        }
        using var ms = new MemoryStream();
        void Write(string s) { var b = Encoding.Latin1.GetBytes(s); ms.Write(b, 0, b.Length); }
        Write("%PDF-1.4\n");
        var offsets = new List<long>();
        for (var i = 0; i < objs.Count; i++) { offsets.Add(ms.Position); Write($"{i + 1} 0 obj\n{objs[i]}\nendobj\n"); }
        var xref = ms.Position;
        Write($"xref\n0 {objs.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) Write($"{o:D10} 00000 n \n");
        Write($"trailer\n<< /Size {objs.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return ms.ToArray();
    }
}
