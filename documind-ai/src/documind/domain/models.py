"""Typed domain models shared by every layer (no GUI / DB / AI imports)."""

from __future__ import annotations

from datetime import datetime
from enum import Enum
from typing import Literal

from pydantic import BaseModel, Field


class DocStatus(str, Enum):
    QUEUED = "queued"
    PROCESSING = "processing"
    NEEDS_REVIEW = "needs_review"
    APPROVED = "approved"
    REJECTED = "rejected"
    FAILED = "failed"


class DocKind(str, Enum):
    INVOICE = "invoice"
    PURCHASE_ORDER = "purchase_order"
    CREDIT_NOTE = "credit_note"
    UNKNOWN = "unknown"


class Severity(str, Enum):
    INFO = "info"
    WARNING = "warning"
    ERROR = "error"


Source = Literal["rules", "llm", "rules+llm", "user"]


class FieldValue(BaseModel):
    """One extracted cell with provenance.

    ``value`` is the current (possibly corrected) normalised value,
    ``extracted_value`` the untouched machine value, ``raw`` the text exactly
    as it appeared in the document. Missing data is ``None``; never invented.
    """

    name: str
    value: str | None = None
    extracted_value: str | None = None
    raw: str | None = None
    page: int | None = None  # 1-based page where the raw text was found
    source: Source = "rules"
    origin: Literal["extracted", "corrected"] = "extracted"
    confirmed: bool = False
    flags: list[str] = Field(default_factory=list)
    alt: str | None = None  # conflicting alternative value, if any

    @property
    def state(self) -> str:
        if self.confirmed:
            return "validated"
        if self.origin == "corrected":
            return "corrected"
        return "extracted" if self.value is not None else "missing"


class ValidationIssue(BaseModel):
    code: str
    severity: Severity
    message: str
    field: str | None = None
    row: int | None = None  # 0-based line-item index
    expected: str | None = None
    actual: str | None = None


class ExtractionInfo(BaseModel):
    """How a record was produced (no document text is stored here)."""

    text_method: str = "none"  # embedded | ocr | mixed | none
    pages: int = 0
    ocr_pages: list[int] = Field(default_factory=list)
    ocr_score: float | None = None  # mean OCR word score, uncalibrated
    mode: str = "rules"  # rules | rules+ai
    model: str | None = None
    template: str | None = None
    date_order: str | None = None
    warnings: list[str] = Field(default_factory=list)


class DocumentRecord(BaseModel):
    id: int | None = None
    file_name: str
    sha256: str
    stored_path: str
    mime: str
    size_bytes: int
    pages: int = 0
    status: DocStatus = DocStatus.QUEUED
    kind: DocKind = DocKind.UNKNOWN
    imported_at: datetime | None = None
    processed_at: datetime | None = None
    error: str | None = None
    fields: dict[str, FieldValue] = Field(default_factory=dict)
    items: list[dict[str, FieldValue]] = Field(default_factory=list)
    issues: list[ValidationIssue] = Field(default_factory=list)
    info: ExtractionInfo = Field(default_factory=ExtractionInfo)

    def get(self, name: str) -> str | None:
        fv = self.fields.get(name)
        return fv.value if fv else None

    @property
    def error_count(self) -> int:
        return sum(1 for i in self.issues if i.severity == Severity.ERROR)

    @property
    def warning_count(self) -> int:
        return sum(1 for i in self.issues if i.severity == Severity.WARNING)


class PageText(BaseModel):
    page: int  # 1-based
    text: str
    source: Literal["embedded", "ocr"] = "embedded"
    ocr_score: float | None = None
