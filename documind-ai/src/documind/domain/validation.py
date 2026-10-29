"""Independent arithmetic and consistency checks.

Never mutates extracted values. Where a document convention is unknown
(e.g. line totals with or without tax) every plausible convention is tried
and a discrepancy is only reported if *none* fits.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from datetime import date, timedelta
from decimal import Decimal

from .money import format_decimal, to_decimal
from .models import DocKind, FieldValue, Severity, ValidationIssue
from .schemas import SCALAR_FIELDS

ZERO = Decimal(0)
HUNDRED = Decimal(100)


@dataclass
class ValidationOptions:
    base_tolerance: Decimal = Decimal("0.02")
    per_line_tolerance: Decimal = Decimal("0.01")
    round_off_allowance: Decimal = Decimal("1.00")  # whole-unit round-off lines
    today: date | None = None
    required: dict[str, tuple[str, ...]] = field(default_factory=dict)  # kind -> fields
    suppress_ambiguous_dates: bool = False  # admin has confirmed the regional date order


def _d(fv: FieldValue | None) -> Decimal | None:
    return to_decimal(fv.value) if fv else None


def _fmt(x: Decimal | None) -> str | None:
    return format_decimal(x.quantize(Decimal("0.01")) if x is not None else None)


def _issue(code: str, sev: Severity, msg: str, **kw: object) -> ValidationIssue:
    return ValidationIssue(code=code, severity=sev, message=msg, **kw)  # type: ignore[arg-type]


def gstin_checksum_ok(gstin: str) -> bool:
    """Validate an Indian GSTIN's structure and mod-36 check character."""
    g = gstin.strip().upper()
    if not re.fullmatch(r"\d{2}[A-Z]{5}\d{4}[A-Z][1-9A-Z]Z[0-9A-Z]", g):
        return False
    alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    total = 0
    for i, ch in enumerate(g[:14]):
        v = alphabet.index(ch) * (1 if i % 2 == 0 else 2)
        total += v // 36 + v % 36
    return alphabet[(36 - total % 36) % 36] == g[14]


def line_base(item: dict[str, FieldValue]) -> Decimal | None:
    """qty x price less line discount, or None if not computable."""
    qty, price = _d(item.get("quantity")), _d(item.get("unit_price"))
    if qty is None or price is None:
        return None
    base = qty * price
    pct, amt = _d(item.get("discount_pct")), _d(item.get("discount_amount"))
    if pct is not None:
        base = base * (1 - pct / HUNDRED)
    if amt is not None:
        base -= amt
    return base


def validate(
    kind: DocKind,
    fields: dict[str, FieldValue],
    items: list[dict[str, FieldValue]],
    opts: ValidationOptions | None = None,
) -> list[ValidationIssue]:
    opts = opts or ValidationOptions()
    out: list[ValidationIssue] = []
    tol = opts.base_tolerance + opts.per_line_tolerance * len(items)

    # ---- completeness & flags ------------------------------------------------
    for spec in SCALAR_FIELDS:
        fv = fields.get(spec.name)
        required = opts.required.get(kind.value) if opts.required else None
        is_req = (spec.name in required) if required is not None else (kind.value in spec.required_for)
        if is_req and (fv is None or fv.value is None):
            sev = Severity.ERROR if spec.name == "total" else Severity.WARNING
            out.append(_issue("MISSING_REQUIRED", sev, f"{spec.label} was not found.", field=spec.name))
        if fv:
            if "ambiguous_date" in fv.flags and not opts.suppress_ambiguous_dates:
                out.append(_issue("AMBIGUOUS_DATE", Severity.WARNING,
                                  f"{spec.label} '{fv.raw}' could be day-first or month-first. Please confirm.",
                                  field=spec.name, actual=fv.value))
            if "multiple_candidates" in fv.flags:
                out.append(_issue("MULTIPLE_CANDIDATES", Severity.WARNING,
                                  f"{spec.label}: several different values were found in the document ({fv.alt or '?'}).",
                                  field=spec.name, actual=fv.value))
            if "ambiguous_symbol" in fv.flags:
                out.append(_issue("AMBIGUOUS_CURRENCY", Severity.WARNING,
                                  "The currency symbol is shared by several currencies (e.g. $). Please confirm.",
                                  field=spec.name, actual=fv.value))
            if "conflicting_currency" in fv.flags:
                out.append(_issue("CONFLICTING_CURRENCY", Severity.WARNING,
                                  f"More than one currency appears in the document ({fv.alt}).", field=spec.name))
            if "ambiguous_number" in fv.flags:
                out.append(_issue("AMBIGUOUS_NUMBER", Severity.WARNING,
                                  f"{spec.label} '{fv.raw}' uses separators that can be read two ways.", field=spec.name,
                                  actual=fv.value))
            if "conflict_ai" in fv.flags:
                out.append(_issue("AI_CONFLICT", Severity.WARNING,
                                  f"{spec.label}: rules found '{fv.value}' but the AI model read '{fv.alt}'.",
                                  field=spec.name, expected=fv.alt, actual=fv.value))
            if "ungrounded_ai" in fv.flags:
                out.append(_issue("AI_UNGROUNDED", Severity.INFO,
                                  f"{spec.label}: an AI-suggested value was discarded because it is not in the document.",
                                  field=spec.name))
            if "ocr_repair" in fv.flags:
                out.append(_issue("OCR_REPAIR", Severity.WARNING,
                                  f"{spec.label}: an OCR letter/digit confusion was corrected (the check digit validates). Please confirm.",
                                  field=spec.name, actual=fv.value))
            if "heuristic" in fv.flags:
                out.append(_issue("HEURISTIC_VALUE", Severity.INFO,
                                  f"{spec.label} was guessed from the page layout. Please check it.", field=spec.name))

    row_flags = {"ocr_repair": ("OCR_REPAIR", "a character that looked like noise was read as 0"),
                 "alignment_uncertain": ("COLUMN_ALIGNMENT", "some columns were empty so the numbers may be in the wrong columns"),
                 "partial_row": ("PARTIAL_ROW", "only some columns could be read"),
                 "ambiguous_number": ("AMBIGUOUS_NUMBER", "a number uses separators that can be read two ways")}
    for idx, it in enumerate(items):
        seen: set[str] = set()
        for cell in it.values():
            for fl in cell.flags:
                if fl in row_flags and fl not in seen:
                    seen.add(fl)
                    code, why = row_flags[fl]
                    out.append(_issue(code, Severity.WARNING, f"Line {idx + 1}: {why}. Please check this row.", row=idx))

    # ---- dates ---------------------------------------------------------------
    inv = _date(fields.get("invoice_date"))
    due = _date(fields.get("due_date"))
    if inv and due and due < inv:
        out.append(_issue("DUE_BEFORE_INVOICE", Severity.ERROR, "The due date is earlier than the document date.",
                          field="due_date", expected=f">= {inv}", actual=str(due)))
    today = opts.today or date.today()
    if inv and inv > today + timedelta(days=1):
        out.append(_issue("FUTURE_DATE", Severity.WARNING, "The document date is in the future.",
                          field="invoice_date", actual=str(inv)))

    # ---- tax id --------------------------------------------------------------
    tid = fields.get("tax_id")
    if tid and tid.value and re.fullmatch(r"[0-9A-Za-z]{15}", tid.value.strip()) and re.match(r"\d{2}[A-Za-z]{5}", tid.value):
        if not gstin_checksum_ok(tid.value):
            out.append(_issue("TAX_ID_CHECKSUM", Severity.WARNING,
                              "This looks like an Indian GSTIN but its format/check character is invalid.",
                              field="tax_id", actual=tid.value))

    subtotal, discount = _d(fields.get("subtotal")), _d(fields.get("discount_total"))
    tax_total, total = _d(fields.get("tax_total")), _d(fields.get("total"))
    tax_rate = _d(fields.get("tax_rate"))

    # ---- sign sanity -----------------------------------------------------------
    if total is not None and total < 0 and kind != DocKind.CREDIT_NOTE:
        out.append(_issue("NEGATIVE_TOTAL", Severity.WARNING,
                          "The total is negative but the document is not a credit note.", field="total",
                          actual=_fmt(total)))

    # ---- line items ------------------------------------------------------------
    nets: list[Decimal] = []
    expected_tax = ZERO
    have_line_tax = False
    for idx, it in enumerate(items):
        base = line_base(it)
        lt = _d(it.get("line_total"))
        rate, tax_amt = _d(it.get("tax_rate")), _d(it.get("tax_amount"))
        if base is None or lt is None:
            if lt is not None:
                nets.append(lt)
            out.append(_issue("LINE_INCOMPLETE", Severity.INFO,
                              f"Line {idx + 1}: quantity/price/total incomplete, arithmetic not checked.", row=idx))
            continue
        cands = [base]
        if rate is not None:
            cands.append(base * (1 + rate / HUNDRED))
        if tax_amt is not None:
            cands.append(base + tax_amt)
        match = next((c for c in cands if abs(c - lt) <= opts.per_line_tolerance + opts.base_tolerance / 2), None)
        if match is None:
            out.append(_issue("LINE_MISMATCH", Severity.ERROR,
                              f"Line {idx + 1}: qty x price"
                              + (" less discount" if _d(it.get("discount_pct")) or _d(it.get("discount_amount")) else "")
                              + f" = {_fmt(base)} but the line total is {_fmt(lt)}.",
                              row=idx, field="line_total", expected=_fmt(base), actual=_fmt(lt)))
            nets.append(lt)
        else:
            inclusive = rate is not None and match != base and tax_amt is None and abs(match - base) > Decimal("0.005")
            nets.append(lt / (1 + rate / HUNDRED) if inclusive and rate is not None else lt)
        if tax_amt is not None:
            expected_tax += tax_amt
            have_line_tax = True
        elif rate is not None:
            expected_tax += nets[-1] * rate / HUNDRED
            have_line_tax = True

    # ---- subtotal --------------------------------------------------------------
    line_sum = sum((_d(it.get("line_total")) or ZERO for it in items), ZERO) if items else None
    if line_sum is not None and subtotal is not None and items:
        fits = abs(line_sum - subtotal) <= tol
        fits_total = total is not None and abs(line_sum - total) <= tol  # lines already include tax
        if not (fits or fits_total):
            out.append(_issue("SUBTOTAL_MISMATCH", Severity.ERROR,
                              f"Line totals add up to {_fmt(line_sum)} but the subtotal is {_fmt(subtotal)}.",
                              field="subtotal", expected=_fmt(line_sum), actual=_fmt(subtotal)))
    elif line_sum is not None and subtotal is None and total is None and items:
        out.append(_issue("NO_TOTALS", Severity.WARNING, "No subtotal or total was found to check the lines against."))

    # ---- tax ---------------------------------------------------------------------
    if tax_total is not None:
        ok = None
        exp = None
        if have_line_tax:
            exp = expected_tax
            ok = abs(tax_total - exp) <= tol
        elif tax_rate is not None and subtotal is not None:
            bases = [subtotal, subtotal - (discount or ZERO)]
            cands = [b * tax_rate / HUNDRED for b in bases]
            exp = cands[-1]
            ok = any(abs(tax_total - c) <= tol for c in cands)
        if ok is False and exp is not None:
            out.append(_issue("TAX_MISMATCH", Severity.ERROR,
                              f"Tax recomputed as {_fmt(exp)} but the document shows {_fmt(tax_total)}.",
                              field="tax_total", expected=_fmt(exp), actual=_fmt(tax_total)))

    # ---- total ---------------------------------------------------------------------
    if total is not None and subtotal is not None:
        base = subtotal - (discount or ZERO)
        exp_excl = base + (tax_total or ZERO)
        diff_excl = abs(total - exp_excl)
        if diff_excl <= tol:
            pass
        elif abs(total - base) <= tol and tax_total is not None:
            out.append(_issue("TAX_INCLUSIVE", Severity.INFO,
                              "Total equals subtotal: prices appear to include tax already.", field="total"))
        elif diff_excl < opts.round_off_allowance:
            out.append(_issue("ROUND_OFF", Severity.INFO,
                              f"Total differs from subtotal - discount + tax by {_fmt(diff_excl)} (likely round-off).",
                              field="total", expected=_fmt(exp_excl), actual=_fmt(total)))
        else:
            out.append(_issue("TOTAL_MISMATCH", Severity.ERROR,
                              f"Subtotal - discount + tax = {_fmt(exp_excl)} but the total is {_fmt(total)}.",
                              field="total", expected=_fmt(exp_excl), actual=_fmt(total)))
    elif total is not None and not items and subtotal is None and tax_total is None:
        out.append(_issue("TOTAL_UNVERIFIED", Severity.INFO,
                          "The total could not be cross-checked (no subtotal or lines).", field="total"))
    return out


def _date(fv: FieldValue | None) -> date | None:
    if not fv or not fv.value:
        return None
    try:
        return date.fromisoformat(fv.value)
    except ValueError:
        return None
