"""Subset Inter + Liberation fonts to WOFF2 so the standalone HTML renders identically offline.
Fonts: Inter (SIL OFL 1.1), Liberation (SIL OFL 1.1).  Re-run only when the font list changes."""
import base64, pathlib
from fontTools import subset

OUT = pathlib.Path(__file__).parent / "assets" / "fonts"
OUT.mkdir(parents=True, exist_ok=True)
UNI = "U+0020-007E,U+00A0-00FF,U+2010-2027,U+2030-203A,U+20AC,U+20B9,U+2190-2193,U+2212,U+2264-2265,U+2713-2714,U+2022,U+00D7,U+2248,U+221E"
FONTS = [
    ("Inter", 400, "/usr/share/fonts/opentype/inter/Inter-Regular.otf"),
    ("Inter", 600, "/usr/share/fonts/opentype/inter/Inter-SemiBold.otf"),
    ("Inter", 700, "/usr/share/fonts/opentype/inter/Inter-Bold.otf"),
    ("VEL Serif", 700, "/usr/share/fonts/truetype/liberation/LiberationSerif-Bold.ttf"),
    ("VEL Mono", 400, "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf"),
]
css = []
for fam, w, path in FONTS:
    opts = subset.Options(); opts.flavor = "woff2"; opts.layout_features = ["kern", "liga", "calt", "tnum"]
    opts.notdef_outline = True
    f = subset.load_font(path, opts)
    s = subset.Subsetter(opts); s.populate(unicodes=subset.parse_unicodes(UNI)); s.subset(f)
    dst = OUT / f"{fam.replace(' ','')}-{w}.woff2"
    subset.save_font(f, str(dst), opts)
    b64 = base64.b64encode(dst.read_bytes()).decode()
    css.append(f'@font-face{{font-family:"{fam}";font-weight:{w};font-style:normal;font-display:swap;'
               f'src:url(data:font/woff2;base64,{b64}) format("woff2");}}')
    print(dst.name, dst.stat().st_size)
(OUT.parent / "fonts.css").write_text("\n".join(css))
