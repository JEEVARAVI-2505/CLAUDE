"""Build the VEL Delivery Handbook as one HTML file (print-ready). Usage: python scripts/build_handbook.py
Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com"""
import base64, re
from pathlib import Path
import markdown

ROOT = Path(__file__).resolve().parent.parent
ORDER = ["00-start-here", "01-how-a-software-company-works", "02-folder-structure-explained", "S00-workstation-and-habits",
         "S01-opportunity-and-customer-discovery", "S02-understand-the-process-as-is", "S03-feasibility-and-to-be-design",
         "S04-requirements-and-scope-freeze", "S05-technology-and-architecture", "S06-project-setup", "S07-build-the-core",
         "S08-ai-and-integrations", "S09-testing-and-ai-evaluation", "S10-security-review", "S11-costing-and-pricing",
         "S12-deployment", "S13-demo-acceptance-onboarding", "S14-maintenance-and-improvement", "A-appendices"]
logo = base64.b64encode((ROOT / "assets/vel-logo-small.webp").read_bytes()).decode()
logo_big = base64.b64encode((ROOT / "assets/vel-logo.webp").read_bytes()).decode()
md = markdown.Markdown(extensions=["tables", "fenced_code", "sane_lists"])

def render(text):
    md.reset()
    html = md.convert(text)
    html = html.replace("<li>[ ] ", '<li class="chk">').replace("<li>[x] ", '<li class="chk">')
    return re.sub(r'<p><em>Developed by.*?</em></p>', '', html)  # footer is added on every page instead

sections, toc = [], []
for i, name in enumerate(ORDER):
    text = (ROOT / "handbook" / f"{name}.md").read_text()
    title = re.search(r"^# (.+)$", text, re.M).group(1)
    toc.append((f"s{i}", title))
    body = render(text).replace("<h1>", f'<h1 id="s{i}">', 1)
    sections.append(f'<section class="chap">{body}</section>')

toc_html = "".join(f'<li><a href="#{a}">{t}</a></li>' for a, t in toc)
css = """
:root{--ink:#1b2430;--muted:#5a6573;--line:#cfd6de;--navy:#14306b;--pink:#c2185b;--gold:#b8860b}
*{box-sizing:border-box}body{font:11pt/1.5 'Segoe UI',Helvetica,Arial,sans-serif;color:var(--ink);margin:0}
.wm{position:fixed;inset:0;background:url(data:image/webp;base64,%(big)s) center 55%%/60%% no-repeat;opacity:.05;z-index:-1}
.cover{height:250mm;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;gap:10mm;page-break-after:always}
.cover img{width:110mm}.cover h1{font-size:30pt;color:var(--navy);margin:0;border:0}.cover h2{font-size:15pt;font-weight:400;color:var(--muted);margin:0;border:0}
.cover .by{margin-top:14mm;font-size:11pt;color:var(--ink)}.cover .by b{color:var(--navy)}
.tocpage{page-break-after:always}.tocpage h1{color:var(--navy)}.tocpage ol{line-height:2}.tocpage a{color:var(--ink);text-decoration:none}
.chap{page-break-before:always}
h1{font-size:21pt;color:var(--navy);border-bottom:3px solid var(--pink);padding-bottom:4mm;margin:0 0 6mm}
h2{font-size:14pt;color:var(--navy);margin:8mm 0 3mm;border-bottom:1px solid var(--line);padding-bottom:1mm;page-break-after:avoid}
h3{font-size:12pt;color:var(--ink);margin:5mm 0 2mm;page-break-after:avoid}
table{border-collapse:collapse;width:100%%;margin:3mm 0;font-size:9.5pt;page-break-inside:auto}
tr{page-break-inside:avoid}th{background:#eef2f8;color:var(--navy);text-align:left}
th,td{border:1px solid var(--line);padding:2mm 2.5mm;vertical-align:top}
code{font-family:Consolas,'Courier New',monospace;font-size:9pt;background:#f2f4f7;padding:0 1mm;border-radius:2px}
pre{background:#f2f4f7;border:1px solid var(--line);border-left:3px solid var(--navy);padding:3mm;font-size:8.5pt;white-space:pre-wrap;word-break:break-word;page-break-inside:avoid}
pre code{background:none;padding:0}
li.chk{list-style:none;margin-left:-5mm}li.chk::before{content:"\\2610  ";color:var(--navy);font-weight:700}
hr{border:0;border-top:1px solid var(--line);margin:6mm 0}p,li{orphans:3;widows:3}
""" % {"big": logo_big}
html = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>VEL Delivery Handbook</title><style>{css}</style></head><body>
<div class="wm"></div>
<div class="cover"><img src="data:image/webp;base64,{logo_big}" alt="VEL Software Solutions">
<h1>Delivery Handbook</h1><h2>AI Automation Projects: from Day 0 to 100% Delivery</h2>
<div class="by">Developed by <b>V. LOKESHWARAN</b><br>VEL Software Solutions<br>velsoftwaresolutions.in@gmail.com</div>
<div style="color:var(--muted);font-size:9.5pt">Document VEL-DH-001 · Version 1.0 (draft for management approval)</div></div>
<div class="tocpage"><h1>Contents</h1><ol>{toc_html}</ol>
<p style="color:var(--muted);font-size:9.5pt">All prices and market figures in this handbook's examples are placeholders until verified from official sources (see Stage S11).</p></div>
{''.join(sections)}</body></html>"""
out = ROOT / "dist" / "VEL-Delivery-Handbook.html"
out.write_text(html)
print("wrote", out, len(html) // 1024, "KB;", len(ORDER), "sections")
