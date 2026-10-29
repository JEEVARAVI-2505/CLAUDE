using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BuildingStudio.IO.Pdf;

public abstract record ReportBlock;
public sealed record ParagraphBlock(string Text, bool Warning = false) : ReportBlock;
public sealed record TableBlock(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) : ReportBlock;
public sealed record SvgBlock(string Svg, float Height = 420, string Caption = "") : ReportBlock;
public sealed record KeyValueBlock(IReadOnlyList<(string Key, string Value)> Pairs) : ReportBlock;
public sealed record ReportSection(string Heading, IReadOnlyList<ReportBlock> Blocks, bool NewPage = false);
public sealed record ReportContent(string Title, string Subtitle, string Banner, IReadOnlyList<ReportSection> Sections);

/// <summary>Generic A4 report writer (QuestPDF, Community licence).</summary>
public static class PdfReportWriter
{
    static PdfReportWriter() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Render(ReportContent content) => Document.Create(doc =>
    {
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(t => t.FontSize(9));
            page.Header().Column(h =>
            {
                h.Item().Text(content.Title).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                h.Item().Text(content.Subtitle).FontColor(Colors.Grey.Darken1);
                if (content.Banner.Length > 0)
                    h.Item().PaddingTop(4).Background(Colors.Orange.Lighten4).Padding(4).Text(content.Banner).Bold().FontColor(Colors.Orange.Darken4);
            });
            page.Content().PaddingVertical(8).Column(col =>
            {
                foreach (var section in content.Sections)
                {
                    if (section.NewPage) col.Item().PageBreak();
                    col.Item().PaddingTop(10).Text(section.Heading).FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    foreach (var block in section.Blocks) col.Item().PaddingTop(4).Element(c => Block(c, block));
                }
            });
            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("AI Building Studio · page ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }).GeneratePdf();

    public static void Save(ReportContent content, string path) => File.WriteAllBytes(path, Render(content));

    private static void Block(IContainer c, ReportBlock block)
    {
        switch (block)
        {
            case ParagraphBlock p:
                if (p.Warning) c.Background(Colors.Red.Lighten5).Padding(4).Text(p.Text).FontColor(Colors.Red.Darken3);
                else c.Text(p.Text);
                break;
            case KeyValueBlock kv:
                c.Table(t =>
                {
                    t.ColumnsDefinition(d => { d.ConstantColumn(170); d.RelativeColumn(); });
                    foreach (var (k, v) in kv.Pairs)
                    {
                        t.Cell().PaddingVertical(1).Text(k).Bold();
                        t.Cell().PaddingVertical(1).Text(v);
                    }
                });
                break;
            case TableBlock tb:
                c.Table(t =>
                {
                    t.ColumnsDefinition(d => { foreach (var _ in tb.Headers) d.RelativeColumn(); });
                    t.Header(h =>
                    {
                        foreach (var head in tb.Headers)
                            h.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text(head).Bold();
                    });
                    foreach (var row in tb.Rows)
                        foreach (var cell in row)
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(cell);
                });
                break;
            case SvgBlock svg:
                c.Column(col =>
                {
                    col.Item().Height(svg.Height).Svg(svg.Svg).FitArea();
                    if (svg.Caption.Length > 0) col.Item().AlignCenter().Text(svg.Caption).Italic().FontColor(Colors.Grey.Darken1);
                });
                break;
        }
    }
}
