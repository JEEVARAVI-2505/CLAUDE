"""Tiny document DSL used by every handbook module.

A Chapter collects HTML parts; the Doc collects chapters and builds a TOC.
Inline markup supported in text: **bold**, `code`, [label](url).
"""
import html
import re

_ID_SEEN: dict[str, int] = {}


def slug(text: str) -> str:
    base = re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-") or "x"
    n = _ID_SEEN.get(base, 0)
    _ID_SEEN[base] = n + 1
    return base if n == 0 else f"{base}-{n}"


def inl(s: str) -> str:
    s = html.escape(str(s), quote=False)
    codes: list[str] = []

    def stash(m):
        codes.append(f"<code>{m.group(1)}</code>")
        return f"\x00{len(codes) - 1}\x00"
    s = re.sub(r"`([^`]+)`", stash, s)
    s = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", s)
    s = re.sub(r"\[([^\]]+)\]\((https?://[^)\s]+)\)",
               r'<a href="\2" target="_blank" rel="noopener">\1</a>', s)
    s = re.sub(r"(?<![\w*])\*([^*\s][^*]*?)\*(?![\w*])", r"<em>\1</em>", s)
    s = re.sub(r"\x00(\d+)\x00", lambda m: codes[int(m.group(1))], s)
    return s.replace("\n", "<br>")


class Chapter:
    def __init__(self, num: str, title: str, intro: str | None = None,
                 kind: str = "chapter", search: str = ""):
        self.num, self.title, self.kind = num, title, kind
        self.id = slug(f"{num}-{title}" if num else title)
        self.parts: list[str] = []
        self.subs: list[tuple[str, str]] = []
        self.h3s: list[tuple[str, str]] = []
        self.search = search
        if intro:
            self.p(intro, cls="lead")

    # ---- structure
    def h2(self, text: str):
        i = slug(text)
        self.subs.append((i, text))
        self.parts.append(f'<h2 id="{i}">{inl(text)}</h2>')

    def h3(self, text: str, toc: bool = False):
        if toc:
            i = slug(text)
            self.h3s.append((i, text))
            self.parts.append(f'<h3 id="{i}">{inl(text)}</h3>')
        else:
            self.parts.append(f"<h3>{inl(text)}</h3>")

    def raw(self, h: str):
        self.parts.append(h)

    def p(self, text: str, cls: str = ""):
        c = f' class="{cls}"' if cls else ""
        self.parts.append(f"<p{c}>{inl(text)}</p>")

    def ul(self, items):
        self.parts.append("<ul>" + "".join(f"<li>{inl(i)}</li>" for i in items) + "</ul>")

    def ol(self, items):
        self.parts.append("<ol>" + "".join(f"<li>{inl(i)}</li>" for i in items) + "</ol>")

    def check(self, items):
        self.parts.append('<ul class="check">' + "".join(
            f"<li><span class='box'></span><span>{inl(i)}</span></li>" for i in items) + "</ul>")

    def code(self, text: str, lang: str = ""):
        lab = f'<span class="lang">{html.escape(lang)}</span>' if lang else ""
        self.parts.append(f'<div class="codewrap">{lab}<pre><code>{html.escape(text.strip(chr(10)))}</code></pre></div>')

    def callout(self, kind: str, title: str, body):
        if isinstance(body, (list, tuple)):
            b = "<ol>" + "".join(f"<li>{inl(x)}</li>" for x in body) + "</ol>"
        else:
            b = f"<p>{inl(body)}</p>"
        self.parts.append(f'<div class="callout {kind}"><div class="ct">{inl(title)}</div>{b}</div>')

    def table(self, head, rows, cls: str = "", widths=None):
        cg = ""
        if widths:
            cg = "<colgroup>" + "".join(f'<col style="width:{w}%">' for w in widths) + "</colgroup>"
        th = "".join(f"<th>{inl(h)}</th>" for h in head)
        body = "".join("<tr>" + "".join(f"<td>{inl(c)}</td>" for c in r) + "</tr>" for r in rows)
        self.parts.append(f'<table class="{cls}">{cg}<thead><tr>{th}</tr></thead><tbody>{body}</tbody></table>')

    def kv(self, pairs, cls: str = ""):
        """Two-column key/value card (used for stage cards)."""
        body = "".join(f"<tr><th>{inl(k)}</th><td>{inl(v)}</td></tr>" for k, v in pairs)
        self.parts.append(f'<table class="kv {cls}"><tbody>{body}</tbody></table>')

    def pills(self, items):
        self.parts.append('<div class="pills">' + '<span class="arrow">→</span>'.join(
            f"<span class='pill'>{inl(i)}</span>" for i in items) + "</div>")

    def pagebreak(self):
        self.parts.append('<div class="pb"></div>')

    # ---- output
    def html(self) -> str:
        num = f'<span class="num">{html.escape(self.num)}.</span> ' if self.num and self.num[0].isdigit() else (
            f'<span class="num">{html.escape(self.num)} —</span> ' if self.num else "")
        return (f'<section class="chapter {self.kind}" id="{self.id}" data-title="{html.escape(self.title)}">'
                f"<h1>{num}{inl(self.title)}</h1>" + "".join(self.parts) + "</section>")

    def text(self) -> str:
        return re.sub(r"<[^>]+>", " ", " ".join(self.parts))
