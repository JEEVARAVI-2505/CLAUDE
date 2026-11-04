"""Mini IR + renderers (HTML / Markdown) + SVG helpers for the VEL handbook build."""
import html, re

# ---------- inline markup: **bold**, `code`, [text](url) ----------
def inline(t, md=False):
    if md:
        return t
    s = html.escape(t, quote=False)
    s = re.sub(r'`([^`]+)`', r'<code>\1</code>', s)
    s = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', s)
    s = re.sub(r'\[([^\]]+)\]\((https?://[^)]+)\)',
               r'<a href="\2" target="_blank" rel="noopener noreferrer">\1</a>', s)
    return s

def plain(t):
    return re.sub(r'\[([^\]]+)\]\([^)]+\)', r'\1', t)

# ---------- IR constructors ----------
def H1(title, id, num=None): return dict(t='h1', title=title, id=id, num=num)
def H2(title): return dict(t='h2', title=title)
def H3(title): return dict(t='h3', title=title)
def P(text): return dict(t='p', text=text)
def UL(items): return dict(t='ul', items=items)
def OL(items): return dict(t='ol', items=items)
def CHECK(items): return dict(t='check', items=items)
def TABLE(head, rows, w=None, cls=''): return dict(t='table', head=head, rows=rows, w=w, cls=cls)
def CALL(kind, title, text): return dict(t='call', kind=kind, title=title, text=text)  # gold|red|blue|green
def CODE(text, lang=''): return dict(t='code', text=text, lang=lang)
def PILLS(items): return dict(t='pills', items=items)
def CARDS(items): return dict(t='cards', items=items)  # (title, html-ish text)
def DIAGRAM(svg, caption, source=None, lang='mermaid'): return dict(t='diagram', svg=svg, caption=caption, source=source, lang=lang)
def BREAK(): return dict(t='break')
def FORM(rows): return dict(t='form', rows=rows)  # fill-in lines: (label, hint)

def LESSON(code, title, parts):
    """15-part teaching format. parts: dict of the 15 keys in LESSON_KEYS."""
    return dict(t='lesson', code=code, title=title, parts=parts)

LESSON_KEYS = ['Definition','Business relevance','Technical purpose','Real-world case','Prerequisites',
 'Tools','Step-by-step method','Diagram','Decision trade-offs','Expected output','Verification',
 'Common mistakes','Documentation','Assignment','Review and approval']

# ---------- HTML renderer ----------
def _li(items, md=False): return ''.join(f'<li>{inline(i)}</li>' for i in items)

def render_html(blocks, sections_numbered=True):
    out = []
    for b in blocks:
        t = b['t']
        if t == 'h1':
            n = f'<span class="num">{b["num"]}.</span> ' if isinstance(b.get('num'), int) else ''
            out.append(f'</section><section class="chapter" id="{b["id"]}"><h1>{n}{html.escape(b["title"])}</h1>')
        elif t == 'h2': out.append(f'<h2>{inline(b["title"])}</h2>')
        elif t == 'h3': out.append(f'<h3>{inline(b["title"])}</h3>')
        elif t == 'p': out.append(f'<p>{inline(b["text"])}</p>')
        elif t == 'ul': out.append(f'<ul>{_li(b["items"])}</ul>')
        elif t == 'ol': out.append(f'<ol>{_li(b["items"])}</ol>')
        elif t == 'check': out.append('<ul class="check">' + ''.join(f'<li><span class="box"></span>{inline(i)}</li>' for i in b['items']) + '</ul>')
        elif t == 'table':
            cg = ''
            if b['w']: cg = '<colgroup>' + ''.join(f'<col style="width:{w}%">' for w in b['w']) + '</colgroup>'
            th = ''.join(f'<th>{inline(h)}</th>' for h in b['head'])
            tr = ''.join('<tr>' + ''.join(f'<td>{inline(c)}</td>' for c in r) + '</tr>' for r in b['rows'])
            out.append(f'<div class="tw"><table class="{b["cls"]}">{cg}<thead><tr>{th}</tr></thead><tbody>{tr}</tbody></table></div>')
        elif t == 'call':
            body = ('<ol>' + _li(b['text']) + '</ol>') if isinstance(b['text'], list) else inline(b['text'])
            out.append(f'<div class="callout {b["kind"]}"><div class="ct">{html.escape(b["title"])}</div><div>{body}</div></div>')
        elif t == 'code':
            out.append(f'<div class="codewrap"><button class="copy" type="button" aria-label="Copy code">Copy</button><pre><code>{html.escape(b["text"])}</code></pre></div>')
        elif t == 'pills':
            out.append('<div class="pills">' + '<span class="arr">→</span>'.join(f'<span class="pill">{html.escape(i)}</span>' for i in b['items']) + '</div>')
        elif t == 'cards':
            out.append('<div class="cards">' + ''.join(f'<div class="card"><h4>{inline(a)}</h4><div>{inline(c)}</div></div>' for a, c in b['items']) + '</div>')
        elif t == 'diagram':
            src = ''
            if b['source']:
                src = f'<details class="src"><summary>Diagram-as-code source ({b["lang"]})</summary><pre><code>{html.escape(b["source"])}</code></pre></details>'
            out.append(f'<figure class="diagram">{b["svg"]}<figcaption>{inline(b["caption"])}</figcaption>{src}</figure>')
        elif t == 'break': out.append('<div class="pb"></div>')
        elif t == 'form':
            out.append('<div class="tw"><table class="formt"><tbody>' + ''.join(f'<tr><th>{inline(a)}</th><td class="fill">{inline(c)}</td></tr>' for a, c in b['rows']) + '</tbody></table></div>')
        elif t == 'lesson':
            rows = ''.join(f'<tr><th><span class="ln">{i+1}</span>{k}</th><td>{inline(b["parts"][k]) if isinstance(b["parts"][k], str) else b["parts"][k]["html"]}</td></tr>' for i, k in enumerate(LESSON_KEYS))
            out.append(f'<h3 class="lesson-h"><span class="lcode">{b["code"]}</span> {html.escape(b["title"])}</h3><div class="tw"><table class="lesson"><tbody>{rows}</tbody></table></div>')
    return '\n'.join(out)

# ---------- Markdown renderer ----------
def render_md(blocks):
    o = []
    for b in blocks:
        t = b['t']
        if t == 'h1': o.append(f'# {b["title"]}\n')
        elif t == 'h2': o.append(f'## {plain(b["title"])}\n')
        elif t == 'h3': o.append(f'### {plain(b["title"])}\n')
        elif t == 'p': o.append(b['text'] + '\n')
        elif t == 'ul': o.append('\n'.join(f'- {i}' for i in b['items']) + '\n')
        elif t == 'ol': o.append('\n'.join(f'{n+1}. {i}' for n, i in enumerate(b['items'])) + '\n')
        elif t == 'check': o.append('\n'.join(f'- [ ] {i}' for i in b['items']) + '\n')
        elif t == 'table':
            esc = lambda c: str(c).replace('|', '\\|').replace('\n', ' ')
            o.append('| ' + ' | '.join(esc(h) for h in b['head']) + ' |\n|' + '---|' * len(b['head']) + '\n' +
                     '\n'.join('| ' + ' | '.join(esc(c) for c in r) + ' |' for r in b['rows']) + '\n')
        elif t == 'call':
            txt = '; '.join(f'({i+1}) {x}' for i, x in enumerate(b['text'])) if isinstance(b['text'], list) else b['text']
            o.append(f'> **{b["title"]}** — {txt}\n')
        elif t == 'code': o.append(f'```{b["lang"]}\n{b["text"]}\n```\n')
        elif t == 'pills': o.append(' → '.join(b['items']) + '\n')
        elif t == 'cards': o.append('\n'.join(f'- **{a}** — {c}' for a, c in b['items']) + '\n')
        elif t == 'diagram':
            o.append(f'*Figure: {b["caption"]}*\n')
            if b['source']: o.append(f'```{b["lang"]}\n{b["source"]}\n```\n')
        elif t == 'form': o.append('| Field | Entry |\n|---|---|\n' + '\n'.join(f'| {a} | _{c}_ |' for a, c in b['rows']) + '\n')
        elif t == 'lesson':
            o.append(f'### {b["code"]} — {b["title"]}\n')
            o.append('| # | Part | Content |\n|---|---|---|\n' + '\n'.join(
                f'| {i+1} | {k} | ' + (b["parts"][k] if isinstance(b["parts"][k], str) else b["parts"][k]["md"]).replace('|', '\\|').replace('\n', ' ') + ' |'
                for i, k in enumerate(LESSON_KEYS)) + '\n')
    return '\n'.join(o)

# ---------- SVG helpers ----------
NAVY, MAG, GOLD = '#14306b', '#b5195b', '#c8961e'
FONT = "Inter, 'Segoe UI', Arial, sans-serif"

class Svg:
    def __init__(self, w, h, title):
        self.w, self.h, self.title, self.e = w, h, title, []
    def _t(self, x, y, text, size=12, fill='#1b2540', weight=400, anchor='middle', lh=None):
        lh = lh or size * 1.25
        lines = text.split('\n')
        y0 = y - (len(lines) - 1) * lh / 2 + size * 0.35
        return ''.join(f'<text x="{x}" y="{y0 + i*lh:.1f}" font-size="{size}" fill="{fill}" font-weight="{weight}" text-anchor="{anchor}">{html.escape(l)}</text>' for i, l in enumerate(lines))
    def box(self, x, y, w, h, text, fill='#eef2fb', stroke=NAVY, color='#14306b', size=12, weight=600, rx=8, dash=False):
        d = ' stroke-dasharray="6 4"' if dash else ''
        self.e.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="{fill}" stroke="{stroke}" stroke-width="1.5"{d}/>' + self._t(x + w/2, y + h/2, text, size, color, weight))
    def label(self, x, y, text, size=11, fill='#5a6480', weight=400, anchor='middle'):
        self.e.append(self._t(x, y, text, size, fill, weight, anchor))
    def zone(self, x, y, w, h, title, stroke=MAG, fill='none'):
        self.e.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="10" fill="{fill}" stroke="{stroke}" stroke-width="1.5" stroke-dasharray="7 5"/>' + self._t(x + 10, y + 16, title, 11, stroke, 700, 'start'))
    def db(self, x, y, w, h, text, fill='#fff4dc', stroke=GOLD):
        ry = 9
        self.e.append(f'<path d="M{x},{y+ry} v{h-2*ry} a{w/2},{ry} 0 0 0 {w},0 v{-(h-2*ry)}" fill="{fill}" stroke="{stroke}" stroke-width="1.5"/><ellipse cx="{x+w/2}" cy="{y+ry}" rx="{w/2}" ry="{ry}" fill="{fill}" stroke="{stroke}" stroke-width="1.5"/>' + self._t(x + w/2, y + h/2 + 4, text, 11.5, '#6b4a00', 600))
    def person(self, x, y, text, sub=''):
        self.e.append(f'<circle cx="{x}" cy="{y}" r="10" fill="{NAVY}"/><path d="M{x-17},{y+34} q0,-22 17,-22 q17,0 17,22z" fill="{NAVY}"/>' + self._t(x, y + 50, text, 11.5, NAVY, 700) + (self._t(x, y + 64, sub, 10, '#5a6480') if sub else ''))
    def arrow(self, x1, y1, x2, y2, text='', both=False, dash=False, dx=0, dy=-6):
        d = ' stroke-dasharray="5 4"' if dash else ''
        ms = ' marker-start="url(#a)"' if both else ''
        self.e.append(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#4a5578" stroke-width="1.5"{d} marker-end="url(#a)"{ms}/>')
        if text:
            self.e.append(self._t((x1 + x2)/2 + dx, (y1 + y2)/2 + dy, text, 10, '#4a5578', 500))
    def path(self, d, text='', tx=0, ty=0, dash=False):
        ds = ' stroke-dasharray="5 4"' if dash else ''
        self.e.append(f'<path d="{d}" fill="none" stroke="#4a5578" stroke-width="1.5"{ds} marker-end="url(#a)"/>')
        if text: self.e.append(self._t(tx, ty, text, 10, '#4a5578', 500))
    def render(self):
        defs = '<defs><marker id="a" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z" fill="#4a5578"/></marker></defs>'
        return (f'<svg class="svgd" viewBox="0 0 {self.w} {self.h}" role="img" aria-label="{html.escape(self.title)}" xmlns="http://www.w3.org/2000/svg" font-family="{FONT}">'
                f'<title>{html.escape(self.title)}</title>{defs}' + ''.join(self.e) + '</svg>')
