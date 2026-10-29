"""Rules-only extraction orchestrator (works with no OCR model and no AI)."""

from __future__ import annotations

from dataclasses import dataclass, field

from .dates import infer_date_order
from .models import DocKind, FieldValue, PageText
from .money import format_decimal, to_decimal
from .rules_fields import ScalarExtractor, detect_kind, fv
from .rules_lines import extract_items
from .rules_text import to_lines
from .schemas import CustomFieldDef
from .templates import RulePack, TemplateDef, pick_template


@dataclass
class RulesResult:
    kind: DocKind
    fields: dict[str, FieldValue]
    items: list[dict[str, FieldValue]]
    template: str | None
    date_order: str
    warnings: list[str] = field(default_factory=list)


def extract_with_rules(pages: list[PageText], pack: RulePack, *, default_order: str = "DMY",
                       custom: list[CustomFieldDef] | None = None) -> RulesResult:
    lines = to_lines(pages)
    full = "\n".join(ln.text for ln in lines)
    template: TemplateDef | None = pick_template(pack.templates, full)
    active = pack.with_template(template) if template else pack
    order = infer_date_order(full, template.date_order if template and template.date_order else default_order)
    kind = detect_kind(lines, active, template.document_kind if template else None)
    ex = ScalarExtractor(lines, active, order, kind, template, custom)

    items, end = extract_items(lines, active)
    ex.table_end = end if items else 0

    f: dict[str, FieldValue] = {}
    ex_list = [
        ("invoice_number", lambda: ex.text_id("invoice_number")),
        ("po_number", lambda: ex.text_id("po_number")),
        ("invoice_date", lambda: ex.date_field("invoice_date", ("due", "delivery", "ship", "expiry", "valid", "print", "payment"))),
        ("due_date", lambda: ex.date_field("due_date")),
        ("currency", ex.currency),
        ("tax_id", ex.tax_id),
        ("payment_terms", ex.payment_terms),
        ("subtotal", lambda: ex.money_field("subtotal")),
        ("discount_total", lambda: ex.money_field("discount_total", active.total_exclusions)),
        ("total", lambda: ex.money_field("total", active.total_exclusions)),
    ]
    for name, fn in ex_list:
        v = fn()
        if v:
            f[name] = v
    # an "order date" label should not turn a PO number into the invoice number
    if kind == DocKind.PURCHASE_ORDER and "invoice_number" in f and "po_number" not in f:
        f["po_number"] = f.pop("invoice_number").model_copy(update={"name": "po_number"})
    elif kind == DocKind.PURCHASE_ORDER:
        f.pop("invoice_number", None)

    supplier, customer = ex.parties()
    for v in (supplier, customer):
        if v:
            f[v.name] = v
    tax_total, tax_rate = ex.tax_block()
    for v in (tax_total, tax_rate):
        if v:
            f[v.name] = v
    if "tax_rate" not in f and items:
        rates = {r["tax_rate"].value for r in items if "tax_rate" in r and r["tax_rate"].value is not None}
        if len(rates) == 1:
            first = next(r["tax_rate"] for r in items if "tax_rate" in r)
            f["tax_rate"] = fv("tax_rate", first.value, first.raw, first.page, ["derived_from_lines"])
    # subtotal derived only when absent: never silently, always flagged
    if "subtotal" not in f and items and all(r.get("line_total") and r["line_total"].value for r in items):
        pass  # intentionally NOT derived: a missing subtotal stays missing
    f.update(ex.customs())
    # normalise currency of a bare number is left to review: no guesses added here
    for name, v in list(f.items()):
        if v.value is None:
            f.pop(name)
    return RulesResult(kind, f, items, template.name if template else None, order)


def total_of(fields: dict[str, FieldValue]) -> str | None:
    v = fields.get("total")
    return format_decimal(to_decimal(v.value)) if v else None
