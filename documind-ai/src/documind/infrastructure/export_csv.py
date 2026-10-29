"""CSV writer (UTF-8 with BOM for Excel, formula-injection neutralised)."""

from __future__ import annotations

import csv
import errno
import os
import tempfile
from pathlib import Path
from typing import Any, Iterable

from ..domain.exceptions import DiskFullError, ExportError, PermissionDeniedError
from .export_common import fmt_csv


def write_csv(path: Path, headers: list[str], keys: list[str], rows: Iterable[dict[str, Any]], delimiter: str = ",") -> None:
    path = Path(path)
    fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=path.name + ".", suffix=".part")
    try:
        with os.fdopen(fd, "w", encoding="utf-8-sig", newline="") as fh:
            w = csv.writer(fh, delimiter=delimiter, quoting=csv.QUOTE_MINIMAL, lineterminator="\r\n")
            w.writerow([fmt_csv(h) for h in headers])
            for r in rows:
                w.writerow([fmt_csv(r.get(k)) for k in keys])
            fh.flush()
            os.fsync(fh.fileno())
        os.replace(tmp, path)
    except OSError as e:
        Path(tmp).unlink(missing_ok=True)
        raise translate_os_error(e, path) from e


def translate_os_error(e: OSError, path: Path) -> Exception:
    if e.errno == errno.ENOSPC:
        return DiskFullError("The disk is full, so the export could not be saved.", detail=str(e))
    if isinstance(e, PermissionError):
        return PermissionDeniedError(f"'{path.name}' cannot be written. Close it if it is open in Excel, or pick another folder.", detail=str(e))
    return ExportError(f"The export could not be saved: {e.strerror or e}", detail=str(e))
