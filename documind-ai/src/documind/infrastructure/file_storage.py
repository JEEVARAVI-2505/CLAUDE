"""Import validation (never trusts extensions), content-addressed storage, safe deletion."""

from __future__ import annotations

import errno
import hashlib
import os
import re
import shutil
import tempfile
import time
import uuid
from dataclasses import dataclass
from pathlib import Path

from ..config import LimitSettings, Paths
from ..domain.exceptions import (DiskFullError, EmptyFileError, FileAccessError, FileTooLargeError, PermissionDeniedError,
                                 StorageError, UnsupportedFileTypeError)

EXT_MIME = {".pdf": "application/pdf", ".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg",
            ".tif": "image/tiff", ".tiff": "image/tiff"}
RESERVED = {"con", "prn", "aux", "nul", *(f"com{i}" for i in range(1, 10)), *(f"lpt{i}" for i in range(1, 10))}


def sniff_mime(head: bytes) -> str | None:
    if b"%PDF-" in head[:1024]:
        return "application/pdf"
    if head.startswith(b"\x89PNG\r\n\x1a\n"):
        return "image/png"
    if head.startswith(b"\xff\xd8\xff"):
        return "image/jpeg"
    if head[:4] in (b"II*\x00", b"MM\x00*"):
        return "image/tiff"
    return None


def sha256_file(path: Path, chunk: int = 1 << 20) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        while block := fh.read(chunk):
            h.update(block)
    return h.hexdigest()


def safe_filename(name: str, default: str = "file", max_len: int = 100) -> str:
    """Make a name safe for any Windows/Linux filesystem (no separators, controls or reserved names)."""
    base = re.split(r"[\\/]", name)[-1]
    base = re.sub(r"[\x00-\x1f<>:\"|?*]", "_", base).strip(" .")
    stem, dot, ext = base.rpartition(".")
    if not dot:
        stem, ext = base, ""
    if stem.lower() in RESERVED:
        stem = "_" + stem
    out = (stem[:max_len] or default) + (("." + ext[:10]) if ext else "")
    return out


@dataclass
class FileInfo:
    path: Path
    name: str
    mime: str
    ext: str
    size: int
    sha256: str
    pages: int


def preflight(path: Path, limits: LimitSettings) -> tuple[str, str, int]:
    """Cheap checks: existence, size, signature vs extension. Returns (ext, mime, size)."""
    p = Path(path)
    try:
        if not p.exists():
            raise FileAccessError("The file could not be found. It may have been moved or deleted.")
        if not p.is_file():
            raise UnsupportedFileTypeError("Only files can be imported, not folders or devices.")
        size = p.stat().st_size
        with open(p, "rb") as fh:
            head = fh.read(4096)
    except PermissionError as e:
        raise FileAccessError("DocuMind is not allowed to read this file (check Windows permissions or whether another program has locked it).", detail=str(e)) from e
    except OSError as e:
        raise FileAccessError("The file could not be read.", detail=str(e)) from e
    if size == 0:
        raise EmptyFileError("The file is empty (0 bytes).")
    if size > limits.max_file_mb * 1024 * 1024:
        raise FileTooLargeError(f"The file is {size / 1048576:.1f} MB; the limit is {limits.max_file_mb} MB (Settings > Limits).")
    ext = p.suffix.lower()
    if ext not in EXT_MIME:
        raise UnsupportedFileTypeError(f"'{ext or 'no extension'}' files are not supported. Use PDF, PNG, JPG or TIFF.")
    mime = sniff_mime(head)
    if mime is None:
        raise UnsupportedFileTypeError("The file content is not a PDF or a supported image (the extension does not match its contents).")
    if mime != EXT_MIME[ext]:
        raise UnsupportedFileTypeError(f"The file is named {ext} but its contents are {mime.split('/')[-1].upper()}. Rename it correctly and retry.")
    return ext, mime, size


class FileStore:
    """Content-addressed copy of imported originals under <data>/files."""

    def __init__(self, paths: Paths) -> None:
        self.paths = paths

    def _inside(self, p: Path) -> bool:
        try:
            return Path(os.path.realpath(p)).is_relative_to(Path(os.path.realpath(self.paths.files)))
        except (OSError, ValueError):
            return False

    def store(self, src: Path, sha256: str, ext: str) -> Path:
        dest = self.paths.files / sha256[:2] / f"{sha256}{ext}"
        if not self._inside(dest):
            raise StorageError("Refusing to write outside the data folder.")
        if dest.exists():
            return dest
        dest.parent.mkdir(parents=True, exist_ok=True)
        fd, tmp = tempfile.mkstemp(dir=dest.parent, prefix=".incoming-", suffix=".part")
        os.close(fd)
        try:
            shutil.copyfile(src, tmp)
            if sha256_file(Path(tmp)) != sha256:
                raise StorageError("The file changed while it was being imported. Please try again.")
            os.replace(tmp, dest)
        except OSError as e:
            Path(tmp).unlink(missing_ok=True)
            if e.errno == errno.ENOSPC:
                raise DiskFullError("The disk is full. Free some space and try again.", detail=str(e)) from e
            if isinstance(e, PermissionError):
                raise PermissionDeniedError("No permission to write to the DocuMind data folder.", detail=str(e)) from e
            raise StorageError("The file could not be copied into DocuMind's storage.", detail=str(e)) from e
        except BaseException:
            Path(tmp).unlink(missing_ok=True)
            raise
        return dest

    def resolve(self, stored_path: str) -> Path:
        p = Path(stored_path)
        if not self._inside(p):
            raise StorageError("The stored file path is outside the data folder and was refused.")
        return p

    def delete(self, stored_path: str, wipe: bool = True) -> bool:
        """Best-effort overwrite-then-delete. Returns True if a file was removed."""
        p = self.resolve(stored_path)
        if not p.exists():
            return False
        try:
            if wipe:
                size = p.stat().st_size
                with open(p, "r+b") as fh:
                    left = size
                    while left > 0:
                        n = min(left, 1 << 20)
                        fh.write(b"\x00" * n)
                        left -= n
                    fh.flush()
                    os.fsync(fh.fileno())
            p.unlink()
            try:
                p.parent.rmdir()
            except OSError:
                pass
            return True
        except PermissionError as e:
            raise PermissionDeniedError("The stored file could not be deleted (it may be open in another program).", detail=str(e)) from e

    def new_temp(self, suffix: str = ".tmp") -> Path:
        self.paths.temp.mkdir(parents=True, exist_ok=True)
        return self.paths.temp / f"{uuid.uuid4().hex}{suffix}"

    def cleanup_temp(self, max_age_s: float = 3600) -> int:
        n, now = 0, time.time()
        if not self.paths.temp.exists():
            return 0
        for f in self.paths.temp.iterdir():
            try:
                if f.is_file() and now - f.stat().st_mtime > max_age_s:
                    f.unlink()
                    n += 1
            except OSError:
                pass
        return n
