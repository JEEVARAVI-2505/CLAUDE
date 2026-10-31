using System.Globalization;
using System.Text;

namespace CRM_Master.Engine.Reporting;

/// <summary>
/// Dependency-free PDF writer for tabular reports (A4 landscape, standard Helvetica fonts, repeating header row, page numbers).
/// Text is limited to Latin characters (others print as '?'); use the Excel export when full Unicode is needed.
/// </summary>
public static class MiniPdfWriter
{
    private const double PageW = 842, PageH = 595, Margin = 36, RowH = 15, FontSize = 8;

    public static byte[] Render(ReportData report)
    {
        var pages = Layout(report);
        var objects = new List<string>();                                 // object bodies, 1-based ids: 1 catalog, 2 pages, 3 font, 4 bold, then page+content pairs
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add("");                                                  // placeholder for Pages
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        var kids = new List<int>();
        for (var i = 0; i < pages.Count; i++)
        {
            var content = pages[i].Replace("{PAGES}", pages.Count.ToString(CultureInfo.InvariantCulture)).Replace("{PAGE}", (i + 1).ToString(CultureInfo.InvariantCulture));
            var pageId = objects.Count + 1; var contentId = pageId + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageW} {PageH}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>");
            objects.Add($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
            kids.Add(pageId);
        }
        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", kids.Select(k => k + " 0 R"))}] /Count {kids.Count} >>";

        using var ms = new MemoryStream();
        void W(string s) { var b = Encoding.Latin1.GetBytes(s); ms.Write(b, 0, b.Length); }
        W("%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++) { offsets.Add(ms.Position); W($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = ms.Position;
        W($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) W($"{o:D10} 00000 n \n");
        W($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info << /Title ({Esc(report.Title)}) /Producer (CRM MASTER) >> >>\nstartxref\n{xref}\n%%EOF\n");
        return ms.ToArray();
    }

    private static List<string> Layout(ReportData r)
    {
        var pages = new List<string>(); var sb = new StringBuilder(); double y = 0;
        var usableW = PageW - 2 * Margin;
        var widths = ColumnWidths(r, usableW);

        void NewPage()
        {
            if (sb.Length > 0) pages.Add(Footer(sb, r));
            sb = new StringBuilder(); y = PageH - Margin;
        }
        void Text(double x, double yy, string s, bool bold = false, double size = FontSize, string color = "0.1 0.1 0.15")
            => sb.Append($"BT /{(bold ? "F2" : "F1")} {size.ToString(CultureInfo.InvariantCulture)} Tf {color} rg {x.ToString("F2", CultureInfo.InvariantCulture)} {yy.ToString("F2", CultureInfo.InvariantCulture)} Td ({Esc(s)}) Tj ET\n");
        void Rect(double x, double yy, double w, double h, string rgb) => sb.Append($"{rgb} rg {x.ToString("F2", CultureInfo.InvariantCulture)} {yy.ToString("F2", CultureInfo.InvariantCulture)} {w.ToString("F2", CultureInfo.InvariantCulture)} {h.ToString("F2", CultureInfo.InvariantCulture)} re f\n");

        NewPage();
        Rect(0, PageH - 54, PageW, 54, "0.06 0.15 0.33");
        Text(Margin, PageH - 30, r.Title, true, 16, "1 1 1");
        if (r.Subtitle is not null) Text(Margin, PageH - 45, r.Subtitle, false, 9, "0.8 0.87 1");
        y = PageH - 54 - 20;
        foreach (var (label, value) in r.Summary)
        {
            if (y < Margin + 40) NewPage();
            Text(Margin, y, label + ":", true, 9); Text(Margin + 150, y, Fit(value, 9, usableW - 150, false), false, 9); y -= 13;
        }
        if (r.Summary.Count > 0) y -= 8;

        void HeaderRow()
        {
            Rect(Margin, y - 4, usableW, RowH, "0.90 0.93 0.98");
            var x = Margin + 3;
            for (var c = 0; c < r.Columns.Count; c++) { Text(x, y, Fit(r.Columns[c], FontSize, widths[c] - 6, true), true); x += widths[c]; }
            y -= RowH;
        }
        HeaderRow();
        for (var i = 0; i < r.Rows.Count; i++)
        {
            if (y < Margin + 24) { NewPage(); y = PageH - Margin; HeaderRow(); }
            if (i % 2 == 1) Rect(Margin, y - 4, usableW, RowH, "0.97 0.97 0.98");
            var x = Margin + 3;
            for (var c = 0; c < r.Columns.Count; c++) { Text(x, y, Fit(Format(r.Rows[i][c]), FontSize, widths[c] - 6, false)); x += widths[c]; }
            y -= RowH;
        }
        if (r.Rows.Count == 0) Text(Margin, y, "No records match the selected filters.", false, 9, "0.4 0.4 0.4");
        pages.Add(Footer(sb, r));
        return pages;
    }

    private static string Footer(StringBuilder sb, ReportData r)
    {
        var footer = $"{r.Footer}   |   Generated {r.GeneratedAt:yyyy-MM-dd HH:mm}   |   Page {{PAGE}} of {{PAGES}}";
        sb.Append($"BT /F1 7 Tf 0.4 0.4 0.45 rg {Margin} 18 Td ({Esc(footer).Replace("\\{", "{").Replace("\\}", "}")}) Tj ET\n");
        return sb.ToString();
    }

    private static string Format(object? v) => v switch
    {
        null => "", DateTime d => d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), bool b => b ? "Yes" : "No",
        double d => d.ToString("0.##", CultureInfo.InvariantCulture), IFormattable f => f.ToString(null, CultureInfo.InvariantCulture), _ => v.ToString() ?? ""
    };

    private static double[] ColumnWidths(ReportData r, double total)
    {
        if (r.Columns.Count == 0) return new[] { total };
        var n = r.Columns.Count;
        var want = new double[n];
        for (var c = 0; c < n; c++)
        {
            var max = TextWidth(r.Columns[c], FontSize, true);
            foreach (var row in r.Rows.Take(300)) max = Math.Max(max, TextWidth(Format(row[c]), FontSize, false));
            want[c] = Math.Clamp(max + 10, 34, 260);
        }
        var sum = want.Sum();
        if (sum <= total) { var extra = (total - sum) / n; return want.Select(w => w + extra).ToArray(); }
        var scale = total / sum;
        return want.Select(w => Math.Max(28, w * scale)).ToArray();
    }

    private static double TextWidth(string s, double size, bool bold)
    {
        double w = 0;
        foreach (var ch in s)
            w += ch switch
            {
                ' ' => 0.278, 'i' or 'l' or 'j' or 't' or 'f' or 'I' or '.' or ',' or ':' or ';' or '\'' or '|' or '!' => 0.28,
                'm' or 'w' or 'M' or 'W' or '@' => 0.85, >= 'A' and <= 'Z' => 0.68, >= '0' and <= '9' => 0.556, _ => 0.52
            } * (bold ? 1.06 : 1.0);
        return w * size;
    }

    private static string Fit(string s, double size, double width, bool bold)
    {
        s = s.Replace('\n', ' ').Replace('\r', ' ');
        if (TextWidth(s, size, bold) <= width) return s;
        while (s.Length > 1 && TextWidth(s + "...", size, bold) > width) s = s[..^1];
        return s + "...";
    }

    private static string Esc(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (var ch in s)
        {
            if (ch is '(' or ')' or '\\') { sb.Append('\\').Append(ch); }
            else if (ch == '₹') sb.Append("Rs");
            else if (ch is >= ' ' and <= '~' or (>= '¡' and <= 'ÿ')) sb.Append(ch);
            else if (ch is '–' or '—') sb.Append('-');
            else if (ch is '‘' or '’') sb.Append('\'');
            else if (ch is '“' or '”') sb.Append('"');
            else if (ch is '\n' or '\r' or '\t') sb.Append(' ');
            else sb.Append('?');
        }
        return sb.ToString();
    }
}
