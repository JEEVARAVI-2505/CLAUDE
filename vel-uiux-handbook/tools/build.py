#!/usr/bin/env python3
"""Builds dist/VEL-UIUX-Handbook-Part1.html from parts/*.html + design-tokens/tokens.json.
Contrast ratios are COMPUTED here (WCAG 2.x relative-luminance formula), never typed by hand."""
import json, base64, pathlib, re
ROOT = pathlib.Path(__file__).resolve().parent.parent
tok = json.loads((ROOT/'design-tokens/tokens.json').read_text())
C = tok['color']

def lum(h):
    h = h.lstrip('#'); h = ''.join(c*2 for c in h) if len(h)==3 else h
    r = [int(h[i:i+2],16)/255 for i in (0,2,4)]
    r = [c/12.92 if c <= .03928 else ((c+.055)/1.055)**2.4 for c in r]
    return .2126*r[0]+.7152*r[1]+.0722*r[2]
def ratio(a,b):
    hi,lo = sorted([lum(a),lum(b)], reverse=True); return (hi+.05)/(lo+.05)
def txt_on(h): return '#fff' if ratio(h,'#FFFFFF') >= 4.5 else '#111827'

NAMES = {'navy':'Vel Navy','magenta':'Lotus Magenta','gold':'Temple Gold','emerald':'Gem Emerald','neutral':'Neutral'}
ROLE = {'navy':{'900':'Logo wordmark, headings, dark UI','700':'Links, secondary actions','100':'Tints, selected rows'},
        'magenta':{'600':'Primary action, accent rule'},'gold':{'500':'Highlight on navy only','700':'Text on gold tint'},
        'emerald':{'600':'Success, positive status'}}
def swatches():
    out=[]
    for fam in ('navy','magenta','gold','emerald','neutral'):
        out.append(f'<h4>{NAMES[fam]}</h4><div class="sw-grid">')
        for k,v in C[fam].items():
            role = ROLE.get(fam,{}).get(k,'')
            out.append(f'<div class="sw"><div class="c" style="background:{v}"></div><div class="l"><b>{fam}-{k}</b>{v}<br>'
                       f'<span style="color:#4B5563">{role}</span></div></div>')
        out.append('</div>')
    out.append('<h4>Semantic colours (status)</h4><div class="sw-grid">')
    for k,v in C['semantic'].items():
        out.append(f'<div class="sw"><div class="c" style="background:{v}"></div><div class="l"><b>{k}</b>{v}<br>bg {C["semantic-bg"][k]}</div></div>')
    out.append(f'<div class="sw"><div class="c" style="background:{C["cream"]["100"]}"></div><div class="l"><b>cream-100</b>{C["cream"]["100"]}<br>Callout background</div></div></div>')
    return '\n'.join(out)

PAIRS = [
 ('Body text','neutral.900','neutral.0'),('Secondary text','neutral.600','neutral.0'),('Muted text (min. allowed)','neutral.500','neutral.0'),
 ('Headings','navy.800','neutral.0'),('Logo navy on white','navy.900','neutral.0'),('Link / secondary action','navy.700','neutral.0'),
 ('Primary button label','neutral.0','magenta.600'),('Magenta text on white','magenta.600','neutral.0'),('Magenta text on cream','magenta.600','cream.100'),
 ('Magenta on its own tint','magenta.600','magenta.100'),('Navy on gold-500 highlight','navy.900','gold.500'),
 ('Gold-600 on white (DO NOT use for text)','gold.600','neutral.0'),('Gold-700 on white','gold.700','neutral.0'),
 ('Gold-700 on gold-100 (warning)','gold.700','gold.100'),('Emerald-600 on white','emerald.600','neutral.0'),
 ('Success on success-bg','semantic.success','#DDF3E8'),('Error on white','semantic.error','neutral.0'),('Info on white','semantic.info','neutral.0'),
 ('Magenta-400 on white (DO NOT use)','magenta.400','neutral.0'),('Emerald-500 on white (DO NOT use)','emerald.500','neutral.0')]
def res(ref):
    if ref.startswith('#'): return ref
    a,b = ref.split('.'); return C[a][b]
def contrast():
    rows=[]
    for name,f,b in PAIRS:
        fg,bg = res(f),res(b); r = ratio(fg,bg)
        aa = '<span class="ok">Pass</span>' if r>=4.5 else '<span class="no">Fail</span>'
        lg = '<span class="ok">Pass</span>' if r>=3 else '<span class="no">Fail</span>'
        rows.append(f'<tr><td>{name}</td><td><span style="display:inline-block;padding:1px 8px;border-radius:4px;background:{bg};color:{fg};border:1px solid #D1D5DB;font-weight:700">Aa</span></td>'
                    f'<td><code>{fg}</code> on <code>{bg}</code></td><td style="white-space:nowrap"><b>{r:.2f} : 1</b></td><td>{aa}</td><td>{lg}</td></tr>')
    return '<table><thead><tr><th>Pair</th><th>Sample</th><th>Colours</th><th>Ratio</th><th>Normal text (4.5)</th><th>Large text / UI (3)</th></tr></thead><tbody>'+'\n'.join(rows)+'</tbody></table>'

def spacing():
    return '\n'.join(f'<div class="sp"><i style="width:{max(v,2)*2}px"></i><b>space-{i}</b> = {v}px</div>' for i,v in enumerate(tok['spacing-px']))

def strip():
    cols=[C['navy']['900'],C['navy']['700'],C['magenta']['600'],C['magenta']['400'],C['gold']['500'],C['emerald']['600']]
    return '<div class="palette-strip">'+''.join(f'<i style="background:{c}"></i>' for c in cols)+'</div>'

logo = 'data:image/png;base64,'+base64.b64encode((ROOT/'assets/vel-logo.png').read_bytes()).decode()
css = (ROOT/'tools/style.css').read_text()
body = '\n'.join(p.read_text() for p in sorted((ROOT/'parts').glob('*.html')))
tokens_pretty = json.dumps({'color':{k:v for k,v in C.items() if k in ('navy','magenta')},'font':tok['font']['body'],'spacing-px':tok['spacing-px']}, indent=1)
for k,v in {'{{SWATCHES}}':swatches(),'{{CONTRAST}}':contrast(),'{{SPACING}}':spacing(),'{{STRIP}}':strip(),'{{LOGO}}':logo,'{{TOKENS}}':tokens_pretty}.items():
    body = body.replace(k,v)
html = f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>VEL UI/UX Department Handbook - Part 1</title><style>{css}</style></head><body><div class="wrap">{body}</div></body></html>'
out = ROOT/'dist/VEL-UIUX-Handbook-Part1.html'; out.write_text(html); print('wrote',out,len(html)//1024,'KB')
