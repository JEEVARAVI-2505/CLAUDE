"""Retention policy: scheduled permanent deletion and optional source-file removal."""

from __future__ import annotations

import logging
from dataclasses import dataclass

from ..config import AppConfig
from ..infrastructure.file_storage import FileStore
from ..infrastructure.repositories import DocumentRepository
from .document_service import DocumentService

log = logging.getLogger("documind.retention")
PURGEABLE = ("approved", "rejected", "failed")  # never touches documents still awaiting review


@dataclass
class RetentionReport:
    candidates: int
    deleted: int
    source_files_removed: int


class RetentionService:
    def __init__(self, cfg: AppConfig, repo: DocumentRepository, docs: DocumentService, store: FileStore) -> None:
        self.cfg, self.repo, self.docs, self.store = cfg, repo, docs, store

    def preview(self) -> list[int]:
        d = self.cfg.retention.days
        return self.repo.older_than(d, PURGEABLE) if d > 0 else []

    def apply(self) -> RetentionReport:
        d = self.cfg.retention.days
        if d <= 0:
            return RetentionReport(0, 0, 0)
        ids = self.preview()
        deleted = removed = 0
        for i in ids:
            try:
                if self.cfg.retention.keep_source_files:
                    self.docs.delete_permanently(i, actor="system", reason=f"retention {d} days")
                    deleted += 1
                else:
                    rec = self.repo.get(i)
                    if self.store.delete(rec.stored_path):
                        removed += 1
            except Exception:
                log.exception("retention step failed for one document")
        if deleted:
            self.docs.db.vacuum()
        log.info("retention: %d deleted, %d source files removed", deleted, removed)
        return RetentionReport(len(ids), deleted, removed)

    def delete_all_data(self) -> int:
        """Permanently delete every document (used by 'Erase all data')."""
        n = 0
        for s in self.repo.search(__import__("documind.infrastructure.repositories", fromlist=["SearchFilter"]).SearchFilter(limit=5000)):
            self.docs.delete_permanently(s.id, reason="erase all")
            n += 1
        self.docs.db.vacuum()
        return n
