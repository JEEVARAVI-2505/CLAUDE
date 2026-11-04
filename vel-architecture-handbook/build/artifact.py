import os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
import build
h = build.web_html(artifact=True)
title = '<title>VEL Architecture Handbook</title>'
fonts = re.search(r'<link rel="preconnect".*?rel="stylesheet">', h, re.S).group(0)
css = re.search(r'<style>(.*?)</style>', h, re.S).group(1)
# token-level dark mode for system preference
m = re.search(r':root\[data-theme=dark\]\{(.*?)\}', css)
dark = m.group(1)
css += f'\n@media (prefers-color-scheme: dark){{:root:not([data-theme=light]){{{dark};color-scheme:dark}}}}\n:root[data-theme=dark]{{color-scheme:dark}}\n'
css += '''
body{padding-left:max(16px,env(safe-area-inset-left,0px));padding-right:max(16px,env(safe-area-inset-right,0px))}
body::before{content:"";position:fixed;inset:0 0 auto;height:env(safe-area-inset-top,0px);z-index:70;background:var(--bg)}
.top{top:env(safe-area-inset-top,0px)}#side{top:calc(57px + env(safe-area-inset-top,0px));height:calc(100vh - 57px - env(safe-area-inset-top,0px))}
@media(max-width:980px){#side{top:calc(57px + env(safe-area-inset-top,0px))}}
'''
body = re.search(r'<body>(.*)</body>', h, re.S).group(1)
body = body.replace('<a href="Cost-Estimation-Template.xlsx" target="_blank" rel="noopener">Cost estimation (XLSX)</a>', '')
meta = '<meta name="description" content="VEL Software Solutions software architecture and consulting handbook: training, 15-stage lifecycle, templates, costing and official sources.">'
out = f'{title}\n<meta name="claude:viewport-fit-sides" content="cover">\n{fonts}\n<style>{css}</style>\n{body}'
os.makedirs(os.path.join(build.OUT, 'artifact'), exist_ok=True)
open(os.path.join(build.OUT, 'artifact', 'index.html'), 'w').write(out)
print(len(out))
