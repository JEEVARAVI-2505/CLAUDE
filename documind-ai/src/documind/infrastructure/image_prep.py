"""Pre-OCR image clean-up: grayscale, contrast, deskew, speckle removal (Pillow only)."""

from __future__ import annotations

from PIL import Image, ImageFilter, ImageOps


def estimate_skew_deg(gray: Image.Image, max_angle: float = 6.0) -> float:
    """Angle (degrees, counter-clockwise) to rotate by so text lines become horizontal."""
    small = gray.copy()
    small.thumbnail((720, 1000))
    ink = small.point(lambda p: 255 if p < 150 else 0)

    def score(angle: float) -> float:
        rot = ink.rotate(angle, resample=Image.Resampling.NEAREST, fillcolor=0)
        rows = list(rot.resize((1, rot.height), Image.Resampling.BOX).tobytes())
        mean = sum(rows) / len(rows)
        return sum((v - mean) ** 2 for v in rows)

    coarse = [a / 2 for a in range(int(-max_angle * 2), int(max_angle * 2) + 1)]
    best = max(coarse, key=score)
    fine = [best + d / 10 for d in range(-4, 5)]
    return max(fine, key=score)


def prepare_for_ocr(img: Image.Image, *, deskew: bool = True, denoise: bool = True) -> tuple[Image.Image, float]:
    """Return (clean grayscale image, applied skew correction in degrees)."""
    g = ImageOps.autocontrast(img.convert("L"), cutoff=1)
    if min(g.size) < 1200:
        f = 2 if min(g.size) >= 600 else 3
        g = g.resize((g.width * f, g.height * f), Image.Resampling.LANCZOS)
    angle = 0.0
    if deskew:
        angle = estimate_skew_deg(g)
        if abs(angle) >= 0.4:
            g = g.rotate(angle, resample=Image.Resampling.BICUBIC, expand=True, fillcolor=255)
        else:
            angle = 0.0
    if denoise:
        g = g.filter(ImageFilter.MedianFilter(3))
    return g, angle
