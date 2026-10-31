"""Generates CRM MASTER's application icon (navy rounded square + white 'C' ring + data bars)."""
from PIL import Image, ImageDraw
import sys

def draw(size):
    s = size * 4
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    navy = (15, 38, 84, 255)
    d.rounded_rectangle([0, 0, s - 1, s - 1], radius=int(s * 0.22), fill=navy)
    white = (255, 255, 255, 255)
    accent = (96, 165, 250, 255)
    cx = cy = s / 2
    r = s * 0.30
    w = int(s * 0.10)
    d.arc([cx - r, cy - r, cx + r, cy + r], start=40, end=320, fill=white, width=w)
    bw = int(s * 0.07)
    base = cy + s * 0.14
    for i, h in enumerate((0.12, 0.20, 0.30)):
        x = cx - s * 0.02 + i * (bw + s * 0.035)
        d.rounded_rectangle([x, base - s * h, x + bw, base], radius=bw // 3, fill=accent)
    return img.resize((size, size), Image.LANCZOS)

sizes = [16, 24, 32, 48, 64, 128, 256]
images = [draw(sz) for sz in sizes]
out = sys.argv[1]
images[-1].save(out, format="ICO", sizes=[(s, s) for s in sizes], append_images=images[:-1])
images[-1].save(out.replace(".ico", ".png"))
print("wrote", out)
