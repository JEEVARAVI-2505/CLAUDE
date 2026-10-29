"""Human review: preview + editable fields + line items + checks + audit history."""

from __future__ import annotations

from PySide6.QtCore import Qt
from PySide6.QtGui import QColor, QKeySequence, QShortcut
from PySide6.QtWidgets import (QComboBox, QFrame, QHBoxLayout, QInputDialog, QLabel, QListWidget, QListWidgetItem, QMessageBox,
                               QPushButton, QSplitter, QTableWidget, QTableWidgetItem, QTabWidget, QVBoxLayout, QWidget)

from ...application.context import AppContext
from ...domain.exceptions import ApprovalBlockedError, DocuMindError
from ...domain.models import DocKind, DocStatus, DocumentRecord
from ...domain.schemas import SCALAR_BY_NAME
from ...infrastructure.repositories import SearchFilter
from ..icons import make_icon
from ..widgets.common import Chip, EmptyState, label
from ..widgets.field_form import FieldsPanel
from ..widgets.line_table import LineTable
from ..widgets.preview import PageViewer

FILTERS = [("Needs review", "needs_review"), ("Failed", "failed"), ("Approved", "approved"), ("Rejected", "rejected"), ("All", "")]
DOT = {"needs_review": "#D97706", "approved": "#16A34A", "failed": "#DC2626", "rejected": "#94A3B8", "queued": "#6366F1", "processing": "#6366F1"}
SEV_ICON = {"error": "error", "warning": "warn", "info": "help"}


class ReviewPage(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx, self.win = ctx, win
        self.rec: DocumentRecord | None = None
        self.tokens: dict[str, str] = {}
        root = QVBoxLayout(self); root.setContentsMargins(20, 16, 20, 16); root.setSpacing(10)
        top = QHBoxLayout()
        top.addWidget(label("Review", "title")); top.addStretch()
        self.safety = label("Machine-extracted values can be wrong. Check every field against the page before approving.", "small")
        top.addWidget(self.safety)
        root.addLayout(top)

        self.split = QSplitter(Qt.Horizontal)
        root.addWidget(self.split, 1)
        # -- left: list
        left = QWidget(); ll = QVBoxLayout(left); ll.setContentsMargins(0, 0, 8, 0)
        self.filter = QComboBox(); self.filter.setAccessibleName("Filter documents by status")
        for t, v in FILTERS:
            self.filter.addItem(t, v)
        self.filter.currentIndexChanged.connect(lambda _i: self.reload_list())
        self.list = QListWidget(); self.list.setAccessibleName("Documents to review")
        self.list.setTextElideMode(Qt.ElideRight); self.list.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.list.currentItemChanged.connect(self._picked)
        ll.addWidget(self.filter); ll.addWidget(self.list, 1)
        self.split.addWidget(left)
        # -- middle: preview
        mid = QWidget(); ml = QVBoxLayout(mid); ml.setContentsMargins(4, 0, 4, 0)
        self.viewer = PageViewer(); ml.addWidget(self.viewer, 1)
        self.info = label("", "small", wrap=True); ml.addWidget(self.info)
        self.split.addWidget(mid)
        # -- right: editor
        right = QWidget(); rl = QVBoxLayout(right); rl.setContentsMargins(8, 0, 0, 0); rl.setSpacing(8)
        self.title = label("Select a document", "h2"); self.title.setWordWrap(True)
        rl.addWidget(self.title)
        chips = QHBoxLayout()
        self.chip_status = Chip("", "neutral"); self.chip_issues = Chip("", "neutral"); self.chip_mode = Chip("", "neutral")
        self.kind = QComboBox(); self.kind.setAccessibleName("Document type")
        for k in DocKind:
            self.kind.addItem(k.value.replace("_", " ").title(), k.value)
        self.kind.activated.connect(self._kind_changed)
        for w in (self.chip_status, self.chip_issues, self.chip_mode):
            chips.addWidget(w)
        chips.addStretch()
        rl.addLayout(chips)
        kr = QHBoxLayout(); kr.addWidget(label("Document type", "muted")); kr.addWidget(self.kind, 1)
        rl.addLayout(kr)
        self.fail_banner = QFrame(); self.fail_banner.setProperty("banner", "danger"); self.fail_banner.hide()
        fb = QHBoxLayout(self.fail_banner); self.fail_text = QLabel(); self.fail_text.setWordWrap(True)
        self.retry = QPushButton("Retry"); self.retry.clicked.connect(self.rerun)
        fb.addWidget(self.fail_text, 1); fb.addWidget(self.retry)
        rl.addWidget(self.fail_banner)
        self.tabs = QTabWidget(); rl.addWidget(self.tabs, 1)
        self.fields = FieldsPanel(ctx.cfg.custom_fields)
        self.fields.edited.connect(self._edit_field); self.fields.reverted.connect(self._revert); self.fields.jump.connect(self.viewer.go_to_page_number)
        self.tabs.addTab(self.fields, "Fields")
        lw = QWidget(); lv = QVBoxLayout(lw); lv.setContentsMargins(6, 6, 6, 6)
        self.lines = LineTable(); self.lines.cell_edited.connect(self._edit_cell); self.lines.jump.connect(self.viewer.go_to_page_number)
        lb = QHBoxLayout(); self.add_line = QPushButton("Add line"); self.del_line = QPushButton("Remove selected line")
        self.add_line.clicked.connect(self._add_line); self.del_line.clicked.connect(self._del_line)
        lb.addWidget(self.add_line); lb.addWidget(self.del_line); lb.addStretch()
        lv.addWidget(self.lines, 1); lv.addLayout(lb)
        self.tabs.addTab(lw, "Line items")
        self.checks = QListWidget(); self.checks.setAccessibleName("Validation checks")
        self.checks.itemActivated.connect(self._check_clicked); self.checks.itemClicked.connect(self._check_clicked)
        self.tabs.addTab(self.checks, "Checks")
        self.audit = QTableWidget(0, 5); self.audit.setHorizontalHeaderLabels(["When", "Who", "Action", "Field", "Change"])
        self.audit.horizontalHeader().setStretchLastSection(True); self.audit.verticalHeader().setVisible(False)
        self.audit.setEditTriggers(QTableWidget.NoEditTriggers); self.audit.setAccessibleName("Audit history")
        self.tabs.addTab(self.audit, "History")
        act = QHBoxLayout()
        self.b_approve = QPushButton("Approve"); self.b_approve.setProperty("variant", "primary"); self.b_approve.setToolTip("Approve (Ctrl+Enter)")
        self.b_reject = QPushButton("Reject"); self.b_reject.setProperty("variant", "danger")
        self.b_reopen = QPushButton("Re-open"); self.b_rerun = QPushButton("Re-run")
        self.b_rerun.setToolTip("Discard this extraction and run it again")
        self.b_prev = QPushButton("‹"); self.b_next = QPushButton("›")
        self.b_prev.setToolTip("Previous document (Alt+Up)"); self.b_next.setToolTip("Next document (Alt+Down)")
        self.b_prev.setAccessibleName("Previous document"); self.b_next.setAccessibleName("Next document")
        for b, fn in ((self.b_approve, self.approve), (self.b_reject, self.reject), (self.b_reopen, self.reopen), (self.b_rerun, self.rerun),
                      (self.b_prev, lambda: self.step(-1)), (self.b_next, lambda: self.step(1))):
            b.clicked.connect(fn)
        act.addWidget(self.b_prev); act.addWidget(self.b_next); act.addStretch()
        for b in (self.b_rerun, self.b_reopen, self.b_reject, self.b_approve):
            act.addWidget(b)
        rl.addLayout(act)
        self.split.addWidget(right)
        self.split.setSizes([240, 420, 600]); self.split.setStretchFactor(0, 0); self.split.setStretchFactor(1, 2); self.split.setStretchFactor(2, 3)
        self.empty = EmptyState("Nothing to review yet", "Import documents and they appear here once extraction finishes.")
        self.empty.hide(); root.addWidget(self.empty)
        QShortcut(QKeySequence("Ctrl+Return"), self, activated=self.approve)
        QShortcut(QKeySequence("Alt+Down"), self, activated=lambda: self.step(1))
        QShortcut(QKeySequence("Alt+Up"), self, activated=lambda: self.step(-1))
        self._show(None)

    def on_theme(self, t: dict[str, str]) -> None:
        self.tokens = t
        self.lines.set_colors(t)
        if self.rec:
            self.lines.set_record(self.rec, self._editable())

    # ---- list ---------------------------------------------------------------------
    def refresh(self) -> None:
        keep = self.rec.id if self.rec else None
        self.reload_list(select=keep)

    def reload_list(self, select: int | None = None) -> None:
        st = self.filter.currentData()
        rows = self.ctx.repo.search(SearchFilter(status=st or None, limit=1000))
        rows = [r for r in rows if r.status not in ("queued", "processing")] if not st else rows
        self.list.blockSignals(True)
        self.list.clear()
        for r in rows:
            it = QListWidgetItem(f"{r.supplier or r.file_name}\n{r.doc_number or '-'}  ·  {r.total or '-'} {r.currency or ''}"
                                 + (f"   {r.errors} error(s)" if r.errors else "") + (f"   {r.warnings} warning(s)" if r.warnings else ""))
            it.setData(Qt.UserRole, r.id)
            it.setForeground(QColor(self.tokens.get("text", "#161A36")))
            it.setToolTip(f"{r.file_name} · {r.status.replace('_', ' ')}")
            self.list.addItem(it)
        self.list.blockSignals(False)
        self.empty.setVisible(self.list.count() == 0 and not st)
        if select is not None:
            self.select_document(select)
        elif self.list.count() and self.rec is None:
            self.list.setCurrentRow(0)

    def select_document(self, doc_id: int) -> None:
        for i in range(self.list.count()):
            if self.list.item(i).data(Qt.UserRole) == doc_id:
                self.list.setCurrentRow(i)
                return
        self.filter.setCurrentIndex(len(FILTERS) - 1)
        self.reload_list()
        for i in range(self.list.count()):
            if self.list.item(i).data(Qt.UserRole) == doc_id:
                self.list.setCurrentRow(i)
                return

    def step(self, d: int) -> None:
        r = self.list.currentRow() + d
        if 0 <= r < self.list.count():
            self.list.setCurrentRow(r)

    def _picked(self, cur, _prev) -> None:
        if cur is None:
            self._show(None)
            return
        try:
            self._show(self.ctx.repo.get(cur.data(Qt.UserRole)), load_preview=True)
        except DocuMindError as e:
            self.win.notify(e.user_message)

    # ---- display ---------------------------------------------------------------------
    def _editable(self) -> bool:
        return bool(self.rec and self.rec.status in (DocStatus.NEEDS_REVIEW, DocStatus.REJECTED))

    def _show(self, rec: DocumentRecord | None, load_preview: bool = False) -> None:
        self.rec = rec
        has = rec is not None
        for w in (self.b_approve, self.b_reject, self.b_reopen, self.b_rerun, self.b_prev, self.b_next, self.kind, self.add_line, self.del_line):
            w.setEnabled(has)
        if rec is None:
            self.title.setText("Select a document")
            self.viewer.clear()
            self.fields.set_record(None, False); self.lines.set_record(None, False); self.checks.clear(); self.audit.setRowCount(0)
            for c in (self.chip_status, self.chip_issues, self.chip_mode):
                c.hide()
            self.fail_banner.hide(); self.info.setText("")
            return
        editable = self._editable()
        if load_preview:
            self.viewer.load(rec.stored_path, rec.mime, self.ctx.cfg.limits)
        self.title.setText(f"{rec.get('supplier_name') or rec.file_name}  ·  {rec.get('invoice_number') or rec.get('po_number') or 'no number'}")
        txt, kind = {"needs_review": ("Needs review", "warning"), "approved": ("Approved", "success"), "failed": ("Failed", "danger"),
                     "rejected": ("Rejected", "neutral"), "queued": ("Queued", "neutral"), "processing": ("Processing", "info")}[rec.status.value]
        self.chip_status.set(txt, kind); self.chip_status.show()
        e, w = rec.error_count, rec.warning_count
        self.chip_issues.set("No issues" if not (e or w) else f"{e} error(s) · {w} warning(s)", "success" if not (e or w) else "danger" if e else "warning")
        self.chip_issues.show()
        self.chip_mode.set("AI-assisted" if rec.info.mode == "rules+ai" else "Rules-only", "primary"); self.chip_mode.show()
        self.kind.setCurrentIndex(max(0, self.kind.findData(rec.kind.value)))
        self.fail_banner.setVisible(rec.status == DocStatus.FAILED)
        self.fail_text.setText(f"This file could not be processed: {rec.error or 'unknown error'}")
        self.fields.set_record(rec, editable)
        self.lines.set_record(rec, editable)
        self.checks.clear()
        for i in sorted(rec.issues, key=lambda i: {"error": 0, "warning": 1, "info": 2}[i.severity.value]):
            col = {"error": "danger", "warning": "warning", "info": "primary"}[i.severity.value]
            it = QListWidgetItem(make_icon(SEV_ICON[i.severity.value], self.tokens.get(col, "#AE2A22"), 16), i.message)
            it.setData(Qt.UserRole, (i.field, i.row))
            self.checks.addItem(it)
        if not rec.issues:
            self.checks.addItem("All arithmetic and data checks passed.")
        self.tabs.setTabText(2, f"Checks ({len(rec.issues)})")
        self.tabs.setTabText(1, f"Line items ({len(rec.items)})")
        self._audit(rec.id)
        meth = {"embedded": "embedded PDF text", "ocr": "OCR", "mixed": "embedded text + OCR", "none": "no text"}[rec.info.text_method]
        parts = [f"Text source: {meth}", f"{rec.pages} page(s)"]
        if rec.info.ocr_score is not None:
            parts.append(f"OCR score {rec.info.ocr_score:.0f}/100 (engine's own score, not a probability)")
        if rec.info.template:
            parts.append(f"template: {rec.info.template}")
        self.info.setText(" · ".join(parts + rec.info.warnings))
        self.b_approve.setVisible(rec.status == DocStatus.NEEDS_REVIEW)
        self.b_reject.setVisible(rec.status in (DocStatus.NEEDS_REVIEW, DocStatus.FAILED))
        self.b_reopen.setVisible(rec.status in (DocStatus.APPROVED, DocStatus.REJECTED))
        self.b_rerun.setVisible(rec.status != DocStatus.APPROVED)
        self.add_line.setEnabled(editable); self.del_line.setEnabled(editable); self.kind.setEnabled(editable)

    def _audit(self, doc_id) -> None:
        rows = self.ctx.repo.audit_trail(doc_id)
        self.audit.setRowCount(len(rows))
        for r, a in enumerate(rows):
            change = ""
            if a["old_value"] is not None or a["new_value"] is not None:
                change = f"{a['old_value'] or '∅'}  →  {a['new_value'] or '∅'}"
            elif a["note"]:
                change = a["note"]
            vals = [a["ts"][:16].replace("T", " "), a["actor"], a["action"].replace("_", " "),
                    (a["field"] or "") + (f" (line {a['row_index'] + 1})" if a["row_index"] is not None else ""), change]
            for c, v in enumerate(vals):
                self.audit.setItem(r, c, QTableWidgetItem(v))
        self.audit.resizeColumnsToContents()

    # ---- edits ------------------------------------------------------------------------------
    def _do(self, fn, *a) -> bool:
        try:
            self.rec = fn(*a)
        except DocuMindError as e:
            self.win.notify(e.user_message)
            if self.rec:
                self._show(self.ctx.repo.get(self.rec.id))
            return False
        self._show(self.rec)
        self.reload_list_keep()
        return True

    def reload_list_keep(self) -> None:
        cur = self.list.currentRow()
        rid = self.rec.id if self.rec else None
        self.list.blockSignals(True)
        self.reload_list()
        for i in range(self.list.count()):
            if self.list.item(i).data(Qt.UserRole) == rid:
                self.list.setCurrentRow(i)
        self.list.blockSignals(False)

    def _edit_field(self, name: str, text: str) -> None:
        if self.rec:
            self._do(self.ctx.review.correct_field, self.rec.id, name, text)

    def _revert(self, name: str) -> None:
        if self.rec:
            self._do(self.ctx.review.revert_field, self.rec.id, name)

    def _edit_cell(self, row: int, name: str, text: str) -> None:
        if self.rec:
            self._do(self.ctx.review.correct_cell, self.rec.id, row, name, text)

    def _add_line(self) -> None:
        if self.rec:
            self._do(self.ctx.review.add_line, self.rec.id)

    def _del_line(self) -> None:
        if self.rec and self.lines.currentRow() >= 0:
            self._do(self.ctx.review.delete_line, self.rec.id, self.lines.currentRow())

    def _kind_changed(self, _i: int) -> None:
        if self.rec:
            self._do(self.ctx.review.set_kind, self.rec.id, DocKind(self.kind.currentData()))

    def _check_clicked(self, it: QListWidgetItem) -> None:
        data = it.data(Qt.UserRole)
        if not data:
            return
        field, row = data
        if row is not None:
            self.tabs.setCurrentIndex(1)
            self.lines.selectRow(row)
        elif field:
            self.tabs.setCurrentIndex(0)
            self.fields.focus_field(field)

    # ---- decisions ------------------------------------------------------------------------------
    def approve(self) -> None:
        if not self.rec or self.rec.status != DocStatus.NEEDS_REVIEW:
            return
        nxt = self.list.currentRow()
        try:
            self.ctx.review.approve(self.rec.id)
        except ApprovalBlockedError as e:
            reason, ok = QInputDialog.getText(self, "Approve despite errors?", f"{e.user_message}\n\nIf you are sure the document itself is correct, "
                                              "write why (it is saved in the audit history):")
            if not ok or not reason.strip():
                return
            self._do(self.ctx.review.approve, self.rec.id, "user", reason)
        except DocuMindError as e:
            self.win.notify(e.user_message); return
        self.win.notify("Approved")
        self.reload_list()
        if self.list.count():
            self.list.setCurrentRow(min(nxt, self.list.count() - 1))
        self.win.pages["dashboard"].refresh()

    def reject(self) -> None:
        if not self.rec:
            return
        reason, ok = QInputDialog.getText(self, "Reject document", "Reason (optional, saved in history):")
        if ok:
            self._do(self.ctx.review.reject, self.rec.id, reason)

    def reopen(self) -> None:
        if self.rec:
            self._do(self.ctx.review.reopen, self.rec.id)

    def rerun(self) -> None:
        if not self.rec:
            return
        if any(f.origin == "corrected" for f in self.rec.fields.values()):
            if QMessageBox.question(self, "Re-run extraction", "Your corrections on this document will be replaced by a fresh extraction. Continue?") != QMessageBox.Yes:
                return
        self.ctx.repo.audit(self.rec.id, "user", "rerun_requested")
        self.ctx.repo.set_status(self.rec.id, DocStatus.QUEUED)
        self.ctx.batch.enqueue([self.rec.id])
        self.win.notify("Re-running extraction…")
        self.win.show_page("import")
