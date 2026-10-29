from __future__ import annotations

import os
import sys
from pathlib import Path

os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

import pytest  # noqa: E402


@pytest.fixture()
def tmp_home(tmp_path, monkeypatch):
    """Isolated config + data directories (never touches the real profile)."""
    cfg, data = tmp_path / "cfg", tmp_path / "data"
    monkeypatch.setenv("DOCUMIND_CONFIG_DIR", str(cfg))
    monkeypatch.setenv("DOCUMIND_DATA_DIR", str(data))
    return tmp_path
