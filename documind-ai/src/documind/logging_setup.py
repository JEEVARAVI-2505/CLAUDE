"""Rotating file log that redacts personal data and never records document text."""

from __future__ import annotations

import logging
import re
from logging.handlers import RotatingFileHandler
from pathlib import Path

_PATTERNS = [
    (re.compile(r"[\w.+-]+@[\w-]+\.[\w.-]+"), "<email>"),
    (re.compile(r"(?i)(api[_-]?key|token|password|secret)\s*[=:]\s*\S+"), r"\1=<redacted>"),
    (re.compile(r"\b\d{9,}\b"), "<number>"),
    (re.compile(r"(?i)([A-Z]:\\Users\\)[^\\\s]+"), r"\1<user>"),
    (re.compile(r"(/home/|/Users/)[^/\s]+"), r"\1<user>"),
]


def redact(text: str) -> str:
    for rx, rep in _PATTERNS:
        text = rx.sub(rep, text)
    return text


class RedactingFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        return redact(super().format(record))


def setup_logging(log_dir: Path, level: int = logging.INFO) -> logging.Logger:
    log_dir.mkdir(parents=True, exist_ok=True)
    logger = logging.getLogger("documind")
    logger.setLevel(level)
    logger.propagate = False
    for h in list(logger.handlers):
        logger.removeHandler(h)
        h.close()
    handler = RotatingFileHandler(log_dir / "documind.log", maxBytes=1_000_000, backupCount=5, encoding="utf-8")
    handler.setFormatter(RedactingFormatter("%(asctime)s %(levelname)s %(name)s: %(message)s"))
    logger.addHandler(handler)
    return logger
