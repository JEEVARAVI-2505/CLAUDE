"""Shared issue building (arithmetic validation + business-key duplicate warning)."""

from __future__ import annotations

from ..config import AppConfig
from ..domain.models import DocumentRecord, Severity, ValidationIssue
from ..domain.validation import ValidationOptions, validate
from ..infrastructure.repositories import DocumentRepository


def duplicate_issues(repo: DocumentRepository, rec: DocumentRecord) -> list[ValidationIssue]:
    num = rec.get("invoice_number") or rec.get("po_number")
    dups = repo.find_business_duplicates(rec.get("supplier_name"), num, rec.id or -1)
    if not dups:
        return []
    ids = ", ".join(f"#{d.id}" for d in dups[:5])
    return [ValidationIssue(code="DUPLICATE_BUSINESS", severity=Severity.WARNING, field="invoice_number",
                            message=f"Another document from the same supplier has the same number ({ids}). It may be a duplicate.")]


def revalidate(repo: DocumentRepository, cfg: AppConfig, rec: DocumentRecord) -> None:
    opts = ValidationOptions(suppress_ambiguous_dates=cfg.suppress_ambiguous_date_warnings)
    rec.issues = validate(rec.kind, rec.fields, rec.items, opts) + duplicate_issues(repo, rec)
