"""Uniform page source for PDFs and images (text layer, rendering, safety limits)."""

from __future__ import annotations

import logging
import threading
from pathlib import Path
from typing import Protocol

import pypdf
import pypdfium2 as pdfium
from PIL import Image, ImageOps

from ..config import LimitSettings
from ..domain.exceptions import CorruptedFileError, PasswordProtectedError, TooManyPagesError

log = logging.getLogger("documind.pdf")
_PDFIUM_LOCK = threading.RLock()  # PDFium is not thread-safe
MAX_RENDER_PIXELS = 40_000_000
MAX_TEXT_CHARS_PER_PAGE = 200_000


class PageSource(Protocol):
    page_count: int
    kind: str

    def embedded_text(self, index: int) -> str: ...
    def image(self, index: int, dpi: int = 150) -> Image.Image: ...
    def close(self) -> None: ...


class PdfSource:
    kind = "pdf"

    def __init__(self, path: Path, limits: LimitSettings) -> None:
        self.path = Path(path)
        self._reader: pypdf.PdfReader | None = None
        self._pdfium: pdfium.PdfDocument | None = None
        encrypted = False
        try:
            self._reader = pypdf.PdfReader(str(self.path))
            if self._reader.is_encrypted:
                encrypted = True
                try:
                    ok = self._reader.decrypt("")
                    if not ok:
                        raise PasswordProtectedError("This PDF is password-protected. Remove the password (open it, print to PDF) and import it again.")
                except PasswordProtectedError:
                    raise
                except Exception:  # missing AES backend etc. -> let PDFium try with an empty password
                    self._reader = None
            n = len(self._reader.pages) if self._reader is not None else None
        except PasswordProtectedError:
            raise
        except Exception as e:  # pypdf raises many types on malformed input
            log.info("pypdf could not open file (%s)", type(e).__name__)
            self._reader, n = None, None
        try:
            with _PDFIUM_LOCK:
                self._pdfium = pdfium.PdfDocument(str(self.path))
                m = len(self._pdfium)
        except pdfium.PdfiumError as e:
            if "password" in str(e).lower():
                raise PasswordProtectedError("This PDF is password-protected. Remove the password and import it again.") from e
            raise CorruptedFileError("The PDF is damaged or not a valid PDF and cannot be read.", detail=str(e)) from e
        if n is not None and n != m and m:
            n = m  # trust the renderer's count for rendering consistency
        self.page_count = n if n is not None else m
        self.encrypted = encrypted
        if self.page_count == 0:
            raise CorruptedFileError("The PDF contains no pages.")
        if self.page_count > limits.max_pages:
            self.close()
            raise TooManyPagesError(f"The PDF has {self.page_count} pages; the limit is {limits.max_pages} (Settings > Limits).")

    def embedded_text(self, index: int) -> str:
        if self._reader is not None:
            try:
                page = self._reader.pages[index]
                try:
                    txt = page.extract_text(extraction_mode="layout")
                except Exception:
                    txt = ""
                if not txt or not txt.strip():
                    txt = page.extract_text() or ""
                if txt.strip():
                    return txt[:MAX_TEXT_CHARS_PER_PAGE]
            except Exception as e:
                log.info("page %d text extraction failed (%s)", index + 1, type(e).__name__)
        if self._pdfium is not None:
            try:
                with _PDFIUM_LOCK:
                    page = self._pdfium[index]
                    tp = page.get_textpage()
                    txt = tp.get_text_range()
                    tp.close()
                    page.close()
                return txt[:MAX_TEXT_CHARS_PER_PAGE]
            except Exception:
                return ""
        return ""

    def image(self, index: int, dpi: int = 150) -> Image.Image:
        assert self._pdfium is not None
        with _PDFIUM_LOCK:
            page = self._pdfium[index]
            try:
                w, h = page.get_size()
                scale = dpi / 72.0
                if w * h * scale * scale > MAX_RENDER_PIXELS:
                    scale = (MAX_RENDER_PIXELS / (w * h)) ** 0.5
                img = page.render(scale=scale).to_pil()
            finally:
                page.close()
        return img.convert("RGB")

    def close(self) -> None:
        with _PDFIUM_LOCK:
            if self._pdfium is not None:
                try:
                    self._pdfium.close()
                except Exception:
                    pass
                self._pdfium = None
        self._reader = None


class ImageSource:
    kind = "image"

    def __init__(self, path: Path, limits: LimitSettings) -> None:
        self.path = Path(path)
        self.limits = limits
        Image.MAX_IMAGE_PIXELS = limits.max_image_megapixels * 1_000_000
        try:
            with Image.open(self.path) as im:
                im.verify()
            with Image.open(self.path) as im:
                self.page_count = max(1, getattr(im, "n_frames", 1))
                w, h = im.size
                if w * h > limits.max_image_megapixels * 1_000_000:
                    raise CorruptedFileError(f"The image is {w}x{h} pixels, which exceeds the {limits.max_image_megapixels} MP limit.")
                im.seek(0)
                im.load()  # catches truncated data on first page
        except CorruptedFileError:
            raise
        except (Image.DecompressionBombError, Image.DecompressionBombWarning) as e:
            raise CorruptedFileError("The image is too large to process safely.", detail=str(e)) from e
        except Exception as e:
            raise CorruptedFileError("The image is damaged or not a valid image file.", detail=f"{type(e).__name__}: {e}") from e
        if self.page_count > limits.max_pages:
            raise TooManyPagesError(f"The image has {self.page_count} pages; the limit is {limits.max_pages}.")

    def embedded_text(self, index: int) -> str:
        return ""

    def image(self, index: int, dpi: int = 150) -> Image.Image:
        Image.MAX_IMAGE_PIXELS = self.limits.max_image_megapixels * 1_000_000
        with Image.open(self.path) as im:
            im.seek(index)
            frame = ImageOps.exif_transpose(im.copy())
        if frame.mode not in ("RGB", "L"):
            frame = frame.convert("RGB")
        return frame

    def close(self) -> None:
        pass


def open_source(path: Path, mime: str, limits: LimitSettings) -> PageSource:
    return PdfSource(path, limits) if mime == "application/pdf" else ImageSource(path, limits)  # type: ignore[return-value]
