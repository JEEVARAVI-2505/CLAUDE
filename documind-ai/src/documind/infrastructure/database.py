"""SQLite access: per-thread connections, transactions, migrations, integrity."""

from __future__ import annotations

import logging
import sqlite3
import threading
from contextlib import contextmanager
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterator

from ..domain.exceptions import DatabaseError, DatabaseLockedError, DiskFullError, IntegrityCheckError, MigrationError

log = logging.getLogger("documind.db")
MIGRATIONS_DIR = Path(__file__).parent / "migrations"


def utcnow() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%fZ")


def _translate(e: sqlite3.Error) -> DatabaseError:
    msg = str(e).lower()
    if "locked" in msg or "busy" in msg:
        return DatabaseLockedError("The database is busy (another DocuMind window or backup may be using it). Try again in a moment.", detail=str(e))
    if "full" in msg or "disk" in msg:
        return DiskFullError("The disk is full. Free some space and try again.", detail=str(e))
    return DatabaseError("A database error occurred.", detail=str(e))


class Database:
    def __init__(self, path: Path, busy_timeout_ms: int = 5000) -> None:
        self.path = Path(path)
        self.busy_timeout_ms = busy_timeout_ms
        self._local = threading.local()
        self._all: list[sqlite3.Connection] = []
        self._lock = threading.Lock()
        self._gen = 0

    # -- connections --------------------------------------------------------
    def connect(self) -> sqlite3.Connection:
        conn = getattr(self._local, "conn", None)
        if conn is not None and getattr(self._local, "gen", -1) == self._gen:
            return conn
        try:
            conn = sqlite3.connect(self.path, timeout=self.busy_timeout_ms / 1000, isolation_level=None, check_same_thread=False)
            conn.row_factory = sqlite3.Row
            conn.execute(f"PRAGMA busy_timeout={self.busy_timeout_ms}")
            conn.execute("PRAGMA foreign_keys=ON")
            conn.execute("PRAGMA secure_delete=ON")
            conn.execute("PRAGMA journal_mode=WAL")
            conn.execute("PRAGMA synchronous=FULL")
        except sqlite3.Error as e:
            raise _translate(e) from e
        self._local.conn, self._local.gen = conn, self._gen
        with self._lock:
            self._all.append(conn)
        return conn

    def close(self) -> None:
        with self._lock:
            for c in self._all:
                try:
                    c.close()
                except sqlite3.Error:
                    pass
            self._all.clear()
            self._gen += 1  # connections cached by other threads are now stale and will be reopened

    @contextmanager
    def transaction(self) -> Iterator[sqlite3.Connection]:
        """BEGIN IMMEDIATE ... COMMIT; rolls back on any exception."""
        conn = self.connect()
        try:
            conn.execute("BEGIN IMMEDIATE")
        except sqlite3.Error as e:
            raise _translate(e) from e
        try:
            yield conn
            conn.execute("COMMIT")
        except BaseException as e:
            try:
                conn.execute("ROLLBACK")
            except sqlite3.Error:
                pass
            if isinstance(e, sqlite3.Error):
                raise _translate(e) from e
            raise

    # -- migrations ---------------------------------------------------------
    @staticmethod
    def _migration_files() -> list[tuple[int, str, Path]]:
        out = []
        for f in sorted(MIGRATIONS_DIR.glob("[0-9][0-9][0-9][0-9]_*.sql")):
            out.append((int(f.name[:4]), f.stem, f))
        return out

    def schema_version(self) -> int:
        conn = self.connect()
        try:
            row = conn.execute("SELECT MAX(version) FROM schema_migrations").fetchone()
            return int(row[0] or 0)
        except sqlite3.OperationalError:
            return 0

    def latest_version(self) -> int:
        files = self._migration_files()
        return files[-1][0] if files else 0

    def migrate(self, backup_dir: Path | None = None) -> list[int]:
        """Apply pending migrations, each in its own transaction."""
        applied: list[int] = []
        current = self.schema_version()
        if current > self.latest_version():
            raise MigrationError("This database was created by a newer version of DocuMind. Update the application; the data was not changed.")
        pending = [m for m in self._migration_files() if m[0] > current]
        if pending and current > 0 and backup_dir is not None:
            self.snapshot(backup_dir / f"pre-migration-v{current}-{datetime.now():%Y%m%d-%H%M%S}.db")
        for version, name, path in pending:
            sql = path.read_text(encoding="utf-8")
            try:
                with self.transaction() as conn:
                    for stmt in _split_statements(sql):
                        conn.execute(stmt)
                    conn.execute("CREATE TABLE IF NOT EXISTS schema_migrations(version INTEGER PRIMARY KEY, name TEXT NOT NULL, applied_at TEXT NOT NULL)")
                    conn.execute("INSERT INTO schema_migrations(version, name, applied_at) VALUES (?,?,?)", (version, name, utcnow()))
            except DatabaseError as e:
                raise MigrationError(f"Database upgrade '{name}' failed and was rolled back. Your data is unchanged.", detail=e.detail) from e
            applied.append(version)
            log.info("applied migration %s", name)
        return applied

    # -- maintenance ----------------------------------------------------------
    def integrity_check(self) -> list[str]:
        conn = self.connect()
        try:
            rows = [r[0] for r in conn.execute("PRAGMA integrity_check").fetchall()]
            fk = conn.execute("PRAGMA foreign_key_check").fetchall()
        except sqlite3.Error as e:
            raise _translate(e) from e
        problems = [r for r in rows if r != "ok"] + [f"foreign key violation in {r[0]}" for r in fk]
        return problems

    def require_healthy(self) -> None:
        problems = self.integrity_check()
        if problems:
            raise IntegrityCheckError("The database failed its integrity check. Restore a backup (Settings > Backup).", detail="; ".join(problems[:5]))

    def snapshot(self, dest: Path) -> Path:
        """Consistent copy using SQLite's online backup API (safe while in use)."""
        dest.parent.mkdir(parents=True, exist_ok=True)
        tmp = dest.with_suffix(dest.suffix + ".part")
        tmp.unlink(missing_ok=True)
        try:
            out = sqlite3.connect(tmp)
            try:
                self.connect().backup(out)
            finally:
                out.close()
            tmp.replace(dest)
        except sqlite3.Error as e:
            tmp.unlink(missing_ok=True)
            raise _translate(e) from e
        return dest

    def checkpoint(self) -> None:
        """Flush and truncate the WAL so deleted rows do not linger in -wal."""
        try:
            self.connect().execute("PRAGMA wal_checkpoint(TRUNCATE)")
        except sqlite3.Error:
            pass

    def vacuum(self) -> None:
        try:
            self.connect().execute("VACUUM")
        except sqlite3.Error as e:
            raise _translate(e) from e


def _split_statements(sql: str) -> list[str]:
    out, buf = [], ""
    for line in sql.splitlines():
        if line.strip().startswith("--"):
            continue
        buf += line + "\n"
        if sqlite3.complete_statement(buf):
            out.append(buf.strip())
            buf = ""
    if buf.strip():
        out.append(buf.strip())
    return out
