"""Local OCR through a user-installed Tesseract executable (no network, no bundling).

Words are rebuilt into text lines from bounding boxes so table columns stay
aligned (gaps become runs of spaces), which the rules extractor relies on.
"""

from __future__ import annotations

import logging
import os
import re
import shutil
import statistics
import subprocess
import sys
import threading
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

from PIL import Image

from ..config import OcrSettings
from .image_prep import prepare_for_ocr
from ..domain.exceptions import OcrError, OcrLanguageMissingError, OcrUnavailableError, OperationCancelled

log = logging.getLogger("documind.ocr")
WIN_CANDIDATES = [r"C:\Program Files\Tesseract-OCR\tesseract.exe", r"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe"]
CancelFn = Callable[[], bool]


@dataclass
class OcrStatus:
    available: bool
    path: str | None = None
    version: str | None = None
    languages: list[str] = field(default_factory=list)
    missing_languages: list[str] = field(default_factory=list)
    has_osd: bool = False
    message: str = ""


@dataclass
class OcrPage:
    text: str
    score: float | None  # mean word score 0-100 as reported by Tesseract; NOT a calibrated probability
    rotation: int = 0
    words: int = 0


def _flags() -> int:
    return getattr(subprocess, "CREATE_NO_WINDOW", 0) if sys.platform == "win32" else 0


class TesseractEngine:
    def __init__(self, settings: OcrSettings, temp_dir: Path) -> None:
        self.s = settings
        self.temp_dir = temp_dir
        self._status: OcrStatus | None = None
        self._lock = threading.Lock()

    # ---- discovery ---------------------------------------------------------------
    def locate(self) -> str | None:
        cands: list[str] = []
        if self.s.tesseract_path:
            cands.append(self.s.tesseract_path)
        if w := shutil.which("tesseract"):
            cands.append(w)
        if sys.platform == "win32":
            cands += WIN_CANDIDATES + [str(Path(os.environ.get("LOCALAPPDATA", "")) / "Programs" / "Tesseract-OCR" / "tesseract.exe")]
        for c in cands:
            if c and Path(c).is_file():
                return c
        return None

    def status(self, refresh: bool = False) -> OcrStatus:
        with self._lock:
            if self._status is not None and not refresh:
                return self._status
            exe = self.locate()
            if exe is None:
                self._status = OcrStatus(False, message="Tesseract OCR is not installed. Scanned documents cannot be read until it is installed (see Help > Install OCR). Text-based PDFs still work.")
                return self._status
            try:
                ver = self._run([exe, "--version"], 20)[0].splitlines()[0]
                langs_out = self._run([exe, "--list-langs"], 20)[0].splitlines()
            except OcrError as e:
                self._status = OcrStatus(False, exe, message=f"Tesseract was found but could not be started: {e.user_message}")
                return self._status
            langs = [l.strip() for l in langs_out[1:] if l.strip()]
            wanted = [l for l in self.s.languages.split("+") if l]
            missing = [l for l in wanted if l not in langs]
            msg = "Ready." if not missing else f"Missing OCR language pack(s): {', '.join(missing)}. Install the matching .traineddata file(s)."
            self._status = OcrStatus(not missing, exe, ver, langs, missing, "osd" in langs, msg)
            return self._status

    # ---- execution -----------------------------------------------------------------
    def _run(self, cmd: list[str], timeout: float, cancel: CancelFn | None = None) -> tuple[str, str]:
        env = dict(os.environ, OMP_THREAD_LIMIT="1")
        try:
            proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8",
                                    errors="replace", creationflags=_flags(), env=env)
        except FileNotFoundError as e:
            raise OcrUnavailableError("Tesseract OCR is not installed or was moved.", detail=str(e)) from e
        except OSError as e:
            raise OcrError("Tesseract could not be started.", detail=str(e)) from e
        deadline = time.monotonic() + timeout
        while True:
            try:
                out, err = proc.communicate(timeout=0.25)
                break
            except subprocess.TimeoutExpired:
                if cancel and cancel():
                    proc.kill(); proc.communicate()
                    raise OperationCancelled()
                if time.monotonic() > deadline:
                    proc.kill(); proc.communicate()
                    raise OcrError(f"OCR timed out after {int(timeout)} seconds on one page.")
        if proc.returncode != 0:
            if "traineddata" in err or "Failed loading language" in err:
                raise OcrLanguageMissingError("An OCR language pack is missing or damaged.", detail=err.strip()[:300])
            raise OcrError("OCR failed on this page.", detail=err.strip()[:300])
        return out, err

    @staticmethod
    def _prepare(img: Image.Image) -> Image.Image:
        return prepare_for_ocr(img)[0]

    def _detect_rotation(self, exe: str, png: Path, cancel: CancelFn | None) -> int:
        try:
            out, _ = self._run([exe, str(png), "stdout", "--psm", "0", "-l", "osd"], 60, cancel)
        except OcrError:
            return 0
        rot = re.search(r"Rotate:\s*(\d+)", out)
        conf = re.search(r"Orientation confidence:\s*([\d.]+)", out)
        if rot and conf and float(conf.group(1)) >= 2.0:
            return int(rot.group(1)) % 360
        return 0

    def ocr_image(self, img: Image.Image, cancel: CancelFn | None = None) -> OcrPage:
        st = self.status()
        if not st.available or st.path is None:
            raise OcrUnavailableError(st.message)
        self.temp_dir.mkdir(parents=True, exist_ok=True)
        png = self.temp_dir / f"ocr-{os.getpid()}-{threading.get_ident()}-{int(time.time() * 1000)}.png"
        try:
            g = self._prepare(img)
            rotation = 0
            if st.has_osd:
                g.save(png)
                rotation = self._detect_rotation(st.path, png, cancel)
                if rotation:
                    g = g.rotate(-rotation, expand=True, fillcolor=255)  # PIL rotates counter-clockwise; Tesseract's Rotate is clockwise
            g.save(png)
            out, _ = self._run([st.path, str(png), "stdout", "-l", self.s.languages, "--psm", "6", "tsv"], self.s.timeout_s, cancel)
            text, score, n = tsv_to_text(out)
            return OcrPage(text, score, rotation, n)
        finally:
            png.unlink(missing_ok=True)


def tsv_to_text(tsv: str) -> tuple[str, float | None, int]:
    """Rebuild layout-preserving text and the mean word score from Tesseract TSV."""
    words: list[tuple[int, int, int, int, str, float]] = []
    for row in tsv.splitlines()[1:]:
        c = row.split("\t")
        if len(c) < 12 or c[0] != "5":
            continue
        try:
            conf = float(c[10])
            text = c[11].strip()
            if conf < 0 or not text:
                continue
            words.append((int(c[6]), int(c[7]), int(c[8]), int(c[9]), text, conf))
        except ValueError:
            continue
    if not words:
        return "", None, 0
    char_w = max(1.0, sum(w[2] for w in words) / max(1, sum(len(w[4]) for w in words)))
    med_h = statistics.median(w[3] for w in words)
    words.sort(key=lambda w: w[1] + w[3] / 2)
    lines: list[list[tuple[int, int, int, int, str, float]]] = []
    for w in words:
        cy = w[1] + w[3] / 2
        if lines:
            last = lines[-1]
            ly = statistics.mean(x[1] + x[3] / 2 for x in last)
            if abs(cy - ly) <= med_h * 0.6:
                last.append(w)
                continue
        lines.append([w])
    gaps = sorted(b[0] - (a[0] + a[2]) for ln in lines for a, b in zip(sorted(ln), sorted(ln)[1:]) if b[0] - (a[0] + a[2]) > 0)
    base_gap = gaps[int(len(gaps) * 0.3)] if gaps else char_w  # typical inter-word gap on this page
    out_lines = []
    left0 = min(w[0] for w in words)
    for ln in lines:
        ln.sort(key=lambda w: w[0])
        s, cursor = "", left0
        for (x, _, wd, _, text, _) in ln:
            gap = x - cursor
            if not s:
                pad = " " * min(14, max(0, round(gap / char_w)))
            else:
                pad = " " if gap <= max(2.2 * base_gap, 0.9 * char_w) else " " * min(14, max(3, round(gap / char_w)))
            s += pad + text
            cursor = x + wd
        out_lines.append(s.rstrip())
    return "\n".join(out_lines), round(statistics.mean(w[5] for w in words), 1), len(words)
