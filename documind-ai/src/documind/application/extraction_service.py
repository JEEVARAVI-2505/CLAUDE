"""End-to-end processing of one stored document: text -> (OCR) -> rules -> (AI) -> validation -> save."""

from __future__ import annotations

import logging
import threading
from typing import Callable

from ..config import AppConfig
from ..domain.exceptions import AiError, DocuMindError, OcrError, OcrUnavailableError, OperationCancelled
from ..domain.models import DocStatus, DocumentRecord, ExtractionInfo, PageText
from ..domain.rules_extractor import extract_with_rules
from ..domain.schemas import SYSTEM_PROMPT, build_llm_model
from ..domain.templates import RulePack
from ..domain.validation import ValidationOptions, validate
from ..infrastructure.file_storage import FileStore
from ..infrastructure.ocr_engine import TesseractEngine
from ..infrastructure.ollama_client import OllamaClient
from ..infrastructure.pdf_reader import open_source
from ..infrastructure.repositories import DocumentRepository
from .issues import duplicate_issues
from .llm_extraction import merge, parse_payload

log = logging.getLogger("documind.pipeline")
Progress = Callable[[str, int], None]


class CancelToken:
    def __init__(self) -> None:
        self._e = threading.Event()

    def cancel(self) -> None:
        self._e.set()

    def __call__(self) -> bool:
        return self._e.is_set()

    def raise_if_cancelled(self) -> None:
        if self._e.is_set():
            raise OperationCancelled()


def validation_options(cfg: AppConfig) -> ValidationOptions:
    return ValidationOptions(suppress_ambiguous_dates=cfg.suppress_ambiguous_date_warnings)


class ExtractionPipeline:
    def __init__(self, cfg: AppConfig, repo: DocumentRepository, store: FileStore, ocr: TesseractEngine, pack: RulePack) -> None:
        self.cfg, self.repo, self.store, self.ocr, self.pack = cfg, repo, store, ocr, pack

    # ---- text ---------------------------------------------------------------------
    def gather_text(self, rec: DocumentRecord, cancel: CancelToken, progress: Progress) -> tuple[list[PageText], ExtractionInfo]:
        info = ExtractionInfo()
        src = open_source(self.store.resolve(rec.stored_path), rec.mime, self.cfg.limits)
        pages: list[PageText] = []
        scores: list[float] = []
        try:
            n = src.page_count
            info.pages = n
            ocr_cfg = self.cfg.ocr
            skipped_ocr = 0
            for i in range(n):
                cancel.raise_if_cancelled()
                progress(f"Reading page {i + 1} of {n}", 5 + int(45 * i / n))
                text = src.embedded_text(i) if ocr_cfg.mode != "always" else ""
                need_ocr = ocr_cfg.mode == "always" or len(text.strip()) < ocr_cfg.min_embedded_chars
                if need_ocr and ocr_cfg.mode != "off":
                    st = self.ocr.status()
                    if st.available:
                        progress(f"Running OCR on page {i + 1} of {n}", 5 + int(45 * i / n))
                        try:
                            res = self.ocr.ocr_image(src.image(i, ocr_cfg.dpi), cancel)
                        except OcrError as e:
                            info.warnings.append(f"OCR failed on page {i + 1}: {e.user_message}")
                            res = None
                        if res is not None and res.text.strip():
                            pages.append(PageText(page=i + 1, text=res.text, source="ocr", ocr_score=res.score))
                            info.ocr_pages.append(i + 1)
                            if res.score is not None:
                                scores.append(res.score)
                                if res.score < 60:
                                    info.warnings.append(f"Page {i + 1} was hard to read (OCR score {res.score:.0f}/100, uncalibrated). Check values carefully.")
                            if res.rotation:
                                info.warnings.append(f"Page {i + 1} was rotated {res.rotation} degrees automatically.")
                            continue
                    else:
                        skipped_ocr += 1
                if text.strip():
                    pages.append(PageText(page=i + 1, text=text, source="embedded"))
            if skipped_ocr:
                info.warnings.append(f"{skipped_ocr} page(s) have no text layer and OCR is unavailable: {self.ocr.status().message}")
        finally:
            src.close()
        emb = any(p.source == "embedded" for p in pages)
        ocr_used = any(p.source == "ocr" for p in pages)
        info.text_method = "mixed" if emb and ocr_used else "ocr" if ocr_used else "embedded" if emb else "none"
        info.ocr_score = round(sum(scores) / len(scores), 1) if scores else None
        if not pages:
            if skipped_ocr or ocr_cfg.mode == "off":
                raise OcrUnavailableError("This looks like a scanned document with no text layer, and OCR is not available. "
                                          "Install Tesseract (Help > Install OCR) or enable OCR in Settings.")
            raise OcrError("No readable text was found in this document.")
        return pages, info

    # ---- AI -----------------------------------------------------------------------------
    def run_ai(self, pages: list[PageText], cancel: CancelToken, info: ExtractionInfo):
        client = OllamaClient(self.cfg.ai)
        body = "\n\n".join(f"=== PAGE {p.page} ===\n{p.text}" for p in pages)
        if len(body) > self.cfg.ai.max_chars:
            body = body[: self.cfg.ai.max_chars]
            info.warnings.append("The document is long; only the first part was sent to the AI model.")
        schema = build_llm_model(self.cfg.custom_fields).model_json_schema()
        user = "Extract the fields from the document between the markers. Return JSON only.\n<document>\n" + body + "\n</document>"
        last: Exception | None = None
        for attempt in range(self.cfg.ai.retries + 1):
            cancel.raise_if_cancelled()
            raw = client.chat_json(SYSTEM_PROMPT, user, schema, cancel)
            try:
                return parse_payload(raw, self.cfg.custom_fields)
            except AiError as e:
                last = e
                user += "\n\nYour previous answer was invalid JSON for the schema. Return only a valid JSON object."
        assert last is not None
        raise last

    # ---- main ------------------------------------------------------------------------------
    def process(self, doc_id: int, cancel: CancelToken | None = None, progress: Progress | None = None,
                on_status: Callable[[DocStatus, str | None], None] | None = None) -> DocumentRecord:
        cancel = cancel or CancelToken()
        progress = progress or (lambda s, p: None)
        rec = self.repo.get(doc_id)
        self.repo.set_status(doc_id, DocStatus.PROCESSING)
        try:
            pages, info = self.gather_text(rec, cancel, progress)
            progress("Extracting fields", 55)
            res = extract_with_rules(pages, self.pack, default_order=self.cfg.date_order, custom=self.cfg.custom_fields)
            info.template, info.date_order = res.template, res.date_order
            kind, fields, items = res.kind, res.fields, res.items
            info.mode = "rules"
            if self.cfg.ai.enabled:
                progress("Asking the local AI model", 65)
                try:
                    payload = self.run_ai(pages, cancel, info)
                    kind, fields, items, notes = merge(payload, res, pages, res.date_order, self.cfg.custom_fields, validation_options(self.cfg))
                    info.warnings += notes
                    info.mode, info.model = "rules+ai", self.cfg.ai.model
                except OperationCancelled:
                    raise
                except DocuMindError as e:
                    info.warnings.append(f"AI step skipped, rules-only result shown: {e.user_message}")
            if "currency" not in fields and self.cfg.default_currency:
                from ..domain.rules_fields import fv
                fields["currency"] = fv("currency", self.cfg.default_currency, None, None, ["template_default"])
            progress("Checking arithmetic", 85)
            issues = validate(kind, fields, items, validation_options(self.cfg))
            rec.fields, rec.items, rec.kind, rec.info, rec.pages = fields, items, kind, info, info.pages
            rec.issues = issues + duplicate_issues(self.repo, rec)
            rec.status, rec.error = DocStatus.NEEDS_REVIEW, None
            self.repo.save_record(rec, processed=True)
            self.repo.audit(doc_id, "system", "extracted", note=f"{info.mode}; {info.text_method}")
            progress("Done", 100)
            if on_status:
                on_status(rec.status, None)
            return rec
        except OperationCancelled:
            self.repo.set_status(doc_id, DocStatus.QUEUED)
            raise
        except DocuMindError as e:
            log.warning("document %s failed: %s", doc_id, e.code)
            self._fail(doc_id, e.user_message)
            raise
        except Exception as e:  # unexpected: record and keep the batch alive
            log.exception("unexpected failure on document %s", doc_id)
            self._fail(doc_id, "An unexpected error occurred while processing this file. See Help > Export diagnostics.")
            raise DocuMindError("Unexpected processing error.", detail=type(e).__name__) from e

    def _fail(self, doc_id: int, msg: str) -> None:
        self.repo.set_status(doc_id, DocStatus.FAILED, msg)
