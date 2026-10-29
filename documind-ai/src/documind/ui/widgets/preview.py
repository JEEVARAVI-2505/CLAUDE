"""Document preview: page navigation, zoom, rotation, fit-to-width."""

from __future__ import annotations

from collections import OrderedDict
from pathlib import Path

from PySide6.QtCore import Qt, Signal
from PySide6.QtGui import QImage, QPixmap, QTransform
from PySide6.QtWidgets import (QHBoxLayout, QLabel, QPushButton, QScrollArea, QSpinBox, QToolButton, QVBoxLayout, QWidget)

from ...config import LimitSettings
from ...domain.exceptions import DocuMindError
from ...infrastructure.pdf_reader import PageSource, open_source
from ..workers import run_async
from .common import label


def pil_to_pixmap(img) -> QPixmap:
    img = img.convert("RGB")
    data = img.tobytes("raw", "RGB")
    qi = QImage(data, img.width, img.height, img.width * 3, QImage.Format_RGB888).copy()
    return QPixmap.fromImage(qi)


class PageViewer(QWidget):
    page_changed = Signal(int)

    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.src: PageSource | None = None
        self.page = 0
        self.zoom = 1.0
        self.rotation = 0
        self._cache: OrderedDict[tuple[int, int], QPixmap] = OrderedDict()
        self._token = 0
        self._fit = True
        lay = QVBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        bar = QHBoxLayout()
        self.prev = self._btn("‹", "Previous page (Page Up)", lambda: self.goto(self.page - 1))
        self.spin = QSpinBox()
        self.spin.setMinimum(1)
        self.spin.setAccessibleName("Page number")
        self.spin.valueChanged.connect(lambda v: self.goto(v - 1))
        self.total = label("/ 0", "muted")
        self.next = self._btn("›", "Next page (Page Down)", lambda: self.goto(self.page + 1))
        for w in (self.prev, self.spin, self.total, self.next):
            bar.addWidget(w)
        bar.addStretch()
        self.zout = self._btn("−", "Zoom out (Ctrl+-)", lambda: self.set_zoom(self.zoom / 1.25))
        self.zlbl = label("100%", "muted")
        self.zin = self._btn("+", "Zoom in (Ctrl++)", lambda: self.set_zoom(self.zoom * 1.25))
        self.fit = self._btn("Fit", "Fit page width", self.fit_width)
        self.rl = self._btn("⟲", "Rotate left (view only)", lambda: self.rotate(-90))
        self.rr = self._btn("⟳", "Rotate right (view only)", lambda: self.rotate(90))
        for w in (self.zout, self.zlbl, self.zin, self.fit, self.rl, self.rr):
            bar.addWidget(w)
        lay.addLayout(bar)
        self.area = QScrollArea()
        self.area.setWidgetResizable(True)
        self.area.setAlignment(Qt.AlignCenter)
        self.canvas = QLabel("No document selected")
        self.canvas.setAlignment(Qt.AlignCenter)
        self.canvas.setProperty("role", "muted")
        self.area.setWidget(self.canvas)
        lay.addWidget(self.area, 1)
        self.setFocusPolicy(Qt.StrongFocus)
        self._enable(False)

    def _btn(self, text: str, tip: str, fn) -> QToolButton:
        b = QToolButton()
        b.setText(text)
        b.setToolTip(tip)
        b.setAccessibleName(tip)
        b.clicked.connect(fn)
        return b

    def _enable(self, on: bool) -> None:
        for w in (self.prev, self.next, self.spin, self.zout, self.zin, self.fit, self.rl, self.rr):
            w.setEnabled(on)

    # ---- loading --------------------------------------------------------------
    def clear(self, message: str = "No document selected") -> None:
        if self.src:
            self.src.close()
        self.src = None
        self._cache.clear()
        self._token += 1
        self.canvas.setPixmap(QPixmap())
        self.canvas.setText(message)
        self._enable(False)
        self.total.setText("/ 0")

    def load(self, path: str, mime: str, limits: LimitSettings) -> None:
        self.clear("Loading preview…")
        if not Path(path).exists():
            self.clear("The original file is no longer stored (it was removed by the retention setting or deleted).")
            return
        try:
            self.src = open_source(Path(path), mime, limits)
        except DocuMindError as e:
            self.clear(f"Preview unavailable: {e.user_message}")
            return
        self.page, self.rotation, self._fit = 0, 0, True
        self.spin.blockSignals(True)
        self.spin.setMaximum(self.src.page_count)
        self.spin.setValue(1)
        self.spin.blockSignals(False)
        self.total.setText(f"/ {self.src.page_count}")
        self._enable(True)
        self._render()

    def goto(self, index: int) -> None:
        if not self.src:
            return
        index = max(0, min(index, self.src.page_count - 1))
        if index == self.page and self.canvas.pixmap() and not self.canvas.pixmap().isNull():
            return
        self.page = index
        self.spin.blockSignals(True)
        self.spin.setValue(index + 1)
        self.spin.blockSignals(False)
        self._render()
        self.page_changed.emit(index + 1)

    def go_to_page_number(self, number: int) -> None:
        self.goto(number - 1)

    def set_zoom(self, z: float) -> None:
        self._fit = False
        self.zoom = max(0.3, min(z, 4.0))
        self.zlbl.setText(f"{int(self.zoom * 100)}%")
        self._render()

    def fit_width(self) -> None:
        self._fit = True
        self._render()

    def rotate(self, deg: int) -> None:
        self.rotation = (self.rotation + deg) % 360
        self._show(self._cache.get(self._key()))

    def _key(self) -> tuple[int, int]:
        return (self.page, int(self.zoom * 100) if not self._fit else -1)

    def _render(self) -> None:
        if not self.src:
            return
        key = self._key()
        if key in self._cache:
            self._show(self._cache[key])
            return
        self._token += 1
        tok, src, idx = self._token, self.src, self.page
        dpi = 150 if self._fit else int(110 * self.zoom)
        self.canvas.setText("Rendering page…")
        run_async(lambda: src.image(idx, dpi),
                  lambda img, t=tok, k=key: self._done(img, t, k),
                  lambda msg, t=tok: self._failed(msg, t), parent=self)

    def _done(self, img, tok: int, key) -> None:
        if tok != self._token:
            return
        pm = pil_to_pixmap(img)
        self._cache[key] = pm
        while len(self._cache) > 6:
            self._cache.popitem(last=False)
        self._show(pm)

    def _failed(self, msg: str, tok: int) -> None:
        if tok == self._token:
            self.canvas.setText(f"Could not render this page: {msg}")

    def _show(self, pm: QPixmap | None) -> None:
        if pm is None:
            return self._render()
        if self.rotation:
            pm = pm.transformed(QTransform().rotate(self.rotation), Qt.SmoothTransformation)
        if self._fit:
            target = max(200, self.area.viewport().width() - 24)
            pm = pm.scaledToWidth(target, Qt.SmoothTransformation)
            self.zoom = target / max(1, pm.width()) if False else self.zoom
        self.canvas.setText("")
        self.canvas.setPixmap(pm)
        self.canvas.setAccessibleName(f"Page {self.page + 1} preview")

    def resizeEvent(self, e) -> None:
        super().resizeEvent(e)
        if self._fit and self.src and self._key() in self._cache:
            self._show(self._cache[self._key()])

    def keyPressEvent(self, e) -> None:
        k = e.key()
        if k == Qt.Key_PageDown:
            self.goto(self.page + 1)
        elif k == Qt.Key_PageUp:
            self.goto(self.page - 1)
        elif e.modifiers() & Qt.ControlModifier and k in (Qt.Key_Plus, Qt.Key_Equal):
            self.set_zoom(self.zoom * 1.25)
        elif e.modifiers() & Qt.ControlModifier and k == Qt.Key_Minus:
            self.set_zoom(self.zoom / 1.25)
        else:
            super().keyPressEvent(e)

    def wheelEvent(self, e) -> None:
        if e.modifiers() & Qt.ControlModifier:
            self.set_zoom(self.zoom * (1.1 if e.angleDelta().y() > 0 else 1 / 1.1))
            e.accept()
        else:
            super().wheelEvent(e)

    def closeEvent(self, e) -> None:
        self.clear()
        super().closeEvent(e)
