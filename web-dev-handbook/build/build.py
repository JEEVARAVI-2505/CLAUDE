#!/usr/bin/env python3
"""VEL handbook build: Markdown/YAML -> print HTML -> PDF (Chromium) + standalone web HTML.

Usage:  python3 build/build.py [handbook|support|all]   (default: all)
Two-pass PDF render: pass 1 measures where every heading lands, pass 2 fills the table-of-contents page numbers.
"""
from __future__ import annotations
import html, json, re, subprocess, sys
from pathlib import Path
import yaml
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from lib import ROOT, CONTENT, Renderer, logo_data_uri, font_face_css  # noqa: E402
from diagrams import DIAGRAMS  # noqa: E402

DIST = ROOT / "dist"
B = ROOT / "build"
MANIFEST = yaml.safe_load((CONTENT / "manifest.yaml").read_text(encoding="utf-8"))
CO = MANIFEST["company"]


def head_css() -> str:
    return (f"<style>{font_face_css()}\n:root{{--logo:url({logo_data_uri()})}}\n{(B / 'style.css').read_text()}\n"
            f"{(B / 'web.css').read_text()}</style>")


def cover(doc: dict) -> str:
    return (f'<div class="cover"><div class="logo" role="img" aria-label="VEL Software Solutions logo"></div>'
            f'<div class="kicker">Company Operating System</div><h1>{html.escape(doc["title"])}</h1>'
            f'<div class="sub">{html.escape(doc["subtitle"])}</div>'
            f'<div class="by">Developed by <b>{html.escape(CO["author"])}</b><br>{html.escape(CO["name"])}<br>{html.escape(CO["email"])}</div>'
            f'<div class="meta">Document {html.escape(doc["doc_no"])} · Version {html.escape(CO["version"])} ({html.escape(CO["status"])})<br>'
            f'{html.escape(CO["disclaimer"])}</div></div>')


def build_sections(doc: dict, secs: list[dict]):
    r = Renderer(DIAGRAMS)
    out, toc = [], []  # toc: (level, id, text, part)
    for i, s in enumerate(secs):
        r.toc = []
        body = r.md_to_html((CONTENT / s["file"]).read_text(encoding="utf-8"))
        sid = f"sec-{s['num'].lower()}" if s.get("num") else f"sec-{Path(s['file']).stem}"
        num = f'<span class="num">{html.escape(s["num"])}.</span> ' if s.get("num") else ""
        ptxt = f'Part {s["part"]} · {MANIFEST["parts"][s["part"]]}' if s.get("part") else ""
        meta = f'<p class="metaline">{html.escape(s["meta"])}</p>' if s.get("meta") else ""
        out.append(f'<section class="sec{" first" if i == 0 else ""}" id="{sid}"><div class="partbanner">{html.escape(ptxt)}</div>'
                   f'<h1 class="sec">{num}{html.escape(s["title"])}</h1>{meta}{body}</section>')
        label = f'{s["num"]}. {s["title"]}' if s.get("num") else s["title"]
        toc.append((1, sid, label, ptxt))
        toc += [(lvl, hid, t, ptxt) for lvl, hid, t in r.toc]
    return "\n".join(out), toc


def toc_html(toc, pages: dict | None) -> str:
    items, last_part = [], None
    for lvl, hid, text, part in toc:
        if lvl == 2 and not text.startswith(("Stage ", "Level ")):
            continue
        if lvl == 1 and part != last_part:
            items.append(f'<li class="part">{html.escape(part)}</li>')
            last_part = part
        pg = pages.get(hid, "00") if pages else "00"
        items.append(f'<li class="l{lvl}"><a href="#{hid}"><span class="t">{html.escape(text)}</span><span class="dots"></span>'
                     f'<span class="pg">{pg}</span></a></li>')
    return f'<section class="sec" id="contents"><h1 class="sec">Table of Contents</h1><ul class="toc">{"".join(items)}</ul></section>'


def print_html(doc, body, toc, pages=None) -> str:
    return (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><title>{html.escape(doc["title"])}</title>{head_css()}</head>'
            f'<body class="print"><div class="wm"></div>{cover(doc)}{toc_html(toc, pages)}{body}</body></html>')


def web_html(doc, body, toc) -> str:
    nav, grp_open, last_part = [], False, None
    for lvl, hid, text, part in toc:
        if lvl == 1:
            if grp_open:
                nav.append("</div></div>")
            if part != last_part:
                nav.append(f'<div class="navpart">{html.escape(part)}</div>')
                last_part = part
            nav.append(f'<div class="grp"><a class="l1" href="#{hid}">{html.escape(text)}</a><div class="sub">')
            grp_open = True
        else:
            nav.append(f'<a class="l{lvl}" href="#{hid}">{html.escape(text)}</a>')
    nav.append("</div></div>")
    return (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<title>{html.escape(doc["title"])} — {html.escape(CO["name"])}</title>{head_css()}</head><body>'
            f'<div class="wm no-print"></div><div class="layout"><nav class="sidebar no-print" aria-label="Handbook navigation">'
            f'<div class="brand"><div class="logo" role="img" aria-label="VEL logo"></div><div><b>VEL</b><small>{html.escape(doc["short"])}<br>{html.escape(doc["doc_no"])} v{CO["version"]}</small></div></div>'
            f'{"".join(nav)}</nav><div class="main"><div class="topbar no-print"><button id="menu" aria-label="Menu">☰</button>'
            f'<input id="q" type="search" placeholder="Search the handbook…  ( / )" aria-label="Search"><button id="expand">Expand all</button>'
            f'<button id="collapse">Collapse all</button><button onclick="window.print()">Print</button><span id="progress" style="font-size:8.5pt;color:#5b6577"></span></div>'
            f'<div id="results" class="no-print"></div><main class="page">{cover(doc)}{body}'
            f'<p class="metaline" style="margin-top:30px">© {html.escape(CO["name"])} · {html.escape(CO["disclaimer"])}</p></main></div></div>'
            f'<script>{(B / "web.js").read_text()}</script></body></html>')


def header_footer(doc) -> dict:
    im = Image.open(ROOT / "assets" / "vel-logo.png").convert("RGB")
    im = im.resize((150, 101), Image.LANCZOS)
    tmp = B / "_hdr_logo.png"
    im.save(tmp, optimize=True)
    import base64
    b64 = base64.b64encode(tmp.read_bytes()).decode()
    tmp.unlink()
    st = "width:100%;font-family:Inter,Arial,sans-serif;font-size:7.4px;color:#5b6577;padding:0 15mm;display:flex;justify-content:space-between;align-items:center;"
    return {
        "header": f'<div style="{st}margin-top:-2mm"><span style="display:flex;align-items:center;gap:6px"><img src="data:image/png;base64,{b64}" style="height:19px">'
                  f'{html.escape(CO["name"])} · {html.escape(doc["short"])} ({html.escape(doc["doc_no"])} v{CO["version"]})</span><span>Confidential · internal use</span></div>',
        "footer": f'<div style="{st}"><span>Developed by {html.escape(CO["author"])} · {html.escape(CO["email"])}</span>'
                  f'<span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>',
    }


def render_pdf(html_path: Path, pdf_path: Path, doc: dict):
    cfg = header_footer(doc)
    cfg_path = html_path.with_suffix(".cfg.json")
    cfg_path.write_text(json.dumps(cfg))
    subprocess.run(["node", str(B / "render.js"), str(html_path), str(pdf_path), str(cfg_path), doc["title"]], check=True)
    cfg_path.unlink()


def norm(s: str) -> str:
    return re.sub(r"[^a-z0-9]", "", s.lower())


def locate_pages(pdf: Path, toc, total: int) -> dict:
    pages_txt = [subprocess.run(["pdftotext", "-layout", "-f", str(i), "-l", str(i), str(pdf), "-"], capture_output=True, text=True).stdout
                 for i in range(1, total + 1)]
    found, cur, first = {}, 1, True
    for lvl, hid, text, _ in toc:
        target = norm(text)
        for pi in range(cur, total):
            lines = [norm(l) for l in pages_txt[pi].splitlines() if l.strip()]
            exact = any(l == target for l in lines)
            loose = (not first) and any(l.startswith(target[:40]) and len(target) >= 6 for l in lines)
            if exact or loose:
                found[hid], cur, first = str(pi + 1), pi, False
                break
    return found


def pdf_pages(pdf: Path) -> int:
    out = subprocess.run(["pdfinfo", str(pdf)], capture_output=True, text=True).stdout
    return int(re.search(r"Pages:\s+(\d+)", out).group(1))


def build_one(doc: dict, secs: list[dict], pdf_name: str, html_name: str | None, tmp_dir: Path):
    body, toc = build_sections(doc, secs)
    tmp_dir.mkdir(parents=True, exist_ok=True)
    hp, pdf = tmp_dir / f"{pdf_name}.print.html", DIST / doc.get("out_dir", "") / f"{pdf_name}.pdf"
    pdf.parent.mkdir(parents=True, exist_ok=True)
    hp.write_text(print_html(doc, body, toc), encoding="utf-8")
    render_pdf(hp, pdf, doc)
    pages = locate_pages(pdf, toc, pdf_pages(pdf))
    missing = [t for _, h, t, _ in toc if h not in pages]
    hp.write_text(print_html(doc, body, toc, pages), encoding="utf-8")
    render_pdf(hp, pdf, doc)
    print(f"  PDF {pdf.relative_to(ROOT)}: {pdf_pages(pdf)} pages; TOC entries unresolved: {len(missing)} {missing[:5]}")
    hp.unlink()
    if html_name:
        out = DIST / doc.get("out_dir", "") / f"{html_name}.html"
        out.write_text(web_html(doc, body, toc), encoding="utf-8")
        print(f"  HTML {out.relative_to(ROOT)}: {out.stat().st_size // 1024} KB")


def main(which: str):
    tmp = ROOT / "build" / "_tmp"
    secs = {s["file"]: s for s in MANIFEST["sections"]}
    if which in ("handbook", "all"):
        print("Handbook")
        build_one(MANIFEST["handbook"], MANIFEST["sections"], "VEL-Web-Development-Handbook", "VEL-Web-Development-Handbook", tmp)
    if which in ("support", "all"):
        print("Supporting materials")
        kit = MANIFEST["support"]
        for item in kit["docs"]:
            doc = {**kit["defaults"], "title": item["title"], "subtitle": item["subtitle"], "doc_no": item["doc_no"],
                   "short": item["title"], "out_dir": "supporting-materials"}
            sec = {"file": item["file"], "num": "", "title": item["title"], "part": "", "meta": item.get("meta", "")}
            build_one(doc, [sec], item["slug"], None, tmp)
        kit_doc = {**kit["defaults"], "title": "Supporting Materials Kit", "subtitle": kit["subtitle"], "doc_no": kit["doc_no"],
                   "short": "Supporting Materials Kit", "out_dir": "supporting-materials"}
        ksecs = [{"file": d["file"], "num": d["num"], "title": d["title"], "part": "S", "meta": d.get("meta", "")} for d in kit["docs"]]
        body, toc = build_sections(kit_doc, ksecs)
        out = DIST / "supporting-materials" / "VEL-Supporting-Materials-Kit.html"
        out.write_text(web_html(kit_doc, body, toc), encoding="utf-8")
        print(f"  HTML {out.relative_to(ROOT)}: {out.stat().st_size // 1024} KB")
    if tmp.exists() and not any(tmp.iterdir()):
        tmp.rmdir()


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "all")
