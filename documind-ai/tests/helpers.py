"""Shared builders for tests (synthetic documents with known answers)."""
from __future__ import annotations

from pathlib import Path

from documind.application.context import AppContext
from documind.config import ConfigManager
from documind.domain.models import PageText
from documind.domain.rules_extractor import extract_with_rules
from documind.domain.templates import load_pack
from documind.resource import resource_path
from documind.samples import generate, write_sample
from documind.samples_pdf import write_text_pdf


def pack():
    return load_pack(resource_path("rules", "labels_default.json"))


def extract(text: str, **kw):
    return extract_with_rules([PageText(page=1, text=text)], pack(), **kw)


def make_ctx(**kw) -> AppContext:
    return AppContext.create(ConfigManager(), **kw)


def sample_pdf(tmp: Path, seed: int = 1, layout: str | None = None, name: str | None = None):
    doc = generate(seed, layout)
    p = tmp / (name or f"s{seed}_{doc.layout}.pdf")
    write_sample(doc, p)
    return doc, p


def pdf_from_lines(path: Path, lines: list[str]) -> Path:
    write_text_pdf([lines], path)
    return path


# ---- file builders ---------------------------------------------------------------
import io
import shutil

from PIL import Image


def make_png(path: Path, text_lines: list[str] | None = None, **degrade) -> Path:
    from documind.samples_pdf import render_page_image
    render_page_image(text_lines or ["Invoice No: X-1", "Total 10.00"], **degrade).save(path, "PNG")
    return path


def make_encrypted_pdf(path: Path, password: str = "secret") -> Path:
    import pypdf
    w = pypdf.PdfWriter()
    w.add_blank_page(200, 200)
    w.encrypt(password, algorithm="RC4-128")
    with open(path, "wb") as fh:
        w.write(fh)
    return path


def tesseract_available() -> bool:
    return shutil.which("tesseract") is not None


def process_all(ctx, ids, timeout: float = 180):
    ctx.batch.enqueue(ids)
    assert ctx.batch.wait_idle(timeout), "batch did not finish"
    return [ctx.repo.get(i) for i in ids]
