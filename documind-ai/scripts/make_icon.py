"""Render the application icon (installer/documind.ico + png) from the vector logo. Needs PySide6; runs offscreen."""
import os
import sys
from pathlib import Path

os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))
from PySide6.QtGui import QGuiApplication  # noqa: E402

from documind.ui.icons import logo_pixmap  # noqa: E402

app = QGuiApplication([])
out = Path(__file__).resolve().parents[1] / "installer"; out.mkdir(exist_ok=True)
logo_pixmap(256, 1.0).save(str(out / "documind.png"))
from PIL import Image  # noqa: E402

Image.open(out / "documind.png").save(out / "documind.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("wrote", out / "documind.ico")
