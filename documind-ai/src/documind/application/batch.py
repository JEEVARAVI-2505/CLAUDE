"""Background batch queue: pause / resume / cancel / retry, failure isolation."""

from __future__ import annotations

import logging
import queue
import threading
from dataclasses import dataclass
from typing import Callable

from ..domain.exceptions import DocuMindError, OperationCancelled
from .extraction_service import CancelToken, ExtractionPipeline

log = logging.getLogger("documind.batch")


@dataclass
class BatchEvent:
    doc_id: int
    kind: str  # queued | started | progress | done | failed | cancelled
    message: str = ""
    percent: int = 0


class BatchProcessor:
    def __init__(self, pipeline: ExtractionPipeline, on_event: Callable[[BatchEvent], None] | None = None) -> None:
        self.pipeline = pipeline
        self.on_event = on_event or (lambda e: None)
        self._q: queue.Queue[int | None] = queue.Queue()
        self._tokens: dict[int, CancelToken] = {}
        self._lock = threading.Lock()
        self._run = threading.Event()
        self._run.set()
        self._stop = False
        self._idle = threading.Event()
        self._idle.set()
        self._pending = 0
        self._thread = threading.Thread(target=self._worker, name="documind-batch", daemon=True)
        self._thread.start()

    # ---- control ---------------------------------------------------------------
    def enqueue(self, doc_ids: list[int]) -> None:
        for i in doc_ids:
            with self._lock:
                if i in self._tokens:
                    continue
                self._tokens[i] = CancelToken()
                self._pending += 1
                self._idle.clear()
            self._q.put(i)
            self._emit(BatchEvent(i, "queued"))

    def pause(self) -> None:
        self._run.clear()

    def resume(self) -> None:
        self._run.set()

    @property
    def paused(self) -> bool:
        return not self._run.is_set()

    def cancel(self, doc_id: int) -> None:
        with self._lock:
            t = self._tokens.get(doc_id)
        if t:
            t.cancel()

    def cancel_all(self) -> None:
        with self._lock:
            for t in self._tokens.values():
                t.cancel()
        self._run.set()  # let the worker drain cancelled jobs

    def retry(self, doc_id: int) -> None:
        self.enqueue([doc_id])

    def wait_idle(self, timeout: float | None = None) -> bool:
        return self._idle.wait(timeout)

    def shutdown(self) -> None:
        self._stop = True
        self.cancel_all()
        self._q.put(None)
        self._thread.join(timeout=5)

    # ---- worker -------------------------------------------------------------------
    def _emit(self, ev: BatchEvent) -> None:
        try:
            self.on_event(ev)
        except Exception:  # a UI callback must never kill the worker
            log.exception("batch listener failed")

    def _worker(self) -> None:
        while True:
            doc_id = self._q.get()
            if doc_id is None or self._stop:
                return
            self._run.wait()
            token = self._tokens.get(doc_id) or CancelToken()
            try:
                if token():
                    self._emit(BatchEvent(doc_id, "cancelled", "Cancelled"))
                    continue
                self._emit(BatchEvent(doc_id, "started", "Starting"))
                self.pipeline.process(doc_id, token, lambda s, p, d=doc_id: self._emit(BatchEvent(d, "progress", s, p)))
                self._emit(BatchEvent(doc_id, "done", "Ready for review", 100))
            except OperationCancelled:
                self._emit(BatchEvent(doc_id, "cancelled", "Cancelled"))
            except DocuMindError as e:
                self._emit(BatchEvent(doc_id, "failed", e.user_message))
            except Exception as e:
                log.exception("worker error")
                self._emit(BatchEvent(doc_id, "failed", f"Unexpected error ({type(e).__name__})"))
            finally:
                with self._lock:
                    self._tokens.pop(doc_id, None)
                    self._pending -= 1
                    if self._pending <= 0:
                        self._pending = 0
                        self._idle.set()
