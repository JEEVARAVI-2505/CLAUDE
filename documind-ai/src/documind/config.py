"""Typed, validated application configuration with safe atomic persistence."""

from __future__ import annotations

import json
import os
import sys
import tempfile
from datetime import datetime
from pathlib import Path
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, ValidationError

from .domain.exceptions import ConfigError, DiskFullError, PermissionDeniedError
from .domain.schemas import CustomFieldDef

DEFAULT_EXPORT_COLUMNS = [
    "source_file", "document_kind", "supplier_name", "customer_name", "invoice_number", "po_number", "invoice_date",
    "due_date", "currency", "tax_id", "subtotal", "discount_total", "tax_rate", "tax_total", "total", "payment_terms",
    "validation_status", "review_status",
]


class AiSettings(BaseModel):
    model_config = ConfigDict(extra="ignore")
    enabled: bool = False  # off until the customer has Ollama + a model installed
    host: str = "http://127.0.0.1:11434"
    model: str = "qwen2.5:7b-instruct"
    timeout_s: int = Field(120, ge=5, le=1800)
    allow_remote_host: bool = False  # must be explicitly enabled to leave localhost
    max_chars: int = Field(24000, ge=2000, le=200000)
    retries: int = Field(1, ge=0, le=3)


class OcrSettings(BaseModel):
    model_config = ConfigDict(extra="ignore")
    mode: Literal["auto", "always", "off"] = "auto"
    tesseract_path: str | None = None
    languages: str = Field("eng", pattern=r"^[A-Za-z_]{2,12}(\+[A-Za-z_]{2,12}){0,5}$")
    dpi: int = Field(200, ge=100, le=400)
    min_embedded_chars: int = Field(40, ge=0, le=2000)
    timeout_s: int = Field(120, ge=10, le=900)


class LimitSettings(BaseModel):
    model_config = ConfigDict(extra="ignore")
    max_file_mb: int = Field(50, ge=1, le=500)
    max_pages: int = Field(100, ge=1, le=1000)
    max_batch_files: int = Field(500, ge=1, le=5000)
    max_image_megapixels: int = Field(50, ge=5, le=500)


class ExportSettings(BaseModel):
    model_config = ConfigDict(extra="ignore")
    columns: list[str] = Field(default_factory=lambda: list(DEFAULT_EXPORT_COLUMNS))
    format: Literal["xlsx", "csv"] = "xlsx"
    approved_only: bool = True
    include_line_items: bool = True
    csv_delimiter: Literal[",", ";", "\t"] = ","
    folder: str | None = None


class RetentionSettings(BaseModel):
    model_config = ConfigDict(extra="ignore")
    days: int = Field(0, ge=0, le=36500)  # 0 = keep until manually deleted
    keep_source_files: bool = True


class BackupSettings(BaseModel):
    model_config = ConfigDict(extra="ignore")
    folder: str | None = None
    include_source_files: bool = True
    keep_last: int = Field(10, ge=1, le=100)


class AppConfig(BaseModel):
    model_config = ConfigDict(extra="ignore")
    schema_version: int = 1
    data_dir: str | None = None
    theme: Literal["light", "dark"] = "light"
    date_order: Literal["DMY", "MDY"] = "DMY"
    suppress_ambiguous_date_warnings: bool = False
    default_currency: str | None = Field(None, pattern=r"^[A-Z]{3}$")
    first_run_done: bool = False
    show_tips: bool = True  # short how-to banner at the top of each workflow screen
    ai: AiSettings = Field(default_factory=AiSettings)
    ocr: OcrSettings = Field(default_factory=OcrSettings)
    limits: LimitSettings = Field(default_factory=LimitSettings)
    export: ExportSettings = Field(default_factory=ExportSettings)
    retention: RetentionSettings = Field(default_factory=RetentionSettings)
    backup: BackupSettings = Field(default_factory=BackupSettings)
    custom_fields: list[CustomFieldDef] = Field(default_factory=list)


def config_dir() -> Path:
    if v := os.environ.get("DOCUMIND_CONFIG_DIR"):
        return Path(v)
    if sys.platform == "win32":
        return Path(os.environ.get("APPDATA", Path.home() / "AppData" / "Roaming")) / "DocuMindAI"
    return Path(os.environ.get("XDG_CONFIG_HOME", Path.home() / ".config")) / "DocuMindAI"


def default_data_dir() -> Path:
    if v := os.environ.get("DOCUMIND_DATA_DIR"):
        return Path(v)
    if sys.platform == "win32":
        return Path(os.environ.get("LOCALAPPDATA", Path.home() / "AppData" / "Local")) / "DocuMindAI"
    return Path(os.environ.get("XDG_DATA_HOME", Path.home() / ".local" / "share")) / "DocuMindAI"


class Paths:
    """All writable locations, derived from one data directory."""

    def __init__(self, data_dir: Path) -> None:
        self.root = Path(data_dir)
        self.db = self.root / "documind.db"
        self.files = self.root / "files"
        self.backups = self.root / "backups"
        self.logs = self.root / "logs"
        self.temp = self.root / "temp"
        self.templates = self.root / "templates"
        self.exports = self.root / "exports"

    def ensure(self) -> None:
        for d in (self.root, self.files, self.backups, self.logs, self.temp, self.templates, self.exports):
            try:
                d.mkdir(parents=True, exist_ok=True)
            except PermissionError as e:
                raise PermissionDeniedError(f"DocuMind cannot write to '{self.root}'. Choose a folder you have access to.",
                                            detail=str(e)) from e
            except OSError as e:
                raise ConfigError(f"The data folder '{self.root}' cannot be used: {e.strerror or e}", detail=str(e)) from e


def atomic_write_text(path: Path, text: str) -> None:
    """Write via temp file + replace so a crash never leaves a half-written file."""
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=path.name + ".", suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as fh:
            fh.write(text)
            fh.flush()
            os.fsync(fh.fileno())
        os.replace(tmp, path)
    except OSError as e:
        Path(tmp).unlink(missing_ok=True)
        if e.errno == 28:
            raise DiskFullError("The disk is full. Free some space and try again.", detail=str(e)) from e
        if isinstance(e, PermissionError):
            raise PermissionDeniedError(f"No permission to write '{path.name}'.", detail=str(e)) from e
        raise


class ConfigManager:
    def __init__(self, directory: Path | None = None) -> None:
        self.dir = Path(directory) if directory else config_dir()
        self.file = self.dir / "config.json"
        self.notices: list[str] = []

    def load(self) -> AppConfig:
        if not self.file.exists():
            return AppConfig()
        try:
            return AppConfig.model_validate(json.loads(self.file.read_text(encoding="utf-8")))
        except (ValueError, ValidationError, OSError) as e:
            stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
            bad = self.file.with_name(f"config.json.corrupt-{stamp}")
            try:
                os.replace(self.file, bad)
            except OSError:
                pass
            self.notices.append(f"Settings file was damaged and has been reset to defaults (kept as {bad.name}).")
            return AppConfig()

    def save(self, cfg: AppConfig) -> None:
        atomic_write_text(self.file, cfg.model_dump_json(indent=2))

    def resolve_paths(self, cfg: AppConfig) -> Paths:
        env = os.environ.get("DOCUMIND_DATA_DIR")
        raw = env or cfg.data_dir
        root = Path(raw) if raw else default_data_dir()
        if not root.is_absolute():
            raise ConfigError("The data folder must be an absolute path.")
        return Paths(root)
