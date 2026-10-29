"""Importing files and permanently deleting documents."""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from pathlib import Path

from ..config import AppConfig
from ..domain.exceptions import DocuMindError, FileValidationError
from ..domain.models import DocKind, DocStatus, DocumentRecord
from ..infrastructure.database import Database
from ..infrastructure.file_storage import FileStore, preflight, sha256_file
from ..infrastructure.pdf_reader import open_source
from ..infrastructure.repositories import DocumentRepository

log = logging.getLogger("documind.import")


@dataclass
class ImportResult:
    path: str
    status: str  # imported | duplicate | rejected
    message: str = ""
    doc_id: int | None = None
    existing_ids: list[int] = field(default_factory=list)


class DocumentService:
    def __init__(self, cfg: AppConfig, repo: DocumentRepository, store: FileStore, db: Database) -> None:
        self.cfg, self.repo, self.store, self.db = cfg, repo, store, db

    def import_files(self, paths: list[Path], *, skip_duplicates: bool = True) -> list[ImportResult]:
        out: list[ImportResult] = []
        limit = self.cfg.limits.max_batch_files
        for n, p in enumerate(paths):
            if n >= limit:
                out.append(ImportResult(str(p), "rejected", f"Batch limit of {limit} files reached (Settings > Limits)."))
                continue
            out.append(self.import_one(Path(p), skip_duplicates=skip_duplicates))
        return out

    def import_one(self, path: Path, *, skip_duplicates: bool = True) -> ImportResult:
        try:
            ext, mime, size = preflight(path, self.cfg.limits)
            sha = sha256_file(path)
            existing = self.repo.find_by_sha(sha)
            if existing and skip_duplicates:
                ids = [e.id for e in existing]
                return ImportResult(str(path), "duplicate", f"Identical file already imported (document #{ids[0]}).", None, ids)
            src = open_source(path, mime, self.cfg.limits)  # validates readability, password, page count
            pages = src.page_count
            src.close()
            stored = self.store.store(path, sha, ext)
            rec = DocumentRecord(file_name=_display_name(path), sha256=sha, stored_path=str(stored), mime=mime, size_bytes=size,
                                 pages=pages, status=DocStatus.QUEUED, kind=DocKind.UNKNOWN)
            doc_id = self.repo.add(rec)
            return ImportResult(str(path), "imported", f"{pages} page(s)", doc_id, [e.id for e in existing])
        except FileValidationError as e:
            return ImportResult(str(path), "rejected", e.user_message)
        except DocuMindError as e:
            return ImportResult(str(path), "rejected", e.user_message)
        except Exception as e:  # never let one bad file abort the batch
            log.exception("unexpected import failure")
            return ImportResult(str(path), "rejected", f"Unexpected error while reading this file ({type(e).__name__}).")

    def delete_permanently(self, doc_id: int, actor: str = "user", reason: str = "user request") -> None:
        stored = self.repo.delete(doc_id, reason, actor)
        if stored and not self.repo.references_path(stored, -1):
            self.store.delete(stored, wipe=True)
        self.db.checkpoint()


def _display_name(p: Path) -> str:
    return p.name[:200]
