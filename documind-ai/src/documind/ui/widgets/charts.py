"""Two small flat charts drawn with QPainter: daily bars and a stacked status bar."""

from __future__ import annotations

from PySide6.QtCore import QPointF, QRectF, Qt
from PySide6.QtGui import QColor, QFont, QPainter, QPen
from PySide6.QtWidgets import QSizePolicy, QWidget


class BarChart(QWidget):
    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.data: list[tuple[str, int]] = []
        self.color, self.text, self.grid = "#1F5FBF", "#59616E", "#DDE1E7"
        self.setMinimumHeight(170)
        self.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.setAccessibleName("Documents imported per day, bar chart")

    def set_colors(self, color: str, text: str, grid: str) -> None:
        self.color, self.text, self.grid = color, text, grid
        self.update()

    def set_data(self, data: list[tuple[str, int]]) -> None:
        self.data = data
        self.setAccessibleDescription(f"{sum(v for _, v in data)} documents in the last {len(data)} days")
        self.update()

    def paintEvent(self, _e) -> None:
        p = QPainter(self)
        p.setRenderHint(QPainter.Antialiasing)
        w, h = self.width(), self.height()
        left, bottom, top = 30, 20, 6
        mx = max([v for _, v in self.data] + [1])
        mx = max(mx, 4)
        f = QFont(self.font()); f.setPointSizeF(8); p.setFont(f)
        for i in range(3):
            y = top + (h - top - bottom) * i / 2
            p.setPen(QPen(QColor(self.grid), 1)); p.drawLine(QPointF(left, y), QPointF(w - 2, y))
            p.setPen(QColor(self.text)); p.drawText(QRectF(0, y - 8, left - 6, 16), Qt.AlignRight | Qt.AlignVCenter, str(round(mx * (1 - i / 2))))
        n = max(1, len(self.data))
        slot = (w - left - 4) / n
        bw = max(3.0, min(22.0, slot * 0.6))
        step = max(1, n // 6)
        p.setPen(Qt.NoPen)
        for i, (lab, v) in enumerate(self.data):
            x = left + slot * i + (slot - bw) / 2
            bh = (h - top - bottom) * v / mx
            if v:
                p.setBrush(QColor(self.color)); p.drawRect(QRectF(x, h - bottom - bh, bw, max(bh, 1.5)))
            if i % step == 0 or i == n - 1:
                p.setPen(QColor(self.text)); p.drawText(QRectF(x - 16, h - bottom + 3, bw + 32, 14), Qt.AlignCenter, lab[5:] if len(lab) == 10 else lab); p.setPen(Qt.NoPen)


class StackedBar(QWidget):
    """Single horizontal bar split by status; the legend next to it carries the numbers."""

    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.parts: list[tuple[int, str]] = []
        self.track = "#F0F2F5"
        self.setFixedHeight(10)

    def set_parts(self, parts: list[tuple[int, str]], track: str) -> None:
        self.parts, self.track = parts, track
        self.update()

    def paintEvent(self, _e) -> None:
        p = QPainter(self)
        p.setRenderHint(QPainter.Antialiasing)
        p.setPen(Qt.NoPen)
        p.setBrush(QColor(self.track)); p.drawRoundedRect(QRectF(self.rect()), 3, 3)
        total = sum(v for v, _ in self.parts)
        if not total:
            return
        x = 0.0
        for v, color in self.parts:
            if v:
                w = self.width() * v / total
                p.setBrush(QColor(color)); p.drawRect(QRectF(x, 0, max(w - 1, 1), self.height()))
                x += w
