"""Build VEL API & System Integration Handbook: HTML (self-contained) + PDF (via Playwright)."""
import base64, pathlib, re, subprocess, sys
import markdown

ROOT = pathlib.Path(__file__).parent
TITLE = "API &amp; System Integration Handbook"
DOCID = "VEL-API-001 v1.0"
logo_b64 = base64.b64encode((ROOT / "assets/vel-logo.png").read_bytes()).decode()
LOGO = f"data:image/png;base64,{logo_b64}"

def include_files(text: str) -> str:
    def rep(m):
        p, lang = m.group(1), m.group(2)
        return f"```{lang}\n{(ROOT / p).read_text().rstrip()}\n```"
    return re.sub(r"\{\{file:([^|}]+)\|([^}]+)\}\}", rep, text)

def render(md_text: str) -> str:
    md = markdown.Markdown(extensions=["tables", "fenced_code", "attr_list", "md_in_html", "sane_lists", "codehilite"],
                           extension_configs={"codehilite": {"noclasses": True, "pygments_style": "friendly", "guess_lang": False}})
    return md.convert(md_text)

sections, toc = [], []
for i, f in enumerate(sorted((ROOT / "src").glob("*.md"))):
    html = render(include_files(f.read_text()))
    h1 = re.search(r"<h1>(.*?)</h1>", html).group(1)
    sid = f"s{i}"
    html = html.replace("<h1>", f'<h1 id="{sid}">', 1)
    # stage / numbered h2 anchors
    html = re.sub(r"<h2>(.*?)</h2>", lambda m: f'<h2 id="{re.sub(r"[^a-z0-9]+","-",re.sub(r"<[^>]+>","",m.group(1)).lower()).strip("-")}">{m.group(1)}</h2>', html)
    html = html.replace("<table>", '<div class="tw"><table>').replace("</table>", "</table></div>")
    toc.append((sid, h1, re.findall(r'<h2 id="([^"]+)">(.*?)</h2>', html)))
    sections.append(f'<section class="chapter {"first" if i == 0 else ""}">{html}</section>')

toc_html = "".join(f'<li><a href="#{sid}">{t}</a></li>' for sid, t, _ in toc)
nav_html = "".join(
    f'<div class="nv"><a class="h" href="#{sid}">{t}</a></div>' for sid, t, subs in toc)

css = (ROOT / "style.css").read_text()
doc = f"""<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>VEL API &amp; System Integration Handbook</title><style>{css}</style></head><body>
<img class="watermark" src="{LOGO}" alt="">
<nav class="side"><img src="{LOGO}" alt="VEL Software Solutions"><div class="docid">{DOCID}</div>{nav_html}</nav>
<main>
<section class="cover">
  <img class="coverlogo" src="{LOGO}" alt="VEL Software Solutions">
  <h1 class="covertitle">API &amp; System Integration<br>Handbook</h1>
  <p class="coversub">Department Charter, Corporate Workflow, Onboarding, 90-Day Roadmap<br>and First Practical Assignment: from Beginner to Supervised Commercial Delivery</p>
  <p class="coverby">Developed by <b>V. LOKESHWARAN</b><br>VEL Software Solutions<br>velsoftwaresolutions.in@gmail.com</p>
  <p class="coverdoc">Document {DOCID} · Module 1 · Draft for management approval · November 2026</p>
</section>
<section class="chapter"><h1 class="toch">Contents</h1><ol class="toc">{toc_html}</ol>
<p class="small">All prices and market figures in this handbook's examples are placeholders until verified from official sources (see Stage 06). Structural, legal and tax matters need qualified review.</p></section>
{''.join(sections)}
</main></body></html>"""
out = ROOT / "VEL-API-Integration-Handbook.html"
out.write_text(doc)
print("HTML", out.stat().st_size // 1024, "KB")
