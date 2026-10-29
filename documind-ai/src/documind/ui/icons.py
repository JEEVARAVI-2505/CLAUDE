"""Vector icons drawn with QPainter (crisp at any DPI, recolourable, no image files)."""

from __future__ import annotations

import math

from PySide6.QtCore import QPointF, QRectF, Qt
from PySide6.QtGui import QBrush, QColor, QIcon, QLinearGradient, QPainter, QPainterPath, QPen, QPixmap


def _pen(color: str, w: float) -> QPen:
    p = QPen(QColor(color), w)
    p.setCapStyle(Qt.RoundCap)
    p.setJoinStyle(Qt.RoundJoin)
    return p


def _draw(name: str, p: QPainter, c: str) -> None:
    """Icons are designed on a 24x24 grid."""
    p.setPen(_pen(c, 1.9))
    p.setBrush(Qt.NoBrush)
    if name == "dashboard":
        for x, y, w, h in ((3.5, 3.5, 7, 9), (13.5, 3.5, 7, 5), (3.5, 15.5, 7, 5), (13.5, 11.5, 7, 9)):
            p.drawRoundedRect(QRectF(x, y, w, h), 2, 2)
    elif name == "import":
        p.drawLine(QPointF(12, 3.5), QPointF(12, 14.5))
        p.drawPolyline([QPointF(7.5, 10.5), QPointF(12, 15), QPointF(16.5, 10.5)])
        path = QPainterPath(); path.moveTo(4, 14.5); path.lineTo(4, 19); path.quadTo(4, 20.5, 5.5, 20.5)
        path.lineTo(18.5, 20.5); path.quadTo(20, 20.5, 20, 19); path.lineTo(20, 14.5); p.drawPath(path)
    elif name == "queue":
        for i, y in enumerate((6, 12, 18)):
            p.drawLine(QPointF(9.5, y), QPointF(20.5, y))
            p.drawEllipse(QPointF(4.8, y), 1.4, 1.4)
    elif name == "review":
        path = QPainterPath(); path.moveTo(6, 3.5); path.lineTo(14, 3.5); path.lineTo(19, 8.5); path.lineTo(19, 20.5); path.lineTo(6, 20.5); path.closeSubpath()
        p.drawPath(path)
        p.drawPolyline([QPointF(13.5, 3.8), QPointF(13.5, 9), QPointF(18.7, 9)])
        p.drawPolyline([QPointF(9, 15), QPointF(11.2, 17.2), QPointF(15.5, 12.6)])
    elif name == "history":
        p.drawEllipse(QPointF(12, 12), 8.5, 8.5)
        p.drawPolyline([QPointF(12, 7), QPointF(12, 12), QPointF(15.5, 14)])
    elif name == "export":
        p.drawLine(QPointF(12, 15), QPointF(12, 4))
        p.drawPolyline([QPointF(7.8, 8.2), QPointF(12, 4), QPointF(16.2, 8.2)])
        path = QPainterPath(); path.moveTo(4, 14); path.lineTo(4, 19); path.quadTo(4, 20.5, 5.5, 20.5)
        path.lineTo(18.5, 20.5); path.quadTo(20, 20.5, 20, 19); path.lineTo(20, 14); p.drawPath(path)
    elif name == "settings":
        p.drawEllipse(QPointF(12, 12), 3.2, 3.2)
        for k in range(8):
            a = k * math.pi / 4
            p.drawLine(QPointF(12 + 6.2 * math.cos(a), 12 + 6.2 * math.sin(a)), QPointF(12 + 9 * math.cos(a), 12 + 9 * math.sin(a)))
        p.drawEllipse(QPointF(12, 12), 6.2, 6.2)
    elif name == "help":
        p.drawEllipse(QPointF(12, 12), 8.5, 8.5)
        path = QPainterPath(); path.moveTo(9.3, 9.6); path.quadTo(9.5, 7, 12, 7); path.quadTo(14.7, 7, 14.7, 9.4)
        path.quadTo(14.7, 11, 12.8, 12); path.quadTo(12, 12.5, 12, 14); p.drawPath(path)
        p.drawPoint(QPointF(12, 16.8))
    elif name == "sun":
        p.drawEllipse(QPointF(12, 12), 4, 4)
        for k in range(8):
            a = k * math.pi / 4
            p.drawLine(QPointF(12 + 6.8 * math.cos(a), 12 + 6.8 * math.sin(a)), QPointF(12 + 9 * math.cos(a), 12 + 9 * math.sin(a)))
    elif name == "moon":
        path = QPainterPath(); path.moveTo(19, 14.5); path.quadTo(12, 18.5, 8.5, 11.5); path.quadTo(7, 8, 9.5, 4.5)
        path.quadTo(3, 6, 3.5, 13); path.quadTo(5, 20.5, 12, 20.5); path.quadTo(17.5, 20.5, 19, 14.5); p.drawPath(path)
    elif name == "check":
        p.drawPolyline([QPointF(5, 12.5), QPointF(10, 17.5), QPointF(19, 7)])
    elif name == "warn":
        path = QPainterPath(); path.moveTo(12, 4); path.lineTo(21, 19.5); path.lineTo(3, 19.5); path.closeSubpath(); p.drawPath(path)
        p.drawLine(QPointF(12, 10), QPointF(12, 14.5)); p.drawPoint(QPointF(12, 17))
    elif name == "error":
        p.drawEllipse(QPointF(12, 12), 8.5, 8.5)
        p.drawLine(QPointF(8.5, 8.5), QPointF(15.5, 15.5)); p.drawLine(QPointF(15.5, 8.5), QPointF(8.5, 15.5))
    elif name == "doc":
        path = QPainterPath(); path.moveTo(6, 3.5); path.lineTo(14, 3.5); path.lineTo(19, 8.5); path.lineTo(19, 20.5); path.lineTo(6, 20.5); path.closeSubpath()
        p.drawPath(path); p.drawLine(QPointF(9, 13), QPointF(16, 13)); p.drawLine(QPointF(9, 16.5), QPointF(14, 16.5))
    elif name == "clock":
        _draw("history", p, c)
    elif name == "shield":
        path = QPainterPath(); path.moveTo(12, 3.5); path.lineTo(19.5, 6.5); path.lineTo(19.5, 12); path.quadTo(19.5, 18, 12, 21); path.quadTo(4.5, 18, 4.5, 12)
        path.lineTo(4.5, 6.5); path.closeSubpath(); p.drawPath(path)
        p.drawPolyline([QPointF(8.8, 12), QPointF(11.2, 14.4), QPointF(15.4, 9.8)])
    elif name == "plus":
        p.drawLine(QPointF(12, 5), QPointF(12, 19)); p.drawLine(QPointF(5, 12), QPointF(19, 12))


def icon_pixmap(name: str, color: str = "#FFFFFF", size: int = 24, dpr: float = 2.0) -> QPixmap:
    px = QPixmap(int(size * dpr), int(size * dpr))
    px.setDevicePixelRatio(dpr)
    px.fill(Qt.transparent)
    p = QPainter(px)
    p.setRenderHint(QPainter.Antialiasing)
    p.scale(size / 24, size / 24)
    _draw(name, p, color)
    p.end()
    return px


def make_icon(name: str, color: str = "#FFFFFF", size: int = 24) -> QIcon:
    return QIcon(icon_pixmap(name, color, size))


def logo_pixmap(size: int = 64, dpr: float = 2.0) -> QPixmap:
    """Plain monogram: blue rounded square with a white page and three text lines."""
    px = QPixmap(int(size * dpr), int(size * dpr))
    px.setDevicePixelRatio(dpr)
    px.fill(Qt.transparent)
    p = QPainter(px)
    p.setRenderHint(QPainter.Antialiasing)
    p.scale(size / 64, size / 64)
    p.setBrush(QBrush(QColor("#1F5FBF"))); p.setPen(Qt.NoPen)
    p.drawRoundedRect(QRectF(2, 2, 60, 60), 12, 12)
    doc = QPainterPath(); doc.moveTo(19, 14); doc.lineTo(37, 14); doc.lineTo(46, 23); doc.lineTo(46, 50); doc.lineTo(19, 50); doc.closeSubpath()
    p.setBrush(QBrush(QColor("#FFFFFF"))); p.drawPath(doc)
    p.setPen(_pen("#1F5FBF", 2.8))
    for y, x2 in ((29, 40), (36, 40), (43, 33)):
        p.drawLine(QPointF(24, y), QPointF(x2, y))
    p.end()
    return px


def app_icon() -> QIcon:
    ic = QIcon()
    for s in (16, 24, 32, 48, 64, 128, 256):
        ic.addPixmap(logo_pixmap(s, 1.0))
    return ic
