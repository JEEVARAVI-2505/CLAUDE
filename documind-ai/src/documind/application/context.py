"""Composition root: builds every service once and wires them together (no globals)."""

from __future__ import annotations

import logging
from dataclasses import dataclass
from pathlib import Path

from ..config import AppConfig, ConfigManager, Paths
from ..domain.templates import RulePack, load_pack, load_templates
from ..infrastructure.database import Database
from ..infrastructure.file_storage import FileStore
from ..infrastructure.ocr_engine import TesseractEngine
from ..infrastructure.repositories import DocumentRepository
from ..logging_setup import setup_logging
from ..resource import resource_path
from .backup_service import BackupService
from .batch import BatchEvent, BatchProcessor
from .diagnostics_service import DiagnosticsService
from .document_service import DocumentService
from .export_service import ExportService
from .extraction_service import ExtractionPipeline
from .retention_service import RetentionService
from .review_service import ReviewService

log = logging.getLogger("documind.app")


@dataclass
class AppContext:
    cfg: AppConfig
    cfg_mgr: ConfigManager
    paths: Paths
    db: Database
    repo: DocumentRepository
    store: FileStore
    pack: RulePack
    ocr: TesseractEngine
    pipeline: ExtractionPipeline
    batch: BatchProcessor
    docs: DocumentService
    review: ReviewService
    exports: ExportService
    backup: BackupService
    retention: RetentionService
    diagnostics: DiagnosticsService
    notices: list[str]

    @classmethod
    def create(cls, cfg_mgr: ConfigManager | None = None, on_batch_event=None) -> "AppContext":
        cfg_mgr = cfg_mgr or ConfigManager()
        cfg = cfg_mgr.load()
        notices = list(cfg_mgr.notices)
        paths = cfg_mgr.resolve_paths(cfg)
        paths.ensure()
        setup_logging(paths.logs)
        db = Database(paths.db)
        db.migrate(paths.backups)
        db.require_healthy()
        repo = DocumentRepository(db)
        stuck = repo.reset_stuck()
        if stuck:
            notices.append(f"{stuck} document(s) were interrupted last time and are back in the queue.")
        store = FileStore(paths)
        store.cleanup_temp()
        pack = load_pack(resource_path("rules", "labels_default.json"))
        pack.templates, problems = load_templates(resource_path("templates"), paths.templates)
        notices += [f"Template problem: {p}" for p in problems]
        ocr = TesseractEngine(cfg.ocr, paths.temp)
        pipeline = ExtractionPipeline(cfg, repo, store, ocr, pack)
        batch = BatchProcessor(pipeline, on_batch_event)
        docs = DocumentService(cfg, repo, store, db)
        ctx = cls(cfg, cfg_mgr, paths, db, repo, store, pack, ocr, pipeline, batch, docs, ReviewService(cfg, repo),
                  ExportService(cfg, repo, paths), BackupService(cfg, paths, db), RetentionService(cfg, repo, docs, store),
                  DiagnosticsService(cfg, paths, db, repo, ocr), notices)
        log.info("DocuMind started; schema v%s", db.schema_version())
        return ctx

    def update_config(self, new: AppConfig, save: bool = True) -> None:
        """Apply settings in place so every service sees them; rebuild the OCR engine."""
        for name in AppConfig.model_fields:
            setattr(self.cfg, name, getattr(new, name))
        self.ocr = TesseractEngine(self.cfg.ocr, self.paths.temp)
        self.pipeline.ocr = self.ocr
        self.diagnostics.ocr = self.ocr
        if save:
            self.cfg_mgr.save(self.cfg)

    def shutdown(self) -> None:
        self.batch.shutdown()
        self.db.close()
