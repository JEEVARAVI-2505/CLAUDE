"""Versioned backups and safe restore (validate -> safety copy -> stage -> swap -> rollback on failure)."""

from __future__ import annotations

import hashlib
import json
import logging
import os
import shutil
import sqlite3
import tempfile
import zipfile
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath

from pydantic import BaseModel

from .. import __version__
from ..config import AppConfig, Paths
from ..domain.exceptions import BackupError, DiskFullError, RestoreValidationError
from ..infrastructure.database import Database

log = logging.getLogger("documind.backup")
FORMAT_VERSION = 1
MAX_ENTRIES = 500_000
MAX_UNCOMPRESSED = 200 * 1024**3
BACKUP_PREFIX = "documind-backup-"


class ManifestFile(BaseModel):
    path: str
    sha256: str
    size: int


class Manifest(BaseModel):
    format_version: int
    app_version: str
    schema_version: int
    created_at: str
    include_files: bool
    document_count: int
    files: list[ManifestFile]


@dataclass
class RestoreReport:
    backup: Path
    documents: int
    safety_backup: Path | None
    kept_previous: list[Path] = field(default_factory=list)
    migrated: list[int] = field(default_factory=list)


def _sha(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        while b := fh.read(1 << 20):
            h.update(b)
    return h.hexdigest()


def _safe_member(name: str) -> bool:
    p = PurePosixPath(name)
    return not (name.startswith(("/", "\\")) or ".." in p.parts or "\\" in name or (len(name) > 1 and name[1] == ":"))


class BackupService:
    def __init__(self, cfg: AppConfig, paths: Paths, db: Database) -> None:
        self.cfg, self.paths, self.db = cfg, paths, db

    def folder(self) -> Path:
        f = Path(self.cfg.backup.folder) if self.cfg.backup.folder else self.paths.backups
        f.mkdir(parents=True, exist_ok=True)
        return f

    # ---- create ------------------------------------------------------------------------
    def create(self, dest_dir: Path | None = None, include_files: bool | None = None, tag: str = "") -> Path:
        include = self.cfg.backup.include_source_files if include_files is None else include_files
        dest_dir = Path(dest_dir) if dest_dir else self.folder()
        dest_dir.mkdir(parents=True, exist_ok=True)
        stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
        final = dest_dir / f"{BACKUP_PREFIX}{tag}{stamp}.zip"
        part = final.with_suffix(".zip.part")
        snap = Path(tempfile.mkdtemp(dir=self.paths.temp)) / "documind.db"
        try:
            self.db.snapshot(snap)
            entries: list[tuple[str, Path]] = [("documind.db", snap)]
            if include and self.paths.files.exists():
                entries += [(f"files/{f.relative_to(self.paths.files).as_posix()}", f) for f in sorted(self.paths.files.rglob("*")) if f.is_file()]
            n_docs = int(self.db.connect().execute("SELECT COUNT(*) FROM documents").fetchone()[0])
            manifest = Manifest(format_version=FORMAT_VERSION, app_version=__version__, schema_version=self.db.schema_version(),
                                created_at=datetime.now(timezone.utc).isoformat(), include_files=include, document_count=n_docs,
                                files=[ManifestFile(path=n, sha256=_sha(p), size=p.stat().st_size) for n, p in entries])
            with zipfile.ZipFile(part, "w", zipfile.ZIP_DEFLATED) as z:
                z.writestr("manifest.json", manifest.model_dump_json(indent=2))
                for name, p in entries:
                    z.write(p, name)
            self.verify(part)
            os.replace(part, final)
        except OSError as e:
            part.unlink(missing_ok=True)
            if e.errno == 28:
                raise DiskFullError("The disk is full, so the backup could not be completed.", detail=str(e)) from e
            raise BackupError(f"The backup could not be written: {e.strerror or e}", detail=str(e)) from e
        except BaseException:
            part.unlink(missing_ok=True)
            raise
        finally:
            shutil.rmtree(snap.parent, ignore_errors=True)
        self._rotate(dest_dir)
        log.info("backup created (%d documents)", n_docs)
        return final

    def _rotate(self, folder: Path) -> None:
        mine = sorted(folder.glob(f"{BACKUP_PREFIX}[0-9]*.zip"))
        for old in mine[: max(0, len(mine) - self.cfg.backup.keep_last)]:
            old.unlink(missing_ok=True)

    # ---- validate ------------------------------------------------------------------------
    def verify(self, zpath: Path) -> Manifest:
        """Full validation without touching live data. Raises RestoreValidationError."""
        try:
            z = zipfile.ZipFile(zpath)
        except (zipfile.BadZipFile, OSError) as e:
            raise RestoreValidationError("This file is not a valid DocuMind backup (not a readable ZIP archive).", detail=str(e)) from e
        with z:
            infos = z.infolist()
            if len(infos) > MAX_ENTRIES or sum(i.file_size for i in infos) > MAX_UNCOMPRESSED:
                raise RestoreValidationError("The backup is unreasonably large and was refused.")
            for i in infos:
                if not _safe_member(i.filename):
                    raise RestoreValidationError("The backup contains an unsafe file path and was refused.", detail=i.filename[:80])
            try:
                man = Manifest.model_validate(json.loads(z.read("manifest.json")))
            except (KeyError, ValueError) as e:
                raise RestoreValidationError("The backup has no valid manifest, so it cannot be trusted.", detail=str(e)[:200]) from e
            if man.format_version > FORMAT_VERSION:
                raise RestoreValidationError("This backup was made by a newer DocuMind version. Update the application first.")
            if man.schema_version > self.db.latest_version():
                raise RestoreValidationError("This backup's database is newer than this application supports. Update DocuMind first.")
            names = {i.filename: i for i in infos}
            for mf in man.files:
                zi = names.get(mf.path)
                if zi is None or zi.file_size != mf.size:
                    raise RestoreValidationError(f"The backup is incomplete or damaged ('{mf.path[:60]}').")
                h = hashlib.sha256()
                with z.open(zi) as fh:
                    while b := fh.read(1 << 20):
                        h.update(b)
                if h.hexdigest() != mf.sha256:
                    raise RestoreValidationError(f"The backup is corrupted (checksum mismatch in '{mf.path[:60]}').")
            if "documind.db" not in names:
                raise RestoreValidationError("The backup does not contain a database.")
            tmp = Path(tempfile.mkdtemp(dir=self.paths.temp))
            try:
                z.extract("documind.db", tmp)
                con = sqlite3.connect(tmp / "documind.db")
                try:
                    res = [r[0] for r in con.execute("PRAGMA integrity_check")]
                finally:
                    con.close()
                if res != ["ok"]:
                    raise RestoreValidationError("The database inside the backup is damaged.", detail="; ".join(res[:3]))
            finally:
                shutil.rmtree(tmp, ignore_errors=True)
        return man

    # ---- restore ------------------------------------------------------------------------------
    def restore(self, zpath: Path, restore_settings: bool = False) -> RestoreReport:
        zpath = Path(zpath)
        man = self.verify(zpath)
        need = sum(f.size for f in man.files)
        free = shutil.disk_usage(self.paths.root).free
        if free < need * 1.3:
            raise DiskFullError(f"Not enough free disk space to restore (need about {need * 1.3 / 1e6:.0f} MB).")
        safety: Path | None = None
        if self.paths.db.exists():
            try:
                safety = self.create(self.paths.backups, include_files=False, tag="pre-restore-")
            except BackupError as e:
                raise BackupError("A safety copy of your current data could not be made, so the restore was not started.", detail=e.detail) from e
        stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
        stage = self.paths.root / f"restore-staging-{stamp}"
        stage.mkdir()
        moved: list[tuple[Path, Path]] = []
        try:
            with zipfile.ZipFile(zpath) as z:
                for mf in man.files:
                    z.extract(mf.path, stage)
            new_db = stage / "documind.db"
            self.db.close()
            old_db = self.paths.root / f"documind.db.pre-restore-{stamp}"
            old_files = self.paths.root / f"files.pre-restore-{stamp}"
            for suffix in ("", "-wal", "-shm"):
                src = Path(str(self.paths.db) + suffix)
                if src.exists():
                    dst = Path(str(old_db) + suffix)
                    os.replace(src, dst)
                    moved.append((dst, src))
            if self.paths.files.exists():
                os.replace(self.paths.files, old_files)
                moved.append((old_files, self.paths.files))
            os.replace(new_db, self.paths.db)
            if (stage / "files").exists():
                os.replace(stage / "files", self.paths.files)
            else:
                self.paths.files.mkdir(exist_ok=True)
            if restore_settings and (stage / "settings.json").exists():
                pass  # settings are not bundled in v1 backups (they hold machine-specific paths)
        except BaseException as e:
            log.error("restore failed, rolling back (%s)", type(e).__name__)
            for dst, src in reversed(moved):
                try:
                    if src.exists():
                        if src.is_dir():
                            shutil.rmtree(src, ignore_errors=True)
                        else:
                            src.unlink()
                    os.replace(dst, src)
                except OSError:
                    log.error("rollback step failed")
            if isinstance(e, OSError):
                raise BackupError("The restore failed and your previous data was put back unchanged.", detail=str(e)) from e
            raise
        finally:
            shutil.rmtree(stage, ignore_errors=True)
        migrated = self.db.migrate()
        self.db.require_healthy()
        n = int(self.db.connect().execute("SELECT COUNT(*) FROM documents").fetchone()[0])
        kept = [p for p in (old_db, old_files) if p.exists()]
        return RestoreReport(zpath, n, safety, kept, migrated)

    def list_backups(self) -> list[Path]:
        return sorted(self.folder().glob(f"{BACKUP_PREFIX}*.zip"), reverse=True)

    def purge_previous_copies(self) -> int:
        """Delete the *.pre-restore-* leftovers after the user confirmed the restored data is right."""
        n = 0
        for p in self.paths.root.glob("*.pre-restore-*"):
            shutil.rmtree(p, ignore_errors=True) if p.is_dir() else p.unlink(missing_ok=True)
            n += 1
        return n
