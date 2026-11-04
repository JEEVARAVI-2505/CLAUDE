"""Markdown + YAML module rendering pipeline for the VEL handbook.

Content is authored as Markdown (sections) and YAML (teaching modules). The module schema enforces the
15-step teaching methodology: the build FAILS if any module omits a required field.
"""
from __future__ import annotations
import html, re, base64
from pathlib import Path
import markdown, yaml

ROOT = Path(__file__).resolve().parent.parent
CONTENT = ROOT / "content"

MODULE_FIELDS = ["definition", "purpose", "example", "prereq", "tools", "procedure", "code", "output",
                 "verify", "errors", "security", "docs", "assignment", "assessment", "gate"]
STEP_LABELS = {
    "definition": "1 · Definition", "purpose": "2 · Business purpose", "example": "3 · Real-world example",
    "prereq": "4 · Prerequisites", "tools": "5 · Tools &amp; installation",
    "verify": "9 · Verification", "errors": "10 · Common errors", "security": "11 · Security considerations",
    "docs": "12 · Documentation", "assignment": "13 · Practical assignment", "assessment": "14 · Assessment",
    "gate": "15 · Approval gate",
}
TAG_CLASS = {"Free": "free", "Paid": "paid", "Essential": "ess", "Optional": "opt"}


def slug(text: str, used: set[str]) -> str:
    s = re.sub(r"<[^>]+>", "", html.unescape(text)).lower()
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")[:60] or "x"
    base, n = s, 2
    while s in used:
        s, n = f"{base}-{n}", n + 1
    used.add(s)
    return s


def inline(text: str) -> str:
    """Render inline markdown (no wrapping <p>)."""
    out = markdown.markdown(str(text).strip(), extensions=["fenced_code", "tables"])
    return re.sub(r"^<p>(.*)</p>$", r"\1", out, flags=re.S)


class Renderer:
    def __init__(self, diagrams: dict):
        self.diagrams = diagrams
        self.used_ids: set[str] = set()
        self.toc: list[tuple[int, str, str]] = []  # (level, id, title)
        self.md = markdown.Markdown(extensions=["tables", "fenced_code", "admonition", "attr_list", "sane_lists"])

    # ---- markdown -------------------------------------------------------------------------------
    def md_to_html(self, text: str) -> str:
        macros: list[str] = []
        start = len(self.toc)

        def stash(html_: str) -> str:
            macros.append(html_)
            return f"\n\n@@MACRO{len(macros) - 1}@@\n\n"

        def flow(m):
            items = [i.strip() for i in m.group(1).split("|")]
            parts = '<span class="arr">&rarr;</span>'.join(f'<span class="pill">{html.escape(i)}</span>' for i in items)
            return stash(f'<div class="flow">{parts}</div>')

        def diagram(m):
            name, _, cap = (s.strip() for s in m.group(1).partition("|"))
            return stash(f'<figure class="diagram">{self.diagrams[name]()}<figcaption>{html.escape(cap)}</figcaption></figure>')

        def modules(m):
            return stash(self.render_modules(m.group(1).strip()))

        def costing(m):
            from costing import markdown as cost_md
            return stash(markdown.markdown(cost_md(m.group(1).strip()), extensions=["tables", "fenced_code"]))

        def stages(m):
            return stash(self.render_stages(m.group(1).strip()))

        text = re.sub(r"^\{\{flow:(.*?)\}\}\s*$", flow, text, flags=re.M)
        text = re.sub(r"^\{\{diagram:(.*?)\}\}\s*$", diagram, text, flags=re.M)
        text = re.sub(r"^\{\{modules:(.*?)\}\}\s*$", modules, text, flags=re.M)
        text = re.sub(r"^\{\{stages:(.*?)\}\}\s*$", stages, text, flags=re.M)
        text = re.sub(r"^\{\{costing:(.*?)\}\}\s*$", costing, text, flags=re.M)
        self.md.reset()
        out = self.md.convert(text)
        out = re.sub(r"<li>\[ \]\s*", '<li class="chk">', out)
        out = re.sub(r"<li>\[x\]\s*", '<li class="chk">', out)
        for i, h in enumerate(macros):
            out = out.replace(f"<p>@@MACRO{i}@@</p>", h)
        out = self._anchor_headings(out)
        self.toc[start:] = sorted(self.toc[start:], key=lambda e: out.find(f'id="{e[1]}"'))
        out = re.sub(r'<a href="(https?://[^"]+)"', r'<a target="_blank" rel="noopener" href="\1"', out)
        return out

    def _anchor_headings(self, out: str) -> str:
        def rep(m):
            lvl, attrs, body = int(m.group(1)), m.group(2), m.group(3)
            if "id=" in attrs:
                return m.group(0)
            hid = slug(body, self.used_ids)
            if lvl == 2:
                self.toc.append((2, hid, re.sub(r"<[^>]+>", "", body)))
            return f'<h{lvl} id="{hid}"{attrs}>{body}</h{lvl}>'
        return re.sub(r"<h([23])([^>]*)>(.*?)</h\1>", rep, out, flags=re.S)

    # ---- modules --------------------------------------------------------------------------------
    def render_modules(self, name: str) -> str:
        data = yaml.safe_load((CONTENT / "modules" / f"{name}.yaml").read_text(encoding="utf-8"))
        return "\n".join(self.render_module(m) for m in data["modules"])

    def render_module(self, m: dict) -> str:
        missing = [f for f in MODULE_FIELDS + ["id", "title", "time", "tags"] if not m.get(f)]
        if missing:
            raise SystemExit(f"Module {m.get('id', '?')} is missing required fields: {missing}")
        hid = slug(f"{m['id']} {m['title']}", self.used_ids)
        title = f"{m['id']} · {m['title']}"
        self.toc.append((3, hid, title))
        tags = "".join(f'<span class="tag {TAG_CLASS.get(t, "")}">{html.escape(t)}</span>' for t in m["tags"])
        tags += f'<span class="tag">{html.escape(str(m["time"]))}</span>'

        def row(k):
            return f"<tr><td>{STEP_LABELS[k]}</td><td>{inline(m[k])}</td></tr>"

        top = "".join(row(k) for k in ["definition", "purpose", "example", "prereq", "tools"])
        steps = "".join(f"<li>{inline(s)}</li>" for s in m["procedure"])
        code = m["code"]
        lang = html.escape(code.get("lang", ""))
        ex = f'<pre data-lang="{lang}"><code>{html.escape(code["text"].rstrip())}</code></pre>'
        out = m["output"]
        out_html = f'<pre data-lang="output"><code>{html.escape(out.rstrip())}</code></pre>'
        bottom = "".join(row(k) for k in ["verify", "errors", "security", "docs", "assignment", "assessment", "gate"])
        refs = ""
        if m.get("refs"):
            refs = '<p class="metaline">Read: ' + " · ".join(
                f'<a href="{html.escape(u)}">{html.escape(l)}</a>' for l, u in m["refs"]) + "</p>"
        return (
            f'<details class="module" id="{hid}" open data-module="{hid}">'
            f'<summary><h3 id="{hid}-h">{html.escape(title)}</h3><div class="tags">{tags}</div></summary>'
            f'<div class="mbody"><table class="card"><tbody>{top}</tbody></table>'
            f"<h4>6 · Step-by-step procedure</h4><ol>{steps}</ol>"
            f"<h4>7 · Complete example</h4>{ex}<h4>8 · Expected output</h4>{out_html}"
            f'<table class="card"><tbody>{bottom}</tbody></table>{refs}'
            f'<p class="mdone no-print"><label><input type="checkbox" data-done="{hid}"> Mark module complete (saved in this browser)</label></p>'
            f"</div></details>"
        )


STAGE_FIELDS = [("purpose", "Purpose"), ("owner", "Responsible employee"), ("inputs", "Inputs"), ("procedure", "Procedure"),
                ("outputs", "Outputs"), ("tools", "Tools"), ("risks", "Risks"), ("verification", "Verification"),
                ("documentation", "Documentation"), ("approval", "Approval authority")]


def _render_stages(self, name: str) -> str:
    data = yaml.safe_load((CONTENT / "modules" / f"{name}.yaml").read_text(encoding="utf-8"))
    out = []
    for st in data["stages"]:
        missing = [k for k, _ in STAGE_FIELDS if not st.get(k)]
        if missing:
            raise SystemExit(f"Stage {st.get('n')} missing fields: {missing}")
        title = f"Stage {int(st['n']):02d} — {st['title']}"
        hid = slug(title, self.used_ids)
        self.toc.append((2, hid, title))
        rows = []
        for k, label in STAGE_FIELDS:
            v = st[k]
            cell = "<ol>" + "".join(f"<li>{inline(i)}</li>" for i in v) + "</ol>" if isinstance(v, list) else inline(v)
            rows.append(f"<tr><td>{label}</td><td>{cell}</td></tr>")
        gate = ""
        if st.get("gate"):
            gate = (f'<div class="admonition gate"><p class="admonition-title">Gate {int(st["n"]):02d}</p>'
                    f'<p>{inline(st["gate"])}</p></div>')
        out.append(f'<h2 id="{hid}">{html.escape(title)}</h2><table class="card stage"><tbody>{"".join(rows)}</tbody></table>{gate}')
    return "\n".join(out)


Renderer.render_stages = _render_stages


def logo_data_uri() -> str:
    return "data:image/png;base64," + base64.b64encode((ROOT / "assets" / "vel-logo.png").read_bytes()).decode()


def font_face_css() -> str:
    spec = [("VEL Inter", 400, "Inter-Regular"), ("VEL Inter", 500, "Inter-Medium"), ("VEL Inter", 600, "Inter-SemiBold"),
            ("VEL Inter", 700, "Inter-Bold"), ("VEL Serif", 700, "LiberationSerif-Bold"),
            ("VEL Mono", 400, "LiberationMono-Regular"), ("VEL Mono", 700, "LiberationMono-Bold")]
    css = []
    for fam, w, f in spec:
        b = base64.b64encode((ROOT / "assets" / "fonts" / f"{f}.woff2").read_bytes()).decode()
        css.append(f'@font-face{{font-family:"{fam}";font-weight:{w};src:url(data:font/woff2;base64,{b}) format("woff2")}}')
    return "\n".join(css)
