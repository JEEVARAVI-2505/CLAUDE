"""Tiny HTML-building DSL shared by every content module.

All helpers return HTML strings.  Inline markup supported in text arguments:
  **bold**   `code`   [label](#anchor-or-url)
Everything else is HTML-escaped, so content authors cannot break the page.
"""
from __future__ import annotations

import html
import re

_state = {"n": 0, "ids": set(), "chapter": ""}


def reset() -> None:
    """Reset chapter numbering and id registry (called once per document build)."""
    _state["n"] = 0
    _state["ids"] = set()
    _state["chapter"] = ""


def _uid(base: str) -> str:
    """Return a document-unique id: reuse `base`, else prefix with the chapter id, else add a counter."""
    cand = base
    if cand in _state["ids"]:
        cand = f"{_state['chapter']}-{base}"
    n = 2
    while cand in _state["ids"]:
        cand = f"{base}-{n}"
        n += 1
    _state["ids"].add(cand)
    return cand


def slug(text: str) -> str:
    s = re.sub(r"[^a-z0-9]+", "-", re.sub(r"<[^>]+>", "", text).lower()).strip("-")
    return s or "section"


def inline(text: str) -> str:
    t = html.escape(str(text), quote=False)
    t = re.sub(r"`([^`]+)`", r"<code>\1</code>", t)
    t = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", t)
    t = re.sub(r"(?<![\w*])\*([^*\s][^*]*?)\*(?![\w*])", r"<em>\1</em>", t)
    t = re.sub(
        r"\[([^\]]+)\]\(([^)\s]+)\)",
        lambda m: f'<a href="{m.group(2)}"'
        + ("" if m.group(2).startswith("#") else ' target="_blank" rel="noopener"')
        + f">{m.group(1)}</a>",
        t,
    )
    return t


# ---------------------------------------------------------------- headings
def h1(title: str, intro: str = "", numbered: bool = True, anchor: str | None = None) -> str:
    """Chapter heading: starts a new page, numbered in magenta, magenta rule."""
    aid = _uid(anchor or slug(title))
    _state["chapter"] = aid
    if numbered:
        _state["n"] += 1
        num = f'<span class="num">{_state["n"]}.</span> '
    else:
        num = ""
    lead = f'<p class="lead">{inline(intro)}</p>' if intro else ""
    return f'<section class="chapter"><h1 id="{aid}">{num}{inline(title)}</h1>{lead}'


def end_chapter() -> str:
    return "</section>"


def h2(title: str, anchor: str | None = None) -> str:
    return f'<h2 id="{_uid(anchor or slug(title))}">{inline(title)}</h2>'


def h3(title: str) -> str:
    return f"<h3>{inline(title)}</h3>"


# ---------------------------------------------------------------- text blocks
def p(text: str, cls: str = "") -> str:
    return f'<p class="{cls}">{inline(text)}</p>' if cls else f"<p>{inline(text)}</p>"


def ul(items) -> str:
    return "<ul>" + "".join(f"<li>{inline(i)}</li>" for i in items) + "</ul>"


def ol(items) -> str:
    return "<ol>" + "".join(f"<li>{inline(i)}</li>" for i in items) + "</ol>"


def code(text: str, title: str = "") -> str:
    cap = f'<div class="code-title">{html.escape(title)}</div>' if title else ""
    return f'<div class="codewrap">{cap}<pre><code>{html.escape(text.strip(chr(10)))}</code></pre></div>'


def callout(kind: str, title: str, body) -> str:
    """kind: gold | danger | info | ok.  body: str (inline markup) or list -> bullet list."""
    if isinstance(body, (list, tuple)):
        inner = ol(body) if kind == "gold" and title.upper().startswith("GOLDEN") else ul(body)
    else:
        inner = f"<p>{inline(body)}</p>"
    return f'<div class="callout {kind}"><div class="callout-title">{html.escape(title)}</div>{inner}</div>'


def pills(labels) -> str:
    items = [f'<span class="pill">{inline(x)}</span>' for x in labels]
    return '<div class="pills">' + '<span class="arr">→</span>'.join(items) + "</div>"


def table(headers, rows, widths=None, cls: str = "", first_bold: bool = False) -> str:
    cg = ""
    if widths:
        cg = "<colgroup>" + "".join(f'<col style="width:{w}">' for w in widths) + "</colgroup>"
    th = "".join(f"<th>{inline(h)}</th>" for h in headers)
    body = []
    for r in rows:
        tds = []
        for i, c in enumerate(r):
            txt = inline(c).replace("\n", "<br>")
            if first_bold and i == 0:
                txt = f"<strong>{txt}</strong>"
            tds.append(f"<td>{txt}</td>")
        body.append("<tr>" + "".join(tds) + "</tr>")
    keep = " keep" if len(rows) <= 6 else ""
    return (
        f'<div class="tablewrap{keep}"><table class="{cls}">{cg}<thead><tr>{th}</tr></thead>'
        f'<tbody>{"".join(body)}</tbody></table></div>'
    )


def kv(rows, cls: str = "kv", keep: bool = False) -> str:
    """Two-column definition table. Value may be str, or list (rendered as bullets), or ('ol', list)."""
    out = []
    for k, v in rows:
        if isinstance(v, tuple) and v[0] == "ol":
            val = ol(v[1])
        elif isinstance(v, (list,)):
            val = ul(v)
        else:
            val = inline(v).replace("\n", "<br>")
        out.append(f"<tr><th>{inline(k)}</th><td>{val}</td></tr>")
    k = " keep" if (keep or len(rows) <= 5) else ""
    return f'<div class="tablewrap{k}"><table class="{cls}"><tbody>{"".join(out)}</tbody></table></div>'


def topic(name: str, rows) -> str:
    """Ten-part teaching card used for every learning topic (see 'Teaching method')."""
    return f'<div class="topic"><div class="topic-name">{inline(name)}</div>{kv(rows, "kv topic-kv")}</div>'


def checklist(items) -> str:
    return '<ul class="checklist">' + "".join(f"<li>{inline(i)}</li>" for i in items) + "</ul>"


def spacer() -> str:
    return '<div class="sp"></div>'


def pagebreak() -> str:
    return '<div class="pb"></div>'


def join(*parts) -> str:
    return "".join(parts)


TEN = ["Definition", "Purpose", "Setup", "Procedure", "Expected output", "Verification",
       "Errors", "Reporting", "Assignment", "Assessment"]


def card10(name: str, *vals: str) -> str:
    """Ten-part teaching card. vals must supply exactly the ten parts in TEN order."""
    assert len(vals) == 10, f"{name}: need 10 parts, got {len(vals)}"
    return topic(name, list(zip(TEN, vals)))


def level_header(duration: str, prereq: str, outcome: str, gate: str) -> str:
    return kv([["Duration", duration], ["Prerequisites", prereq], ["Outcome", outcome], ["Exit gate", gate]])


import pathlib as _pl

TEMPLATE_ROOT = _pl.Path(__file__).resolve().parent.parent / "qa-project-template"


def file_code(rel: str, title: str | None = None) -> str:
    """Embed a real file from qa-project-template/ as a code block, so handbook code can never drift from the repository."""
    return code((TEMPLATE_ROOT / rel).read_text(encoding="utf-8"), title or rel)
