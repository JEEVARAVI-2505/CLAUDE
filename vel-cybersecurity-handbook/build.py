#!/usr/bin/env python3
"""Build the VEL Cybersecurity Department Handbook (HTML + PDF).

Usage: python3 -I build.py
Pass 1 renders the PDF, pdftotext finds the page of each heading, pass 2
re-renders with page numbers in the contents list.
"""
import base64, glob, html, json, os, re, subprocess, sys

ROOT = os.path.dirname(os.path.abspath(__file__))
DIST = os.path.join(ROOT, "dist")
DOC_ID, VERSION = "VEL-CS-001", "1.0"
TITLE = "Cybersecurity Department Handbook"
OUT = "VEL-Cybersecurity-Department-Handbook"


def b64(path):
    with open(os.path.join(ROOT, "assets", path), "rb") as f:
        return "data:image/png;base64," + base64.b64encode(f.read()).decode()


def load_parts():
    parts = []
    for p in sorted(glob.glob(os.path.join(ROOT, "src", "parts", "*.html"))):
        with open(p, encoding="utf-8") as f:
            parts.append(f.read())
    return "\n".join(parts)


def add_ids(body):
    """Give every h1/h2 an id and return (body, headings)."""
    heads, n = [], [0]

    def rep(m):
        n[0] += 1
        lvl, attrs, text = m.group(1), m.group(2), m.group(3)
        hid = f"h{n[0]}"
        plain = html.unescape(re.sub(r"<[^>]+>", "", text)).strip()
        heads.append((int(lvl), hid, plain))
        return f'<h{lvl}{attrs} id="{hid}">{text}</h{lvl}>'

    body = re.sub(r"<h([12])([^>]*)>(.*?)</h\1>", rep, body, flags=re.S)
    return body, heads


def toc_html(heads, pages):
    out = ['<ul class="toc">']
    for lvl, hid, text in heads:
        if text.lower() == "contents":
            continue
        pg = pages.get(hid, "")
        cls = "p" if lvl == 1 else "s"
        out.append(
            f'<li class="{cls}"><a href="#{hid}">{html.escape(text)}</a>'
            f'<span class="dots"></span><span class="pg">{pg}</span></li>'
        )
    out.append("</ul>")
    return "\n".join(out)


def side_nav(heads, logo):
    items = [f'<img src="{logo}" alt="VEL Software Solutions">']
    items.append('<div class="h">Navigate</div>')
    for lvl, hid, text in heads:
        if lvl == 1 and text.lower() != "contents":
            items.append(f'<a href="#{hid}">{html.escape(text)}</a>')
    return '<nav class="side">' + "\n".join(items) + "</nav>"


def assemble(pages):
    css = open(os.path.join(ROOT, "src", "style.css"), encoding="utf-8").read()
    body = load_parts()
    body, heads = add_ids(body)
    logo, wm = b64("vel-logo.png"), b64("vel-logo-watermark.png")
    body = body.replace("{{TOC}}", toc_html(heads, pages))
    body = body.replace("{{LOGO}}", logo).replace("{{DOCID}}", DOC_ID)
    doc = f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>VEL Software Solutions - {TITLE} ({DOC_ID} v{VERSION})</title>
<style>{css}</style></head>
<body>
<img class="wm" src="{wm}" alt="">
{side_nav(heads, logo)}
<main>
{body}
</main>
</body></html>"""
    return doc, heads


def render(html_path, pdf_path):
    subprocess.run(["node", os.path.join(ROOT, "render.js"), html_path, pdf_path,
                    f"{DOC_ID} v{VERSION}"], check=True)


def find_pages(pdf_path, heads):
    n = int(re.search(r"Pages:\s+(\d+)", subprocess.run(
        ["pdfinfo", pdf_path], capture_output=True, text=True).stdout).group(1))
    texts = []
    for i in range(1, n + 1):
        t = subprocess.run(["pdftotext", "-f", str(i), "-l", str(i), "-layout", pdf_path, "-"],
                           capture_output=True, text=True).stdout
        texts.append(re.sub(r"\s+", " ", t))
    pages, start = {}, 3  # skip cover + contents pages in search
    toc_end = 3
    for i, t in enumerate(texts, 1):
        if "Contents" in t[:200] and i <= 6:
            toc_end = i + 0
    start = toc_end  # first page after the last contents page
    cur = start
    for lvl, hid, text in heads:
        if text.lower() == "contents":
            continue
        key = re.sub(r"\s+", " ", text)
        for i in range(cur, n):
            if key in texts[i]:
                pages[hid] = i + 1
                cur = i
                break
    return pages, n


def main():
    os.makedirs(DIST, exist_ok=True)
    html_path = os.path.join(DIST, OUT + ".html")
    pdf_path = os.path.join(DIST, OUT + ".pdf")
    pages = {}
    for attempt in range(3):
        doc, heads = assemble(pages)
        with open(html_path, "w", encoding="utf-8") as f:
            f.write(doc)
        render(html_path, pdf_path)
        new_pages, n = find_pages(pdf_path, heads)
        print(f"pass {attempt + 1}: {n} pages, {len(new_pages)}/{len(heads) - 1} headings located")
        if new_pages == pages:
            break
        pages = new_pages
    print("OK", html_path, pdf_path)


if __name__ == "__main__":
    sys.exit(main())
