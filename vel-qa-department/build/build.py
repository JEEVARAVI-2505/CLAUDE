#!/usr/bin/env python3
"""VEL QA Department — document build system.

One content source -> standalone HTML (web) and A4 PDFs (print).  Run:  python3 build/build.py [--only handbook]
Outputs go to ../dist.  Requires: Python 3.10+, Node + Playwright (Chromium) for PDF rendering.
"""
from __future__ import annotations

import base64
import html
import importlib
import io
import pathlib
import re
import subprocess
import sys

HERE = pathlib.Path(__file__).parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE / "content"))
import lib  # noqa: E402

DIST = HERE.parent / "dist"
COMPANY = "VEL Software Solutions"
EMAIL = "velsoftwaresolutions.in@gmail.com"
ISSUED = "4 November 2026"
VERSION = "1.0"

# name -> (module, function) lists are resolved lazily so each module stays small.
DOCS = {
    "handbook": dict(
        file="VEL-QA-Department-Handbook", id="VEL-QA-001", short="QA Department Handbook",
        kicker="Quality Engineering Department",
        title="Software Testing &amp; QA Automation<br>Department Handbook",
        subtitle="From zero to professional: onboarding, training, client QA delivery, automation, "
                 "performance, costing, metrics and release sign-off",
        parts=[("c01_front", "how_to_use"), ("c01_front", "portfolio"), ("c02_onboarding", "onboarding"),
               ("c02_onboarding", "first_assignment"), ("c03_levels_1_2", "level1"), ("c03_levels_1_2", "level2"),
               ("c04_levels_3_4", "level3"), ("c04_levels_3_4", "level4"), ("c05_level5", "level5"),
               ("c06_level6", "level6"), ("c07_tools", "tools"), ("c08_lifecycle", "lifecycle"),
               ("c09_repo_docs", "repository"), ("c09_repo_docs", "doc_standards"), ("c10_metrics_cost", "metrics"),
               ("c10_metrics_cost", "costing"), ("c11_roles", "roles"), ("c12_projects_plan", "projects"),
               ("c12_projects_plan", "training_plan"), ("c13_assessment", "assessment"),
               ("c14_templates", "templates"), ("c15_appendix", "appendix")],
        web=True),
    "training": dict(
        file="VEL-QA-90-Day-Training-Plan", id="VEL-QA-002", short="90-Day Employee Training Plan",
        kicker="QA Employee Programme", title="90-Day Employee<br>Training Plan",
        subtitle="Day-by-day onboarding and learning schedule for a beginner QA employee, with weekly "
                 "deliverables, review gates and evidence checklists",
        parts=[("c12_projects_plan", "training_plan"), ("c02_onboarding", "onboarding"),
               ("c02_onboarding", "first_assignment")]),
    "templates": dict(
        file="VEL-QA-Templates-Pack", id="VEL-QA-003", short="QA Templates Pack",
        kicker="Controlled Templates", title="QA Templates &amp; Sign-off<br>Checklist Pack",
        subtitle="Test strategy, test plan, test case, scenario, defect report, execution report, traceability, "
                 "automation coverage, performance, UAT and release sign-off",
        parts=[("c14_templates", "templates")]),
    "workbook": dict(
        file="VEL-QA-Practical-Exercise-Workbook", id="VEL-QA-004", short="Practical Exercise Workbook",
        kicker="Hands-on Training", title="Practical Exercise<br>Workbook",
        subtitle="First manual-testing assignment plus eight practical employee projects with briefs, "
                 "evidence checklists, acceptance criteria and marking rubrics",
        parts=[("c02_onboarding", "first_assignment"), ("c12_projects_plan", "projects")]),
    "assessment": dict(
        file="VEL-QA-Assessment-Competency-Framework", id="VEL-QA-005", short="Assessment & Competency Framework",
        kicker="Skills Assurance", title="Assessment &amp; Competency<br>Framework",
        subtitle="Competency matrix, level gates, marking rubrics, question banks with model answers and "
                 "promotion criteria",
        parts=[("c13_assessment", "assessment")]),
}


def b64(path: pathlib.Path) -> str:
    return base64.b64encode(path.read_bytes()).decode()


def favicon() -> str:
    from PIL import Image
    im = Image.open(HERE / "assets" / "logo.png").convert("RGBA")
    side = max(im.size)
    sq = Image.new("RGBA", (side, side), (255, 255, 255, 255))
    sq.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
    buf = io.BytesIO()
    sq.resize((64, 64), Image.LANCZOS).save(buf, "PNG", optimize=True)
    return base64.b64encode(buf.getvalue()).decode()


def cover(d: dict, logo: str) -> str:
    return f"""<section class="cover" id="top">
<img class="logo" src="data:image/png;base64,{logo}" alt="VEL Software Solutions logo" width="613" height="630">
<div class="kicker">{d['kicker']}</div>
<h1 class="title">{d['title']}</h1>
<p class="subtitle">{d['subtitle']}</p>
<p class="by">Issued by <b>{COMPANY}</b></p>
<p class="by">{EMAIL}</p>
<div class="docmeta">Document {d['id']} · Version {VERSION} (draft for management approval) · Issued {ISSUED}<br>
Tool versions, licences, limits and prices change. Always verify against official documentation
and vendor pricing before training, building or quoting. Confidential · internal use.</div>
</section>"""


def toc_html(body: str) -> str:
    items, cur = [], None
    for m in re.finditer(r'<h([12]) id="([^"]+)">(.*?)</h\1>', body, re.S):
        lvl, i, t = m.group(1), m.group(2), re.sub(r"<[^>]+>", "", m.group(3)).strip()
        if lvl == "1":
            if i in ("contents",):
                continue
            cur = [i, t, []]
            items.append(cur)
        elif cur is not None:
            cur[2].append((i, t))
    out = []
    for i, t, _subs in items:
        label = html.escape(re.sub(r"^\d+\.\s*", "", t))
        out.append(f'<li><a href="#{i}">{label}</a></li>')
    return '<div class="toc"><h2 id="contents-list">Contents</h2><ol>' + "".join(out) + "</ol></div>"


def nav_html(body: str) -> str:
    out, open_li = [], False
    for m in re.finditer(r'<h([12]) id="([^"]+)">(.*?)</h\1>', body, re.S):
        lvl, i, raw = m.group(1), m.group(2), m.group(3)
        num = re.search(r'<span class="num">([^<]+)</span>', raw)
        t = html.escape(re.sub(r"<[^>]+>", "", raw).strip())
        t = re.sub(r"^\d+\.\s*", "", t)
        if lvl == "1":
            if open_li:
                out.append("</ul></li>" if out[-1] != "<li>" else "</li>")
            n = f'<span class="n">{num.group(1)}</span>' if num else ""
            out.append(f'<li><a href="#{i}">{n}{t}</a><ul>')
            open_li = True
        elif open_li:
            out.append(f'<li><a href="#{i}">{t}</a></li>')
    if open_li:
        out.append("</ul></li>")
    return '<ul class="navlist" id="navlist">' + "".join(out) + "</ul>"


def page_css(d: dict) -> str:
    f = "font-family:Inter,sans-serif;font-size:7pt;color:#56606f;"
    return (
        "@media print{@page{"
        f'@top-left{{content:"{COMPANY} · {d["short"]} ({d["id"]} v{VERSION})";{f}vertical-align:top;padding-top:7mm;width:72%;text-align:left}}'
        f'@top-right{{content:"Confidential · internal use";{f}vertical-align:top;padding-top:7mm;width:28%;text-align:right}}'
        f'@bottom-left{{content:"{COMPANY} · QA Department · {EMAIL}";{f}vertical-align:bottom;padding-bottom:7mm;width:72%;text-align:left}}'
        f'@bottom-right{{content:"Page " counter(page) " of " counter(pages);{f}vertical-align:bottom;padding-bottom:7mm;width:28%;text-align:right}}'
        "}}"
    )


def load_parts(parts) -> str:
    out = []
    for mod, fn in parts:
        m = importlib.import_module(mod)
        out.append(getattr(m, fn)())
    return "".join(out)


def assemble(name: str, web: bool) -> str:
    d = DOCS[name]
    lib.reset()
    body = load_parts(d["parts"])
    toc = toc_html(body)
    n_chapters = len(re.findall(r"<h1 id=", body))
    if "<!--TOC-->" in body:
        body = body.replace("<!--TOC-->", toc)
    elif n_chapters >= 5:                       # short standalone documents do not need a separate contents page
        body = toc_page(toc) + body
    logo = b64(HERE / "assets" / "logo.png")
    wm = b64(HERE / "assets" / "watermark.png")
    css = (HERE / "assets" / "fonts.css").read_text() + (HERE / "theme.css").read_text() + page_css(d)
    if web:
        css += (HERE / "web.css").read_text()
    shell_open = shell_close = ""
    js = ""
    if web:
        shell_open = f"""<a class="skip" href="#main">Skip to content</a><div class="progress" aria-hidden="true"></div>
<header class="topbar noprint"><button class="btn" data-act="menu" aria-label="Open navigation">☰</button><b>{d['short']}</b>
<button class="btn" data-act="theme" aria-label="Toggle dark mode">Theme</button></header>
<div class="shell"><aside class="sidebar noprint" aria-label="Navigation">
<a class="brand" href="#top"><img src="data:image/png;base64,{logo}" alt="" width="46" height="47"><div><b>VEL QA</b><span>Department Handbook</span></div></a>
<div class="search"><input id="q" type="search" placeholder="Search handbook…" aria-label="Search handbook" autocomplete="off"><kbd>/</kbd></div>
<ul class="results" id="results" aria-live="polite"></ul>
{nav_html(body)}
<div class="tools"><button class="btn" data-act="theme">Theme</button><button class="btn" data-act="print">⎙ Print / PDF</button></div>
</aside><main class="main" id="main"><div class="doc">"""
        shell_close = '</div></main></div><button class="fab noprint" aria-label="Back to top">↑</button>'
        js = "<script>" + (HERE / "web.js").read_text() + "</script>"
    # first chapter should not force a blank page after the cover
    body = body.replace('<section class="chapter">', '<section class="chapter first">', 1)
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{html.escape(re.sub('<[^>]+>', ' ', d['title']).replace('&amp;','&'))} — {COMPANY}</title>
<meta name="description" content="{html.escape(re.sub('<[^>]+>', ' ', d['subtitle']))}">
<meta name="theme-color" content="#14306b"><link rel="icon" href="data:image/png;base64,{favicon()}">
<style>{css}</style></head><body>
<img class="wm" src="data:image/png;base64,{wm}" alt="" aria-hidden="true">
{shell_open}{cover(d, logo)}{body}{shell_close}{js}</body></html>"""


def toc_page(toc: str) -> str:
    return f'<section class="chapter first"><h1 id="contents">Contents</h1>{toc}</section>'


def check_links(doc: str, name: str) -> list[str]:
    ids = set(re.findall(r'\sid="([^"]+)"', doc))
    bad = sorted({h for h in re.findall(r'href="#([A-Za-z0-9_-]+)"', doc) if h not in ids})
    dup = [i for i in ids if len(re.findall(rf'\sid="{re.escape(i)}"', doc)) > 1]
    msgs = [f"[{name}] broken anchor: #{b}" for b in bad] + [f"[{name}] duplicate id: {i}" for i in dup]
    return msgs


def main() -> int:
    only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
    DIST.mkdir(exist_ok=True)
    import make_template
    print(f"== templates: {make_template.make()} files regenerated in qa-project-template/")
    problems = []
    for name, d in DOCS.items():
        if only and name != only:
            continue
        print(f"== {name}")
        pdf_html = assemble(name, web=False)
        problems += check_links(pdf_html, name)
        tmp = DIST / f".{d['file']}.print.html"
        tmp.write_text(pdf_html, encoding="utf-8")
        out = DIST / f"{d['file']}.pdf"
        subprocess.run(["node", str(HERE / "render_pdf.js"), str(tmp), str(out), d["title"].replace("<br>", " ").replace("&amp;", "&") + " — " + COMPANY], check=True)
        tmp.unlink()
        print(f"   PDF  {out.name}  {out.stat().st_size // 1024} KB")
        if d.get("web"):
            w = assemble(name, web=True)
            problems += check_links(w, name + "-web")
            hp = DIST / f"{d['file']}.html"
            hp.write_text(w, encoding="utf-8")
            print(f"   HTML {hp.name}  {hp.stat().st_size // 1024} KB")
    for m in problems:
        print("PROBLEM:", m)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
