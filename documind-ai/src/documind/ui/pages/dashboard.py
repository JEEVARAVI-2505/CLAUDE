"""Overview: what needs doing now, key figures, recent activity, system readiness."""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

from PySide6.QtCore import Qt, Signal
from PySide6.QtGui import QColor
from PySide6.QtWidgets import (QComboBox, QFrame, QGridLayout, QHBoxLayout, QLabel, QListWidget, QListWidgetItem, QPushButton,
                               QScrollArea, QVBoxLayout, QWidget)

from ...application.context import AppContext
from ...infrastructure.repositories import SearchFilter
from ..widgets.charts import BarChart, StackedBar
from ..widgets.common import Card, EmptyState, MetricStrip, label

PERIODS = [("Last 7 days", 7), ("Last 30 days", 30), ("Last 90 days", 90), ("All time", 0)]
STATUS_ORDER = [("approved", "Approved", "success"), ("needs_review", "Needs review", "warning"), ("failed", "Failed", "danger"),
                ("queued", "Queued or processing", "info"), ("rejected", "Rejected", "muted")]


class DashboardPage(QWidget):
    import_requested = Signal()
    demo_requested = Signal()
    review_requested = Signal(object)

    def __init__(self, ctx: AppContext) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx = ctx
        self.tokens: dict[str, str] = {}
        outer = QVBoxLayout(self); outer.setContentsMargins(0, 0, 0, 0)
        scroll = QScrollArea(); scroll.setWidgetResizable(True); outer.addWidget(scroll)
        body = QWidget(); body.setObjectName("Page"); scroll.setWidget(body)
        lay = QVBoxLayout(body); lay.setContentsMargins(24, 20, 24, 24); lay.setSpacing(14)

        head = QHBoxLayout()
        head.addWidget(label("Overview", "title")); head.addStretch()
        head.addWidget(label("Period", "muted"))
        self.period = QComboBox(); self.period.setAccessibleName("Summary period")
        for t, d in PERIODS:
            self.period.addItem(t, d)
        self.period.setCurrentIndex(1)
        self.period.currentIndexChanged.connect(lambda _i: self.refresh())
        head.addWidget(self.period)
        lay.addLayout(head)

        # next action
        self.next_card = Card()
        row = QHBoxLayout(); row.setSpacing(14)
        col = QVBoxLayout(); col.setSpacing(2)
        self.next_title = label("", "h2"); self.next_text = label("", "muted", wrap=True)
        col.addWidget(self.next_title); col.addWidget(self.next_text)
        row.addLayout(col, 1)
        self.b_demo = QPushButton("Load sample documents"); self.b_demo.setProperty("variant", "link")
        self.b_import = QPushButton("Import documents…"); self.b_import.setToolTip("Import documents (Ctrl+O)")
        self.b_review = QPushButton("Start reviewing"); self.b_review.setProperty("variant", "primary")
        for b in (self.b_demo, self.b_import, self.b_review):
            row.addWidget(b)
        self.next_card.lay.addLayout(row)
        lay.addWidget(self.next_card)

        self.metrics = MetricStrip([("review", "Pending review"), ("approved", "Approved"), ("failed", "Extraction failures"),
                                    ("disc", "With validation discrepancies"), ("processed", "Processed in period")])
        lay.addWidget(self.metrics)

        mid = QHBoxLayout(); mid.setSpacing(14)
        c1 = Card(title="Documents imported per day"); self.bar = BarChart(); c1.lay.addWidget(self.bar)
        c2 = Card(title="Status of documents"); self.stack = StackedBar(); c2.lay.addWidget(self.stack)
        self.legend = QGridLayout(); self.legend.setHorizontalSpacing(12); self.legend.setVerticalSpacing(4); c2.lay.addLayout(self.legend); c2.lay.addStretch()
        mid.addWidget(c1, 3); mid.addWidget(c2, 2)
        lay.addLayout(mid)

        bot = QHBoxLayout(); bot.setSpacing(14)
        c3 = Card(title="Needs attention")
        self.attn = QListWidget(); self.attn.setMinimumHeight(150); self.attn.setAccessibleName("Documents needing attention")
        self.attn.itemActivated.connect(lambda it: self.review_requested.emit(it.data(Qt.UserRole)))
        self.attn.itemClicked.connect(lambda it: self.review_requested.emit(it.data(Qt.UserRole)))
        self.empty = EmptyState("Nothing here yet", "Import invoices or purchase orders and they will appear here once they are ready to check. "
                                "Everything is processed on this computer.", "Import documents…")
        self.empty.button.clicked.connect(self.import_requested.emit)
        c3.lay.addWidget(self.attn); c3.lay.addWidget(self.empty)
        c4 = Card(title="This computer")
        self.ready_ocr = label("", "muted", wrap=True); self.ready_ai = label("", "muted", wrap=True); self.ready_hint = label("", "small", wrap=True)
        for w in (self.ready_ocr, self.ready_ai, self.ready_hint):
            c4.lay.addWidget(w)
        c4.lay.addStretch()
        bot.addWidget(c3, 3); bot.addWidget(c4, 2)
        lay.addLayout(bot)
        lay.addStretch()
        self.b_import.clicked.connect(self.import_requested.emit)
        self.b_demo.clicked.connect(self.demo_requested.emit)
        self.b_review.clicked.connect(lambda: self.review_requested.emit(None))

    def on_theme(self, t: dict[str, str]) -> None:
        self.tokens = t
        self.bar.set_colors(t["primary"], t["muted"], t["border"])
        self.refresh()

    def refresh(self) -> None:
        days = self.period.currentData() or 0
        since = (datetime.now(timezone.utc) - timedelta(days=days)).strftime("%Y-%m-%dT%H:%M:%S") if days else None
        s = self.ctx.repo.stats(since)
        every = self.ctx.repo.stats(None)
        pend = every["needs_review"]
        if every["total"] == 0:
            self.next_title.setText("Start by importing a few documents")
            self.next_text.setText("Drop PDF or image files on the Import page. You can also load fictional sample invoices to try the workflow.")
        elif pend:
            self.next_title.setText(f"{pend} document{'s' if pend != 1 else ''} to review")
            self.next_text.setText("Check each value against the page, fix anything wrong, then approve.")
        elif every["failed"]:
            self.next_title.setText(f"{every['failed']} document{'s' if every['failed'] != 1 else ''} could not be read")
            self.next_text.setText("Open the Import page to see why and retry, or review them manually.")
        else:
            self.next_title.setText("All caught up")
            self.next_text.setText("Nothing is waiting for review. Export approved documents from the Export page.")
        self.b_review.setVisible(pend > 0)
        self.b_demo.setVisible(every["total"] == 0)
        self.metrics.set_value("review", s["needs_review"]); self.metrics.set_value("approved", s["approved"])
        self.metrics.set_value("failed", s["failed"]); self.metrics.set_value("disc", s["with_discrepancies"]); self.metrics.set_value("processed", s["processed"])
        self.bar.set_data(self.ctx.repo.per_day(30 if days in (0, 90) else days))
        t = self.tokens or {"success": "#1B7536", "warning": "#85550A", "danger": "#AE2A22", "info": "#1F5FBF", "muted": "#59616E", "surface2": "#F0F2F5"}
        counts = {"approved": s["approved"], "needs_review": s["needs_review"], "failed": s["failed"], "queued": s["queued"] + s["processing"], "rejected": s["rejected"]}
        self.stack.set_parts([(counts[k], t[c]) for k, _n, c in STATUS_ORDER], t["surface2"])
        while self.legend.count():
            w = self.legend.takeAt(0).widget()
            if w:
                w.setParent(None)
        for r, (k, name, c) in enumerate(STATUS_ORDER):
            sw = QLabel(); sw.setFixedSize(10, 10); sw.setStyleSheet(f"background:{t[c]}; border-radius:2px;")
            nm = label(name); nm.setAccessibleName(f"{name}: {counts[k]}")
            self.legend.addWidget(sw, r, 0); self.legend.addWidget(nm, r, 1); self.legend.addWidget(label(str(counts[k])), r, 2, Qt.AlignRight)
        self.legend.setColumnStretch(1, 1)
        self.attn.clear()
        for st, tag in (("failed", "Failed"), ("needs_review", "Review")):
            for r in self.ctx.repo.search(SearchFilter(status=st, limit=6)):
                extra = f"  ·  {r.errors} error(s)" if r.errors else ""
                it = QListWidgetItem(f"{r.supplier or r.file_name}   {r.doc_number or ''}{extra}" + ("   (failed)" if st == "failed" else ""))
                it.setData(Qt.UserRole, r.id)
                if st == "failed":
                    it.setForeground(QColor(t["danger"]))
                self.attn.addItem(it)
        has = self.attn.count() > 0
        self.attn.setVisible(has); self.empty.setVisible(not has)

    def set_readiness(self, ocr_ok: bool, ocr_text: str, ai_kind: str, ai_text: str) -> None:
        self.ready_ocr.setText(f"Text recognition (OCR): {ocr_text}")
        self.ready_ai.setText(f"AI assistant: {ai_text}")
        hints = []
        if not ocr_ok:
            hints.append("Scanned PDFs and photos need Tesseract OCR (Help › Install OCR). Digital PDFs work without it.")
        if ai_kind != "success":
            hints.append("Rules-only mode is on. Fields are found by labels and layout.")
        self.ready_hint.setText(" ".join(hints) or "Everything is available. No data leaves this computer.")
