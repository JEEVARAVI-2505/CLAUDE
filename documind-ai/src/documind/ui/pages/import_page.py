"""Import & queue: drag-and-drop, file/folder pickers, batch progress with pause/cancel/retry."""

from __future__ import annotations

from pathlib import Path

from PySide6.QtCore import Qt
from PySide6.QtWidgets import (QAbstractItemView, QCheckBox, QFileDialog, QHBoxLayout, QHeaderView, QProgressBar, QPushButton,
                               QTableWidget, QTableWidgetItem, QVBoxLayout, QWidget)

from ...application.batch import BatchEvent
from ...application.context import AppContext
from ...application.document_service import ImportResult
from ...domain.models import DocStatus
from ...infrastructure.repositories import SearchFilter
from ..widgets.common import EmptyState, label
from ..widgets.dropzone import DropZone, expand_paths
from ..workers import run_async

COLS = ["File", "Pages", "Status", "Progress", "Details"]
STATUS_TEXT = {"queued": "Queued", "started": "Processing", "progress": "Processing", "done": "Ready for review", "failed": "Failed",
               "cancelled": "Cancelled", "duplicate": "Duplicate, skipped", "rejected": "Not imported"}


class ImportPage(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx, self.win = ctx, win
        self.rows: dict[int, int] = {}
        lay = QVBoxLayout(self); lay.setContentsMargins(28, 22, 28, 22); lay.setSpacing(12)
        lay.addWidget(label("Import & Queue", "title"))
        self.drop = DropZone(); self.drop.set_limits_text(ctx.cfg.limits.max_file_mb)
        self.drop.files_dropped.connect(self.handle_paths); self.drop.browse_requested.connect(self.browse)
        lay.addWidget(self.drop)
        opts = QHBoxLayout()
        self.skip_dups = QCheckBox("Skip files I have already imported (same content)"); self.skip_dups.setChecked(True)
        b_files = QPushButton("Add files…"); b_files.clicked.connect(self.browse)
        b_dir = QPushButton("Add folder…"); b_dir.clicked.connect(self.browse_folder)
        opts.addWidget(self.skip_dups); opts.addStretch(); opts.addWidget(b_files); opts.addWidget(b_dir)
        lay.addLayout(opts)
        self.table = QTableWidget(0, len(COLS)); self.table.setHorizontalHeaderLabels(COLS)
        self.table.verticalHeader().setVisible(False); self.table.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.table.setEditTriggers(QAbstractItemView.NoEditTriggers); self.table.setAlternatingRowColors(True)
        h = self.table.horizontalHeader(); h.setSectionResizeMode(0, QHeaderView.Stretch); h.setSectionResizeMode(4, QHeaderView.Stretch)
        self.table.setColumnWidth(1, 60); self.table.setColumnWidth(2, 150); self.table.setColumnWidth(3, 150)
        h.setDefaultAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        self.table.setAccessibleName("Processing queue"); self.table.itemDoubleClicked.connect(lambda _i: self.open_selected())
        lay.addWidget(self.table, 1)
        self.empty = EmptyState("No files yet", "Files you add appear here with their progress. A file that cannot be read is listed with the reason.")
        lay.addWidget(self.empty, 1)
        bar = QHBoxLayout()
        self.summary = label("Queue is empty.", "muted")
        self.b_pause = QPushButton("Pause"); self.b_cancel = QPushButton("Cancel selected"); self.b_cancel_all = QPushButton("Cancel all")
        self.b_retry = QPushButton("Retry failed / cancelled"); self.b_clear = QPushButton("Clear finished"); self.b_open = QPushButton("Open in Review")
        self.b_open.setProperty("variant", "primary")
        for b, fn in ((self.b_pause, self.toggle_pause), (self.b_cancel, self.cancel_selected), (self.b_cancel_all, self.cancel_all),
                      (self.b_retry, self.retry), (self.b_clear, self.clear_finished), (self.b_open, self.open_selected)):
            b.clicked.connect(fn)
        bar.addWidget(self.summary, 1)
        for b in (self.b_pause, self.b_cancel, self.b_cancel_all, self.b_retry, self.b_clear, self.b_open):
            bar.addWidget(b)
        lay.addLayout(bar)
        self._loaded = False

    # ---- loading existing queue -------------------------------------------------------
    def refresh(self) -> None:
        if not self._loaded:
            self._loaded = True
            for st in ("queued", "failed"):
                for s in self.ctx.repo.search(SearchFilter(status=st, limit=200)):
                    if s.id not in self.rows:
                        self._add_row(s.id, s.file_name, s.pages, "queued" if st == "queued" else "failed", s.error or "")
        self._summary()

    def browse(self) -> None:
        files, _ = QFileDialog.getOpenFileNames(self, "Choose invoices or purchase orders", str(Path.home()),
                                                "Documents (*.pdf *.png *.jpg *.jpeg *.tif *.tiff);;All files (*)")
        if files:
            self.handle_paths(files)

    def browse_folder(self) -> None:
        d = QFileDialog.getExistingDirectory(self, "Choose a folder of documents", str(Path.home()))
        if d:
            self.handle_paths(expand_paths([d]))

    def handle_paths(self, paths: list[str]) -> None:
        if not paths:
            return
        self.win.notify(f"Checking {len(paths)} file(s)…")
        skip = self.skip_dups.isChecked()
        run_async(lambda: self.ctx.docs.import_files([Path(p) for p in paths], skip_duplicates=skip),
                  lambda res: (self.add_results(res), self.ctx.batch.enqueue([r.doc_id for r in res if r.status == "imported" and r.doc_id])),
                  lambda m: self.win.error("Import", m), parent=self)

    # ---- table ----------------------------------------------------------------------------------
    def _add_row(self, doc_id: int | None, name: str, pages: int, status: str, details: str) -> int:
        if doc_id is not None and doc_id in self.rows:  # one row per document, never duplicated
            r = self.rows[doc_id]
            self.table.item(r, 2).setText(STATUS_TEXT.get(status, status)); self.table.item(r, 2).setData(Qt.UserRole, status)
            self.table.item(r, 4).setText(details)
            return r
        r = self.table.rowCount()
        self.table.insertRow(r)
        self.table.setItem(r, 0, QTableWidgetItem(name)); self.table.setItem(r, 1, QTableWidgetItem(str(pages or "")))
        st = QTableWidgetItem(STATUS_TEXT.get(status, status)); st.setData(Qt.UserRole, status); self.table.setItem(r, 2, st)
        pb = QProgressBar(); pb.setRange(0, 100); pb.setValue(100 if status in ("done",) else 0); pb.setTextVisible(True); pb.setAccessibleName(f"Progress for {name}")
        self.table.setCellWidget(r, 3, pb)
        self.table.setItem(r, 4, QTableWidgetItem(details))
        self.table.item(r, 0).setData(Qt.UserRole, doc_id)
        if doc_id is not None:
            self.rows[doc_id] = r
        return r

    def add_results(self, results: list[ImportResult]) -> None:
        for r in results:
            name = Path(r.path).name
            if r.status == "imported" and r.doc_id:
                self._add_row(r.doc_id, name, int(r.message.split()[0]) if r.message[:1].isdigit() else 0, "queued", "")
            else:
                self._add_row(None, name, 0, r.status, r.message)
        n_bad = sum(1 for r in results if r.status != "imported")
        self.win.notify(f"{len(results) - n_bad} file(s) added" + (f", {n_bad} skipped or rejected - see Details." if n_bad else "."))
        self._summary()

    def on_event(self, ev: BatchEvent) -> None:
        r = self.rows.get(ev.doc_id)
        if r is None:
            rec = self.ctx.repo.get(ev.doc_id)
            r = self._add_row(ev.doc_id, rec.file_name, rec.pages, "queued", "")
        st = self.table.item(r, 2)
        st.setText(STATUS_TEXT.get(ev.kind, ev.kind)); st.setData(Qt.UserRole, ev.kind)
        pb: QProgressBar = self.table.cellWidget(r, 3)  # type: ignore[assignment]
        if ev.kind in ("progress", "done"):
            pb.setValue(ev.percent)
        elif ev.kind in ("failed", "cancelled"):
            pb.setValue(0)
        self.table.item(r, 4).setText(ev.message if ev.kind in ("progress", "failed", "done", "cancelled") else "")
        self._summary()

    def _statuses(self) -> list[str]:
        return [self.table.item(r, 2).data(Qt.UserRole) for r in range(self.table.rowCount())]

    def on_theme(self, t: dict[str, str]) -> None:
        self.drop.set_icon_color(t["primary"])

    def _summary(self) -> None:
        s = self._statuses()
        self.table.setVisible(bool(s)); self.empty.setVisible(not s)
        if not s:
            self.summary.setText("Queue is empty. Drop files above to begin."); return
        done = s.count("done"); fail = s.count("failed") + s.count("rejected"); act = s.count("queued") + s.count("started") + s.count("progress")
        self.summary.setText(f"{done} ready for review · {act} in progress/waiting · {fail} failed or not imported · {len(s)} total")
        self.b_pause.setText("Resume" if self.ctx.batch.paused else "Pause")

    # ---- actions -------------------------------------------------------------------------------------
    def toggle_pause(self) -> None:
        self.ctx.batch.resume() if self.ctx.batch.paused else self.ctx.batch.pause()
        self._summary()
        self.win.notify("Queue paused - the current document will finish first." if self.ctx.batch.paused else "Queue resumed.")

    def _selected_ids(self) -> list[int]:
        ids = []
        for idx in self.table.selectionModel().selectedRows():
            d = self.table.item(idx.row(), 0).data(Qt.UserRole)
            if d is not None:
                ids.append(d)
        return ids

    def cancel_selected(self) -> None:
        for i in self._selected_ids():
            self.ctx.batch.cancel(i)

    def cancel_all(self) -> None:
        self.ctx.batch.cancel_all()
        self.win.notify("Cancelling all pending documents…")

    def retry(self) -> None:
        ids = []
        for r in range(self.table.rowCount()):
            if self.table.item(r, 2).data(Qt.UserRole) in ("failed", "cancelled"):
                d = self.table.item(r, 0).data(Qt.UserRole)
                if d is not None:
                    self.ctx.repo.set_status(d, DocStatus.QUEUED)
                    self.table.item(r, 2).setText(STATUS_TEXT["queued"]); self.table.item(r, 2).setData(Qt.UserRole, "queued")
                    ids.append(d)
        self.ctx.batch.enqueue(ids)
        self.win.notify(f"Retrying {len(ids)} document(s)." if ids else "Nothing to retry.")

    def clear_finished(self) -> None:
        for r in reversed(range(self.table.rowCount())):
            if self.table.item(r, 2).data(Qt.UserRole) in ("done", "duplicate", "rejected"):
                d = self.table.item(r, 0).data(Qt.UserRole)
                self.table.removeRow(r)
                if d in self.rows:
                    del self.rows[d]
        self.rows = {self.table.item(r, 0).data(Qt.UserRole): r for r in range(self.table.rowCount()) if self.table.item(r, 0).data(Qt.UserRole) is not None}
        self._summary()

    def open_selected(self) -> None:
        ids = self._selected_ids()
        if not ids:
            self.win.show_page("review"); return
        self.win.open_review(ids[0])
