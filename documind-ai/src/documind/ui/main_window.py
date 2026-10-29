"""Application shell: sidebar navigation, lazily built pages, status bar, theme."""

from __future__ import annotations

from PySide6.QtCore import QObject, QSize, Qt, QTimer, Signal
from PySide6.QtGui import QIcon, QKeySequence, QShortcut
from PySide6.QtWidgets import (QApplication, QFrame, QHBoxLayout, QLabel, QMainWindow, QMessageBox, QPushButton, QStackedWidget,
                               QToolButton, QVBoxLayout, QWidget)

from .. import APP_NAME, __version__
from ..application.batch import BatchEvent
from ..application.context import AppContext
from ..infrastructure.ollama_client import OllamaClient
from .icons import app_icon, icon_pixmap, logo_pixmap
from .styles.theme import apply_theme
from .workers import run_async

NAV = [("dashboard", "Home", "dashboard", "Ctrl+1"), ("import", "1  Add documents", "import", "Ctrl+2"),
       ("review", "2  Check and approve", "review", "Ctrl+3"), ("export", "3  Export to Excel", "export", "Ctrl+4"),
       ("history", "All documents", "history", "Ctrl+5"), ("settings", "Settings", "settings", "Ctrl+6"), ("help", "Help", "help", "F1")]


class BatchBridge(QObject):
    """Moves BatchProcessor events (worker thread) onto the GUI thread."""
    event = Signal(object)

    def push(self, ev: BatchEvent) -> None:
        self.event.emit(ev)


class LazyPages(dict):
    """Pages are built the first time they are opened (faster start, less memory)."""

    def __init__(self, factories: dict) -> None:
        super().__init__()
        self._f = factories
        self.on_create = lambda key, page: None

    def __getitem__(self, key: str) -> QWidget:
        if not super().__contains__(key):
            page = self._f[key]()
            super().__setitem__(key, page)
            self.on_create(key, page)
        return super().__getitem__(key)


class MainWindow(QMainWindow):
    def __init__(self, ctx: AppContext, bridge: BatchBridge) -> None:
        super().__init__()
        self.ctx, self.bridge = ctx, bridge
        self.setWindowTitle(APP_NAME)
        self.setWindowIcon(app_icon())
        self.resize(1280, 800)
        self.setMinimumSize(960, 620)
        self.tokens: dict[str, str] = {}
        root = QWidget(); self.setCentralWidget(root)
        h = QHBoxLayout(root); h.setContentsMargins(0, 0, 0, 0); h.setSpacing(0)

        self.side = QFrame(); self.side.setObjectName("Sidebar"); self.side.setFixedWidth(232)
        sv = QVBoxLayout(self.side); sv.setContentsMargins(0, 14, 0, 12); sv.setSpacing(0)
        brand = QHBoxLayout(); brand.setContentsMargins(16, 0, 12, 0); brand.setSpacing(10)
        logo = QLabel(); logo.setPixmap(logo_pixmap(28)); logo.setAccessibleName("DocuMind logo")
        name = QLabel("DocuMind"); name.setStyleSheet("font-weight:600; font-size:11.5pt;")
        brand.addWidget(logo); brand.addWidget(name); brand.addStretch()
        sv.addLayout(brand); sv.addSpacing(14)
        self.nav_buttons: dict[str, QToolButton] = {}
        for key, text, icon, sc in NAV:
            b = QToolButton(); b.setProperty("nav", "true"); b.setText("  " + text.replace("&", "&&")); b.setCheckable(True); b.setAutoExclusive(True)
            b.setIconSize(QSize(18, 18)); b.setToolButtonStyle(Qt.ToolButtonTextBesideIcon); b.setMinimumHeight(36)
            b.setSizePolicy(b.sizePolicy().Policy.Expanding, b.sizePolicy().Policy.Fixed)
            b.setCursor(Qt.PointingHandCursor); b.setToolTip(f"{text} ({sc})"); b.setAccessibleName(text)
            b.clicked.connect(lambda _c=False, k=key: self.show_page(k))
            QShortcut(QKeySequence(sc), self, activated=lambda k=key: self.show_page(k))
            self.nav_buttons[key] = b
            sv.addWidget(b)
        sv.addStretch()
        self.st_ocr = QLabel("Scans: checking…"); self.st_ai = QLabel("AI helper: checking…")
        for lb in (self.st_ocr, self.st_ai):
            lb.setProperty("role", "small"); lb.setContentsMargins(16, 0, 8, 2); lb.setWordWrap(True); sv.addWidget(lb)
        sv.addSpacing(6)
        self.theme_btn = QPushButton(); self.theme_btn.setProperty("variant", "link"); self.theme_btn.setCursor(Qt.PointingHandCursor)
        self.theme_btn.clicked.connect(self.toggle_theme); self.theme_btn.setAccessibleName("Switch between light and dark theme")
        self.theme_btn.setStyleSheet("text-align:left; padding-left:16px;"); sv.addWidget(self.theme_btn)
        ver = QLabel(f"Version {__version__}"); ver.setProperty("role", "small"); ver.setContentsMargins(16, 0, 0, 0)
        sv.addWidget(ver)
        h.addWidget(self.side)

        right = QVBoxLayout(); right.setContentsMargins(0, 0, 0, 0); right.setSpacing(0)
        self.banner = QFrame(); self.banner.setProperty("banner", "info"); self.banner.hide()
        bl = QHBoxLayout(self.banner); bl.setContentsMargins(12, 6, 8, 6)
        self.banner_text = QLabel(""); self.banner_text.setWordWrap(True)
        close = QToolButton(); close.setText("Dismiss"); close.clicked.connect(self.banner.hide)
        bl.addWidget(self.banner_text, 1); bl.addWidget(close)
        right.addWidget(self.banner)
        self.stack = QStackedWidget(); right.addWidget(self.stack, 1)
        h.addLayout(right, 1)

        def mk(mod: str, cls: str, *extra):
            def f():
                import importlib
                return getattr(importlib.import_module(f"documind.ui.pages.{mod}"), cls)(ctx, *extra)
            return f
        self.pages = LazyPages({
            "dashboard": lambda: __import__("documind.ui.pages.dashboard", fromlist=["x"]).DashboardPage(ctx),
            "import": mk("import_page", "ImportPage", self), "review": mk("review_page", "ReviewPage", self),
            "history": mk("history_page", "HistoryPage", self), "export": mk("export_page", "ExportPage", self),
            "settings": mk("settings_page", "SettingsPage", self), "help": mk("help_page", "HelpPage", self)})
        self.pages.on_create = self._page_created
        d = self.pages["dashboard"]
        d.import_requested.connect(self.go_import); d.demo_requested.connect(self.load_demo); d.go_requested.connect(self.show_page)
        d.review_requested.connect(lambda i: self.open_review(i) if i else self.show_page("review"))
        bridge.event.connect(self.on_batch_event)
        QShortcut(QKeySequence("Ctrl+O"), self, activated=self.go_import)
        QShortcut(QKeySequence("Ctrl+E"), self, activated=lambda: self.show_page("export"))
        self.apply_theme(ctx.cfg.theme)
        self.show_page("dashboard")
        for n in ctx.notices:
            self.show_banner(n, "warning")
        self.refresh_readiness()
        QTimer.singleShot(0, self.setFocus)

    def _page_created(self, key: str, page: QWidget) -> None:
        self.stack.addWidget(page)
        if self.tokens and hasattr(page, "on_theme"):
            page.on_theme(self.tokens)

    # ---- navigation -------------------------------------------------------------------
    def go_import(self) -> None:
        self.show_page("import")
        self.pages["import"].browse()

    def show_page(self, key: str, animate: bool = False) -> None:
        page = self.pages[key]
        self.nav_buttons[key].setChecked(True)
        if hasattr(page, "refresh"):
            page.refresh()
        self.stack.setCurrentWidget(page)
        self.statusBar().showMessage(dict((k, t) for k, t, _i, _s in NAV)[key], 1500)

    def open_review(self, doc_id) -> None:
        self.show_page("review")
        self.pages["review"].select_document(doc_id)

    # ---- feedback ------------------------------------------------------------------------
    def notify(self, text: str) -> None:
        self.statusBar().showMessage(text, 9000)

    def show_banner(self, text: str, kind: str = "info") -> None:
        self.banner.setProperty("banner", kind)
        self.banner.style().unpolish(self.banner); self.banner.style().polish(self.banner)
        self.banner_text.setText(text)
        self.banner.show()

    def error(self, title: str, text: str) -> None:
        QMessageBox.warning(self, title, text)

    # ---- theme ------------------------------------------------------------------------------
    def apply_theme(self, name: str) -> None:
        t = apply_theme(QApplication.instance(), name)  # type: ignore[arg-type]
        self.tokens = t
        self.ctx.cfg.theme = name  # type: ignore[assignment]
        self.theme_btn.setText("Switch to dark mode" if name == "light" else "Switch to light mode")
        for key, b in self.nav_buttons.items():
            icon = next(i for k, _t, i, _s in NAV if k == key)
            ic = QIcon()
            ic.addPixmap(icon_pixmap(icon, t["muted"], 18), QIcon.Normal, QIcon.Off)
            ic.addPixmap(icon_pixmap(icon, t["primary_text"], 18), QIcon.Normal, QIcon.On)
            b.setIcon(ic)
        for p in self.pages.values():
            if hasattr(p, "on_theme"):
                p.on_theme(t)

    def toggle_theme(self) -> None:
        self.apply_theme("dark" if self.ctx.cfg.theme == "light" else "light")
        try:
            self.ctx.cfg_mgr.save(self.ctx.cfg)
        except Exception:
            pass

    # ---- readiness -----------------------------------------------------------------------------
    def refresh_readiness(self) -> None:
        def probe():
            o = self.ctx.ocr.status(refresh=True)
            kind, text, note = "neutral", "off (rules-only)", "AI is disabled."
            if self.ctx.cfg.ai.enabled:
                try:
                    st = OllamaClient(self.ctx.cfg.ai).check()
                    kind, text, note = ("success", f"ready ({self.ctx.cfg.ai.model})", st.message) if st.ready else ("warning", "not ready", st.message)
                except Exception as e:
                    kind, text, note = "warning", "not ready", getattr(e, "user_message", str(e))
            return o, kind, text, note

        def done(res):
            o, kind, text, note = res
            self.st_ocr.setText("Scans (OCR): " + ("ready" if o.available else "not installed")); self.st_ocr.setToolTip(o.message)
            self.st_ai.setText("AI helper: " + text.replace("off (rules-only)", "off")); self.st_ai.setToolTip(note)
            self.pages["dashboard"].set_readiness(o.available, "ready" + (f" ({o.version})" if o.version else "") if o.available else "not installed", kind, text)
        run_async(probe, done, lambda m: self.st_ai.setText("AI helper: check failed"), parent=self)

    # ---- batch + demo ---------------------------------------------------------------------------
    def on_batch_event(self, ev: BatchEvent) -> None:
        if "import" in self.pages:
            self.pages["import"].on_event(ev)
        if ev.kind in ("done", "failed"):
            self.pages["dashboard"].refresh()
            if ev.kind == "done" and self.ctx.batch.wait_idle(0):
                self.notify("All documents have been read. Open step 2 to check them.")

    def load_demo(self) -> None:
        from ..samples import write_demo_set
        self.notify("Creating fictional sample documents…")

        def work():
            files = write_demo_set(self.ctx.paths.temp / "demo")
            return [r for r in self.ctx.docs.import_files(files, skip_duplicates=False) if r.doc_id]

        def done(rs):
            self.show_page("import")
            self.pages["import"].add_results(rs)
            self.ctx.batch.enqueue([r.doc_id for r in rs])
        run_async(work, done, lambda m: self.error("Sample documents", m), parent=self)

    def closeEvent(self, e) -> None:
        if not self.ctx.batch.wait_idle(0):
            ans = QMessageBox.question(self, "Still processing", "Documents are still being processed. Quit anyway? Unfinished ones are queued again next time.")
            if ans != QMessageBox.Yes:
                e.ignore()
                return
        self.ctx.cfg_mgr.save(self.ctx.cfg)
        self.ctx.shutdown()
        super().closeEvent(e)
