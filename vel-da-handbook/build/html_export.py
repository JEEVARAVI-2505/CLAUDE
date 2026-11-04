"""Render the same reportlab flowables to a single self-contained HTML file (logo watermark embedded)."""
import base64
import html
import re
from pathlib import Path

from reportlab.graphics.renderSVG import drawToString
from reportlab.graphics.shapes import Drawing
from reportlab.platypus import KeepTogether, PageBreak, Paragraph, Preformatted, Spacer, Table

from vel_pdf import DOC_ID, DOC_VER, FOOTER_L, HEADER_L, HEADER_R, LOGO

CSS = """
:root{--navy:#1B2F6B;--mag:#B5174F;--grey:#5B6475;--head:#E8EDF8;--grid:#CBD2E0;--code:#F3F5F9;--bg:#fff;--fg:#1c2230}
@media (prefers-color-scheme:dark){:root{--navy:#9db4f2;--mag:#ff6b9a;--grey:#a3adc2;--head:#222c47;--grid:#3a4361;--code:#1a2033;--bg:#0f1424;--fg:#e6e9f2}}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--fg);font:15px/1.6 Inter,Segoe UI,Helvetica,Arial,sans-serif}
body::before{content:"";position:fixed;inset:0;background:url(__LOGO__) center 52%/min(70vw,640px) no-repeat;opacity:.09;pointer-events:none;z-index:0}
.layout{display:flex;position:relative;z-index:1}
nav{position:sticky;top:0;align-self:flex-start;width:270px;height:100vh;overflow:auto;padding:20px 14px;border-right:1px solid var(--grid);font-size:13px;background:color-mix(in srgb,var(--bg) 92%,transparent)}
nav a{display:block;padding:4px 6px;color:var(--navy);text-decoration:none;border-radius:4px}nav a:hover{background:var(--head)}
main{max-width:960px;padding:0 28px 60px;margin:0 auto;flex:1;min-width:0}
header.top{font-size:12px;color:var(--grey);display:flex;justify-content:space-between;padding:14px 0;border-bottom:1px solid var(--grid)}
footer{font-size:12px;color:var(--grey);padding:18px 0;border-top:1px solid var(--grid);margin-top:40px}
.cover{text-align:center;padding:50px 0 30px}.cover img{width:min(420px,80%)}
.cover h1{font-size:38px;color:var(--navy);margin:24px 0 8px;border:0}.cover .sub{color:var(--grey);font-size:17px}
h1{font-size:27px;color:var(--navy);border-bottom:3px solid var(--mag);padding-bottom:6px;margin:56px 0 18px}
h2{font-size:19px;color:var(--navy);margin:28px 0 8px;border-bottom:1px solid var(--grid);padding-bottom:3px}
h3{font-size:15px;color:var(--navy);margin:18px 0 6px}
table{border-collapse:collapse;width:100%;margin:10px 0 16px;font-size:13px;background:color-mix(in srgb,var(--bg) 88%,transparent)}
th,td{border:1px solid var(--grid);padding:6px 8px;vertical-align:top;text-align:left}th{background:var(--head);color:var(--navy)}
.scroll{overflow-x:auto}
pre{background:var(--code);border:1px solid var(--grid);padding:12px;overflow:auto;font:12.5px/1.45 ui-monospace,Consolas,monospace;margin:10px 0}
code{font:0.92em ui-monospace,Consolas,monospace;background:var(--code);padding:1px 4px;border-radius:3px}
.callout{border-left:4px solid var(--mag);background:#FFF4E5;color:#1c2230;padding:10px 14px;margin:14px 0;border-radius:3px}
.callout.ok{border-color:#2E7D4F;background:#E9F6EE}
ul,ol{padding-left:24px}li{margin:3px 0}.small{font-size:12.5px;color:var(--grey)}
svg text{font-family:Inter,Segoe UI,Helvetica,Arial,sans-serif!important}svg{max-width:100%;height:auto;display:block;margin:10px 0}
@media(max-width:860px){nav{display:none}}
@media print{nav{display:none}h1{page-break-before:always}body::before{opacity:.07}}
"""


def _inline(markup: str) -> str:
    """Convert reportlab mini-markup to HTML."""
    s = markup
    s = re.sub(r"<font face=['\"]Courier['\"]>(.*?)</font>", r"<code>\1</code>", s, flags=re.S)
    s = re.sub(r"<font color=['\"](#[0-9A-Fa-f]+)['\"]>(.*?)</font>", r'<span style="color:\1">\2</span>', s, flags=re.S)
    return s.replace("<br/>", "<br>")


def _plain(markup: str) -> str:
    return html.unescape(re.sub(r"<[^>]+>", "", markup))


class Exporter:
    def __init__(self):
        self.out, self.toc, self.n = [], [], 0
        self.list_tag = None  # open list: "ul" or "ol"

    def _close_list(self):
        if self.list_tag:
            self.out.append(f"</{self.list_tag}>")
            self.list_tag = None

    def _list_item(self, p):
        tag = "ul" if p.bulletText == "•" else "ol"
        if self.list_tag != tag:
            self._close_list()
            self.out.append(f"<{tag}>")
            self.list_tag = tag
        self.out.append(f"<li>{_inline(p.text)}</li>")

    def para(self, p: Paragraph):
        name = p.style.name
        if getattr(p, "bulletText", None):
            return self._list_item(p)
        self._close_list()
        if name == "h1":
            self.n += 1
            self.toc.append((self.n, _plain(p.text)))
            self.out.append(f'<h1 id="s{self.n}">{_inline(p.text)}</h1>')
        elif name in ("h2", "h3"):
            self.out.append(f"<{name}>{_inline(p.text)}</{name}>")
        elif name == "small":
            self.out.append(f'<p class="small">{_inline(p.text)}</p>')
        else:
            self.out.append(f"<p>{_inline(p.text)}</p>")

    def cell(self, c):
        if isinstance(c, Paragraph):
            return _inline(c.text)
        if isinstance(c, str):
            return html.escape(c)
        return ""

    def tbl(self, t: Table):
        rows = t._cellvalues
        # code box: single cell holding Preformatted
        if len(rows) == 1 and len(rows[0]) == 1 and isinstance(rows[0][0], Preformatted):
            self._close_list()
            text = "\n".join(rows[0][0].lines)
            if self.out and self.out[-1].startswith("<pre>") and self.out[-1].endswith("</pre>"):
                self.out[-1] = self.out[-1][:-6] + "\n" + html.escape(text) + "</pre>"
            else:
                self.out.append(f"<pre>{html.escape(text)}</pre>")
            return
        # callout: single cell holding a list [title, spacer, text]
        if len(rows) == 1 and len(rows[0]) == 1 and isinstance(rows[0][0], list):
            self._close_list()
            items = [x for x in rows[0][0] if isinstance(x, Paragraph)]
            ok = "ok" if t._bkgrndcmds and "E9F6EE" in repr(t._bkgrndcmds).upper() else ""
            self.out.append(f'<div class="callout {ok}">' + "".join(f"<p style='margin:2px 0'>{_inline(i.text)}</p>" for i in items) + "</div>")
            return
        self._close_list()
        h = ["<div class='scroll'><table>"]
        for r, row in enumerate(rows):
            tag = "th" if r == 0 and t.repeatRows else "td"
            h.append("<tr>" + "".join(f"<{tag}>{self.cell(c)}</{tag}>" for c in row) + "</tr>")
        h.append("</table></div>")
        self.out.append("".join(h))

    def walk(self, flowables):
        for f in flowables:
            if isinstance(f, Paragraph):
                self.para(f)
            elif isinstance(f, Table):
                if f._cellvalues == [[""]]:  # heading rule
                    continue
                self.tbl(f)
            elif isinstance(f, KeepTogether):
                self.walk(f._content)
            elif isinstance(f, Drawing):
                self._close_list()
                self.out.append(_svg(f))
            elif isinstance(f, (Spacer, PageBreak)):
                continue
        self._close_list()


def _svg(d: Drawing) -> str:
    """SVG scaled to the page width via viewBox."""
    s = drawToString(d)
    s = s[s.find("<svg"):]
    m = re.search(r'<svg[^>]*>', s)
    tag = m.group(0)
    w = re.search(r'width="([\d.]+)', tag).group(1)
    h = re.search(r'height="([\d.]+)', tag).group(1)
    new = re.sub(r'\s(width|height)="[^"]*"', "", tag)[:-1] + f' viewBox="0 0 {w} {h}" width="100%">'
    return s.replace(tag, new, 1)


def export(story_parts, out_path: Path):
    ex = Exporter()
    for flowables in story_parts:
        ex.walk(flowables)
    logo = "data:image/png;base64," + base64.b64encode(Path(LOGO).read_bytes()).decode()
    nav = "".join(f'<a href="#s{n}">{html.escape(t)}</a>' for n, t in ex.toc)
    cover = (f'<section class="cover"><img src="{logo}" alt="VEL Software Solutions logo">'
             "<h1>Data Analytics &amp; Business Intelligence<br>Department Handbook</h1>"
             '<p class="sub">Training, Project Engineering and Client Delivery Operating System<br>'
             "Beginner to Professional · 0 to 100% Project Lifecycle</p>"
             "<p>Developed by <b>V. LOKESHWARAN</b><br>VEL Software Solutions<br>velsoftwaresolutions.in@gmail.com</p>"
             f'<p class="small">Document {DOC_ID} · Version {DOC_VER} · Module 1: Foundation, Onboarding and 90-Day Plan</p></section>')
    doc = (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
           f"<title>VEL Data Analytics and BI Handbook</title><style>{CSS.replace('__LOGO__', logo)}</style></head><body>"
           f'<div class="layout"><nav><b>Contents</b>{nav}</nav><main><header class="top"><span>{html.escape(HEADER_L)}</span>'
           f"<span>{HEADER_R}</span></header>{cover}{''.join(ex.out)}<footer>{html.escape(FOOTER_L)}</footer></main></div></body></html>")
    out_path.write_text(doc, encoding="utf-8")
