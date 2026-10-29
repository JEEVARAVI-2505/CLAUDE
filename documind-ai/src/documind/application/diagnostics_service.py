"""Customer-reviewable diagnostics (no document contents, no field values)."""

from __future__ import annotations

import platform
import shutil
import sys
from datetime import datetime
from importlib import metadata
from pathlib import Path

from .. import __version__
from ..config import AppConfig, Paths
from ..infrastructure.database import Database
from ..infrastructure.ocr_engine import TesseractEngine
from ..infrastructure.ollama_client import OllamaClient
from ..infrastructure.repositories import DocumentRepository
from ..logging_setup import redact

PKGS = ["PySide6-Essentials", "pydantic", "pypdf", "pypdfium2", "Pillow", "openpyxl"]


class DiagnosticsService:
    def __init__(self, cfg: AppConfig, paths: Paths, db: Database, repo: DocumentRepository, ocr: TesseractEngine) -> None:
        self.cfg, self.paths, self.db, self.repo, self.ocr = cfg, paths, db, repo, ocr

    def ai_status_text(self) -> str:
        try:
            st = OllamaClient(self.cfg.ai).check()
            return f"reachable={st.reachable} model_installed={st.model_installed} version={st.version} :: {st.message}"
        except Exception as e:
            return f"not checked: {getattr(e, 'user_message', type(e).__name__)}"

    def report(self, log_lines: int = 150) -> str:
        o = self.ocr.status(refresh=True)
        try:
            problems = self.db.integrity_check()
        except Exception as e:
            problems = [type(e).__name__]
        stats = self.repo.stats(None)
        free = shutil.disk_usage(self.paths.root).free / 1e9 if self.paths.root.exists() else 0
        vers = []
        for p in PKGS:
            try:
                vers.append(f"  {p} {metadata.version(p)}")
            except metadata.PackageNotFoundError:
                vers.append(f"  {p} (not found)")
        lines = [
            f"DocuMind AI Professional {__version__} diagnostics - {datetime.now():%Y-%m-%d %H:%M}",
            "This report contains NO document text or extracted values. Review it before sending to support.", "",
            f"OS: {platform.platform()}", f"Python: {sys.version.split()[0]}", "Packages:", *vers, "",
            f"Data folder: {self.paths.root}", f"Free disk space: {free:.1f} GB",
            f"Database schema version: {self.db.schema_version()} (latest {self.db.latest_version()})",
            f"Database integrity: {'ok' if not problems else '; '.join(problems[:3])}",
            f"Documents by status: { {k: v for k, v in stats.items() if k in ('queued','processing','needs_review','approved','rejected','failed')} }", "",
            f"OCR: available={o.available} path={o.path} version={o.version} languages={o.languages} missing={o.missing_languages} osd={o.has_osd}",
            f"OCR message: {o.message}", f"AI: enabled={self.cfg.ai.enabled} host={self.cfg.ai.host} model={self.cfg.ai.model}",
            f"AI status: {self.ai_status_text() if self.cfg.ai.enabled else 'disabled (rules-only mode)'}", "",
            "Settings summary: " + f"theme={self.cfg.theme} date_order={self.cfg.date_order} ocr_mode={self.cfg.ocr.mode} "
            f"limits={self.cfg.limits.max_file_mb}MB/{self.cfg.limits.max_pages}pages retention_days={self.cfg.retention.days}", "",
            f"--- last {log_lines} log lines (redacted) ---",
        ]
        lf = self.paths.logs / "documind.log"
        if lf.exists():
            try:
                tail = lf.read_text(encoding="utf-8", errors="replace").splitlines()[-log_lines:]
                lines += [redact(x) for x in tail]
            except OSError:
                lines.append("(log unreadable)")
        return "\n".join(lines)

    def export(self, dest: Path) -> Path:
        dest.write_text(self.report(), encoding="utf-8")
        return dest
