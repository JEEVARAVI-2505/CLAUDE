"""Business rules for Indian GST purchase bills (deterministic, no AI, no network).

Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com
"""
from __future__ import annotations
from dataclasses import dataclass
from datetime import date
from decimal import Decimal

_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"


def gstin_check_char(first14: str) -> str:
    """Return the 15th (check) character of a GSTIN given its first 14 characters.

    Algorithm (GSTN checksum): for each character take its index in _CHARS,
    multiply by 1 (even position, 0-based) or 2 (odd position), split the product p into
    p // 36 and p % 36, add both to a running sum; check = (36 - sum % 36) % 36.
    """
    total = 0
    for i, ch in enumerate(first14):
        product = _CHARS.index(ch) * (1 if i % 2 == 0 else 2)
        total += product // 36 + product % 36
    return _CHARS[(36 - total % 36) % 36]


def is_valid_gstin(gstin: str) -> bool:
    """True when `gstin` is 15 chars, uppercase alphanumeric, state code 01-38, and check char matches."""
    g = (gstin or "").strip().upper()
    if len(g) != 15 or any(c not in _CHARS for c in g):
        return False
    if not g[:2].isdigit() or not (1 <= int(g[:2]) <= 38):
        return False
    return g[14] == gstin_check_char(g[:14])


@dataclass(frozen=True)
class Bill:
    vendor_gstin: str
    invoice_no: str
    invoice_date: date
    taxable_value: Decimal
    cgst: Decimal
    sgst: Decimal
    igst: Decimal
    total: Decimal


def validate_bill(bill: Bill, today: date | None = None, tolerance: Decimal = Decimal("1.00")) -> list[str]:
    """Return a list of human-readable rule failures (empty list = bill passes all rules)."""
    errors: list[str] = []
    today = today or date.today()
    if not is_valid_gstin(bill.vendor_gstin):
        errors.append("GSTIN_INVALID")
    if not bill.invoice_no or len(bill.invoice_no) > 16:
        errors.append("INVOICE_NO_INVALID")  # GST rules cap the invoice number at 16 characters
    if bill.invoice_date > today:
        errors.append("DATE_IN_FUTURE")
    intra = bill.cgst != 0 or bill.sgst != 0
    if intra and bill.igst != 0:
        errors.append("TAX_TYPE_CONFLICT")  # a bill is either CGST+SGST or IGST, never both
    if bill.cgst != bill.sgst:
        errors.append("CGST_NE_SGST")
    computed = bill.taxable_value + bill.cgst + bill.sgst + bill.igst
    if abs(computed - bill.total) > tolerance:
        errors.append("TOTAL_MISMATCH")
    return errors
