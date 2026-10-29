"""Tiny dependency-free PDF writer and scan simulator for synthetic samples.

Produces text PDFs (Courier, so columns align), image-only PDFs and degraded
page images. Used by demo mode, tests and the benchmark; not used on customer
documents.
"""

from __future__ import annotations

import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

PAGE_W, PAGE_H, MARGIN, FS, LEAD = 612, 792, 36, 9, 11
LINES_PER_PAGE = int((PAGE_H - 2 * MARGIN) / LEAD)


def _esc(s: str) -> bytes:
    b = s.encode("cp1252", errors="replace")
    return b.replace(b"\\", b"\\\\").replace(b"(", b"\\(").replace(b")", b"\\)")


def write_text_pdf(pages: list[list[str]], path: Path) -> None:
    objs: list[bytes] = []
    n_pages = len(pages)
    font_id = 3 + 2 * n_pages
    kids = " ".join(f"{3 + 2 * i} 0 R" for i in range(n_pages))
    objs.append(b"<< /Type /Catalog /Pages 2 0 R >>")
    objs.append(f"<< /Type /Pages /Kids [{kids}] /Count {n_pages} >>".encode())
    for i, lines in enumerate(pages):
        content = bytearray(f"BT /F1 {FS} Tf {LEAD} TL {MARGIN} {PAGE_H - MARGIN - FS} Td\n".encode())
        for ln in lines:
            content += b"(" + _esc(ln) + b") Tj T*\n"
        content += b"ET"
        pid, cid = 3 + 2 * i, 4 + 2 * i
        objs.append(f"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PAGE_W} {PAGE_H}] /Resources << /Font << /F1 {font_id} 0 R >> >> /Contents {cid} 0 R >>".encode())
        objs.append(b"<< /Length %d >>\nstream\n" % len(content) + bytes(content) + b"\nendstream")
    objs.append(b"<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>")
    out = bytearray(b"%PDF-1.4\n%\xe2\xe3\xcf\xd3\n")
    offsets = []
    for i, o in enumerate(objs, 1):
        offsets.append(len(out))
        out += f"{i} 0 obj\n".encode() + o + b"\nendobj\n"
    xref = len(out)
    out += f"xref\n0 {len(objs) + 1}\n0000000000 65535 f \n".encode()
    for off in offsets:
        out += f"{off:010d} 00000 n \n".encode()
    out += f"trailer\n<< /Size {len(objs) + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n".encode()
    Path(path).write_bytes(bytes(out))


def paginate(lines: list[str], per_page: int = LINES_PER_PAGE) -> list[list[str]]:
    return [lines[i:i + per_page] for i in range(0, len(lines), per_page)] or [[]]


def render_page_image(lines: list[str], *, font_px: int = 22, seed: int = 0, rotate: float = 0.0,
                      blur: float = 0.0, noise: int = 0, jpeg: bool = False) -> Image.Image:
    """Draw tokens on a fixed character grid (keeps table columns aligned)."""
    font = ImageFont.load_default(size=font_px)
    cw = int(font_px * 0.62)
    width = max(len(ln) for ln in lines) * cw + 120 if lines else 800
    height = len(lines) * int(font_px * 1.45) + 120
    img = Image.new("L", (width, height), 255)
    d = ImageDraw.Draw(img)
    y = 60
    for ln in lines:
        x = 0
        while x < len(ln):
            if ln[x] == " ":
                x += 1
                continue
            e = x
            while e < len(ln) and ln[e] != " ":
                e += 1
            d.text((60 + x * cw, y), ln[x:e], fill=20, font=font)
            x = e
        y += int(font_px * 1.45)
    rng = random.Random(seed)
    if rotate:
        img = img.rotate(rotate, expand=True, fillcolor=255, resample=Image.Resampling.BICUBIC)
    if blur:
        img = img.filter(ImageFilter.GaussianBlur(blur))
    if noise:
        px = img.load()
        for _ in range(int(img.width * img.height * 0.002 * noise)):
            px[rng.randrange(img.width), rng.randrange(img.height)] = rng.choice((0, 255, 128))
    return img.convert("RGB") if jpeg else img


def write_image_pdf(images: list[Image.Image], path: Path) -> None:
    rgb = [im.convert("RGB") for im in images]
    rgb[0].save(path, "PDF", save_all=True, append_images=rgb[1:], resolution=150.0)
