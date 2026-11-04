"""VEL house style for reportlab: page furniture, watermark, flowable helpers."""
from reportlab.graphics.shapes import Drawing, Line, Polygon, Rect, String
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfgen import canvas
from reportlab.platypus import (KeepTogether, PageBreak, Paragraph, Preformatted, Spacer, Table,
                                TableStyle)
from reportlab.platypus.doctemplate import BaseDocTemplate, PageTemplate
from reportlab.platypus.frames import Frame

NAVY = colors.HexColor("#1B2F6B")
MAGENTA = colors.HexColor("#B5174F")
GREY = colors.HexColor("#5B6475")
HEAD_FILL = colors.HexColor("#E8EDF8")
GRID = colors.HexColor("#CBD2E0")
CODE_BG = colors.HexColor("#F3F5F9")
WARN_BG = colors.HexColor("#FFF4E5")
OK_BG = colors.HexColor("#E9F6EE")

DOC_ID, DOC_VER = "VEL-DA-001", "1.0"
HEADER_L = f"VEL Software Solutions · Data Analytics & BI Handbook ({DOC_ID} v{DOC_VER})"
HEADER_R = "Confidential · internal use"
FOOTER_L = "Developed by V. LOKESHWARAN · velsoftwaresolutions.in@gmail.com"
LOGO = "/home/user/CLAUDE/vel-da-handbook/assets/vel-logo.png"
W, H = A4
LM = RM = 19 * mm

base = ParagraphStyle("base", fontName="Helvetica", fontSize=9.4, leading=13.6, spaceAfter=5)
S = {
    "p": base,
    "h1": ParagraphStyle("h1", parent=base, fontName="Helvetica-Bold", fontSize=21, leading=26,
                         textColor=NAVY, spaceAfter=4),
    "h2": ParagraphStyle("h2", parent=base, fontName="Helvetica-Bold", fontSize=13.2, leading=17,
                         textColor=NAVY, spaceBefore=11, spaceAfter=5, keepWithNext=1),
    "h3": ParagraphStyle("h3", parent=base, fontName="Helvetica-Bold", fontSize=10.4, leading=14,
                         textColor=NAVY, spaceBefore=6, spaceAfter=3, keepWithNext=1),
    "cell": ParagraphStyle("cell", parent=base, fontSize=8.3, leading=10.8, spaceAfter=0),
    "cellb": ParagraphStyle("cellb", parent=base, fontName="Helvetica-Bold", fontSize=8.3,
                            leading=10.8, spaceAfter=0, textColor=NAVY),
    "bullet": ParagraphStyle("bullet", parent=base, leftIndent=14, bulletIndent=3, spaceAfter=2.5),
    "small": ParagraphStyle("small", parent=base, fontSize=8, leading=11, textColor=GREY),
    "code": ParagraphStyle("code", fontName="Courier", fontSize=7.6, leading=9.6),
    "ctitle": ParagraphStyle("ctitle", parent=base, fontName="Helvetica-Bold", fontSize=34,
                             leading=40, textColor=NAVY, alignment=TA_CENTER),
    "csub": ParagraphStyle("csub", parent=base, fontSize=15, leading=20, textColor=GREY,
                           alignment=TA_CENTER),
    "cmid": ParagraphStyle("cmid", parent=base, fontSize=10.5, leading=15, alignment=TA_CENTER),
}


class Doc(BaseDocTemplate):
    """Document template with TOC notification and watermark page template."""

    def __init__(self, path, **kw):
        super().__init__(path, pagesize=A4, leftMargin=LM, rightMargin=RM, topMargin=24 * mm,
                         bottomMargin=20 * mm, title="Data Analytics & BI Department Handbook",
                         author="V. LOKESHWARAN, VEL Software Solutions", **kw)
        frame = Frame(LM, 20 * mm, W - LM - RM, H - 44 * mm, id="f", leftPadding=0, rightPadding=0,
                      topPadding=0, bottomPadding=0)
        self.addPageTemplates([PageTemplate(id="main", frames=[frame], onPage=self._watermark)])

    @staticmethod
    def _watermark(c, _doc):
        c.saveState()
        c.setFillAlpha(0.085)
        size = 120 * mm
        c.drawImage(LOGO, (W - size) / 2, (H - size * 630 / 936) / 2 - 8 * mm, width=size,
                    height=size * 630 / 936, mask="auto", preserveAspectRatio=True)
        c.restoreState()

    def afterFlowable(self, f):
        if isinstance(f, Paragraph) and f.style.name in ("h1", "h2"):
            lvl = 0 if f.style.name == "h1" else 1
            text = f.getPlainText()
            key = f"k{self.seq.nextf('toc')}"
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(text, key, level=lvl, closed=True)
            if text != "Contents" and (lvl == 0) or f.style.name == "h2" and getattr(f, "_toc", False):
                self.notify("TOCEntry", (lvl, text, self.page, key))


class NumberedCanvas(canvas.Canvas):
    """Adds header and 'Page X of N' footer after total page count is known."""

    def __init__(self, *a, **kw):
        super().__init__(*a, **kw)
        self._saved = []

    def showPage(self):
        self._saved.append(dict(self.__dict__))
        self._startPage()

    def save(self):
        total = len(self._saved)
        for st in self._saved:
            self.__dict__.update(st)
            self._furniture(total)
            super().showPage()
        super().save()

    def _furniture(self, total):
        self.setFont("Helvetica", 7.2)
        self.setFillColor(GREY)
        self.drawString(LM, H - 11 * mm, HEADER_L)
        self.drawRightString(W - RM, H - 11 * mm, HEADER_R)
        self.drawString(LM, 10 * mm, FOOTER_L)
        self.drawRightString(W - RM, 10 * mm, f"Page {self._pageNumber} of {total}")


# ---------- flowable helpers ----------
def esc(t):
    return t


def P(text, style="p"):
    return Paragraph(text, S[style])


def H1(text):
    p = Paragraph(text, S["h1"])
    rule = Table([[""]], colWidths=[W - LM - RM], rowHeights=[2])
    rule.setStyle(TableStyle([("LINEABOVE", (0, 0), (-1, 0), 2, MAGENTA)]))
    return [p, rule, Spacer(1, 8)]


def H2(text, toc=False):
    p = Paragraph(text, S["h2"])
    p._toc = toc
    return [p]


def H3(text):
    return [Paragraph(text, S["h3"])]


def bullets(items):
    return [Paragraph(i, S["bullet"], bulletText="•") for i in items]


def numbered(items):
    return [Paragraph(i, S["bullet"], bulletText=f"{n}.") for n, i in enumerate(items, 1)]


def table(rows, widths, header=True, zebra=False, fs=None):
    """rows: list of lists of str. widths: fractions of text width."""
    tw = W - LM - RM
    data = []
    for r, row in enumerate(rows):
        st = "cellb" if (header and r == 0) else "cell"
        data.append([c if not isinstance(c, str) else Paragraph(c, S[st]) for c in row])
    t = Table(data, colWidths=[w * tw for w in widths], repeatRows=1 if header else 0)
    style = [("GRID", (0, 0), (-1, -1), 0.5, GRID), ("VALIGN", (0, 0), (-1, -1), "TOP"),
             ("LEFTPADDING", (0, 0), (-1, -1), 5), ("RIGHTPADDING", (0, 0), (-1, -1), 5),
             ("TOPPADDING", (0, 0), (-1, -1), 4), ("BOTTOMPADDING", (0, 0), (-1, -1), 4)]
    if header:
        style.append(("BACKGROUND", (0, 0), (-1, 0), HEAD_FILL))
    if zebra:
        for i in range(2, len(rows), 2):
            style.append(("BACKGROUND", (0, i), (-1, i), colors.HexColor("#F8F9FC")))
    t.setStyle(TableStyle(style))
    return [t, Spacer(1, 7)]


def code(text, chunk=46):
    """Monospace block; long listings are split into separate boxes so they can break across pages."""
    lines = text.strip("\n").split("\n")
    out = []
    for i in range(0, len(lines), chunk):
        pre = Preformatted("\n".join(lines[i:i + chunk]), S["code"])
        t = Table([[pre]], colWidths=[W - LM - RM])
        t.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, -1), CODE_BG),
                               ("BOX", (0, 0), (-1, -1), 0.5, GRID), ("LEFTPADDING", (0, 0), (-1, -1), 7),
                               ("TOPPADDING", (0, 0), (-1, -1), 5), ("BOTTOMPADDING", (0, 0), (-1, -1), 5)]))
        out.append(t)
    return out + [Spacer(1, 7)]


def callout(title, text, kind="warn"):
    bg = WARN_BG if kind == "warn" else OK_BG
    edge = MAGENTA if kind == "warn" else colors.HexColor("#2E7D4F")
    body = [Paragraph(f"<b>{title}</b>", S["cell"]), Spacer(1, 2), Paragraph(text, S["cell"])]
    t = Table([[body]], colWidths=[W - LM - RM])
    t.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, -1), bg), ("LINEBEFORE", (0, 0), (0, -1), 3, edge),
                           ("LEFTPADDING", (0, 0), (-1, -1), 9), ("TOPPADDING", (0, 0), (-1, -1), 6),
                           ("BOTTOMPADDING", (0, 0), (-1, -1), 6)]))
    return [KeepTogether([t]), Spacer(1, 7)]


def flow(labels, width=None, fill=HEAD_FILL, box_h=34, fs=7.6, per_row=None):
    """Horizontal box-and-arrow flow diagram (wraps into rows if per_row)."""
    tw = width or (W - LM - RM)
    per_row = per_row or len(labels)
    rows = [labels[i:i + per_row] for i in range(0, len(labels), per_row)]
    gap = 14
    bw = (tw - gap * (per_row - 1)) / per_row
    h = len(rows) * (box_h + 16) + 2
    d = Drawing(tw, h)
    for ri, row in enumerate(rows):
        y = h - (ri + 1) * (box_h + 16) + 8
        for ci, lab in enumerate(row):
            x = ci * (bw + gap)
            d.add(Rect(x, y, bw, box_h, rx=4, ry=4, fillColor=fill, strokeColor=NAVY, strokeWidth=0.8))
            for li, line in enumerate(lab.split("\n")):
                n = len(lab.split("\n"))
                d.add(String(x + bw / 2, y + box_h / 2 + (n - 1) * 4.3 - li * 8.6 - 2.5, line,
                             fontName="Helvetica-Bold" if li == 0 else "Helvetica", fontSize=fs,
                             fillColor=NAVY, textAnchor="middle"))
            if ci < len(row) - 1:
                ax = x + bw + 1
                d.add(Line(ax, y + box_h / 2, ax + gap - 3, y + box_h / 2, strokeColor=MAGENTA, strokeWidth=1.2))
                d.add(Polygon([ax + gap - 3, y + box_h / 2 + 3, ax + gap + 1, y + box_h / 2, ax + gap - 3,
                               y + box_h / 2 - 3], fillColor=MAGENTA, strokeColor=MAGENTA))
    return [d, Spacer(1, 6)]


def orgchart():
    """Department organisation chart drawing."""
    tw = W - LM - RM
    d = Drawing(tw, 230)

    def box(x, y, w, h, lines, fill=HEAD_FILL):
        d.add(Rect(x, y, w, h, rx=4, ry=4, fillColor=fill, strokeColor=NAVY, strokeWidth=0.8))
        for i, ln in enumerate(lines):
            d.add(String(x + w / 2, y + h - 12 - i * 9.5, ln, fontName="Helvetica-Bold" if i == 0 else "Helvetica",
                         fontSize=7.6, fillColor=NAVY, textAnchor="middle"))

    def link(x1, y1, x2, y2):
        d.add(Line(x1, y1, x2, y2, strokeColor=GREY, strokeWidth=0.9))

    cx = tw / 2
    box(cx - 70, 190, 140, 34, ["Founder / Managing Director", "Commercial and final approval"],
        colors.HexColor("#DCE4F5"))
    box(cx - 80, 138, 160, 34, ["Head of Data & Analytics", "(Technical Lead, Delivery Owner)"])
    link(cx, 190, cx, 172)
    bw, gap = 112, 8
    names = [["Data Engineer", "Pipelines, DB, warehouse"], ["BI Developer", "Power BI / Tableau"],
             ["Data Analyst", "Analysis, KPIs, SQL"], ["Data Scientist / ML", "Forecast, churn, anomaly"],
             ["QA & Data Quality", "Reconciliation, tests"]]
    total = len(names) * bw + (len(names) - 1) * gap
    x0 = (tw - total) / 2
    link(cx, 138, cx, 122)
    link(x0 + bw / 2, 122, x0 + total - bw / 2, 122)
    for i, n in enumerate(names):
        x = x0 + i * (bw + gap)
        link(x + bw / 2, 122, x + bw / 2, 106)
        box(x, 70, bw, 36, n)
    box(x0, 14, 2 * bw + gap, 36, ["Business Analyst / Client Lead", "Discovery, requirements, UAT"],
        colors.HexColor("#F3F5F9"))
    box(x0 + 2 * (bw + gap), 14, 2 * bw + gap, 36, ["Project Manager (shared)", "Plan, risks, client comms"],
        colors.HexColor("#F3F5F9"))
    box(x0 + 4 * (bw + gap) - 0, 14, bw, 36, ["Trainees", "Report to buddy + Lead"],
        colors.HexColor("#FDEBF1"))
    link(cx, 70, cx, 50)
    return [d, Spacer(1, 4)]


def pb():
    return PageBreak()
