"""Golden validation tests. Each expected value is hand-calculated in the comment."""
from datetime import date
from decimal import Decimal

from documind.domain.models import DocKind, FieldValue, Severity
from documind.domain.validation import ValidationOptions, gstin_checksum_ok, validate


def F(**kw):
    return {k: FieldValue(name=k, value=None if v is None else str(v)) for k, v in kw.items()}


def row(**kw):
    return {k: FieldValue(name=k, value=str(v)) for k, v in kw.items()}


def codes(issues, sev=None):
    return [i.code for i in issues if sev is None or i.severity == sev]


OPTS = ValidationOptions(today=date(2025, 6, 1))


def test_consistent_invoice_has_no_errors():
    # 10 x 250 less 10% = 2250.00 ; tax 18% of 2250 = 405.00 ; total = 2655.00
    items = [row(quantity=10, unit_price=250, discount_pct=10, line_total=2250)]
    f = F(subtotal=2250, tax_rate=18, tax_total=405, total=2655, invoice_date="2025-05-01",
          supplier_name="A", invoice_number="1", currency="INR")
    assert codes(validate(DocKind.INVOICE, f, items, OPTS), Severity.ERROR) == []


def test_line_mismatch_reported_with_expected_value():
    # 3 x 100 = 300 but the document says 330
    items = [row(quantity=3, unit_price=100, line_total=330)]
    issues = validate(DocKind.INVOICE, F(subtotal=330, total=330), items, OPTS)
    bad = [i for i in issues if i.code == "LINE_MISMATCH"]
    assert len(bad) == 1 and bad[0].expected == "300" and bad[0].actual == "330" and bad[0].row == 0


def test_line_total_including_tax_is_accepted():
    # 2 x 500 = 1000 ; +12% = 1120 shown as line total (tax-inclusive convention)
    items = [row(quantity=2, unit_price=500, tax_rate=12, line_total=1120)]
    issues = validate(DocKind.INVOICE, F(subtotal=1120, total=1120), items, OPTS)
    assert "LINE_MISMATCH" not in codes(issues)


def test_subtotal_mismatch():
    # lines: 100 + 200 = 300, subtotal says 350
    items = [row(quantity=1, unit_price=100, line_total=100), row(quantity=1, unit_price=200, line_total=200)]
    issues = validate(DocKind.INVOICE, F(subtotal=350, total=350), items, OPTS)
    assert "SUBTOTAL_MISMATCH" in codes(issues, Severity.ERROR)


def test_tax_mismatch_from_rate():
    # 5% of 1000 = 50, document says 60
    issues = validate(DocKind.INVOICE, F(subtotal=1000, tax_rate=5, tax_total=60, total=1060), [], OPTS)
    t = [i for i in issues if i.code == "TAX_MISMATCH"][0]
    assert t.expected == "50" and t.actual == "60"


def test_tax_on_discounted_base_accepted():
    # subtotal 1000 - discount 100 = 900 ; 18% of 900 = 162 ; total = 900 + 162 = 1062
    issues = validate(DocKind.INVOICE, F(subtotal=1000, discount_total=100, tax_rate=18, tax_total=162, total=1062), [], OPTS)
    assert codes(issues, Severity.ERROR) == []


def test_multi_rate_line_tax_sum():
    # 1000 @5% = 50 ; 2000 @18% = 360 ; total tax 410 ; total 3410
    items = [row(quantity=1, unit_price=1000, tax_rate=5, line_total=1000),
             row(quantity=1, unit_price=2000, tax_rate=18, line_total=2000)]
    ok = validate(DocKind.INVOICE, F(subtotal=3000, tax_total=410, total=3410), items, OPTS)
    assert codes(ok, Severity.ERROR) == []
    bad = validate(DocKind.INVOICE, F(subtotal=3000, tax_total=540, total=3540), items, OPTS)
    assert "TAX_MISMATCH" in codes(bad)


def test_total_mismatch_error():
    issues = validate(DocKind.INVOICE, F(subtotal=1000, tax_total=180, total=1200), [], OPTS)
    t = [i for i in issues if i.code == "TOTAL_MISMATCH"][0]
    assert t.expected == "1180"


def test_round_off_below_one_unit_is_info_not_error():
    # CGST Act 2017 s.170 permits rounding to nearest rupee: 1180.40 shown as 1180.00
    issues = validate(DocKind.INVOICE, F(subtotal=1000, tax_total=180.40, total=1180), [], OPTS)
    assert "ROUND_OFF" in codes(issues, Severity.INFO) and "TOTAL_MISMATCH" not in codes(issues)


def test_tax_inclusive_total_equals_subtotal():
    issues = validate(DocKind.INVOICE, F(subtotal=1180, tax_total=180, total=1180), [], OPTS)
    assert "TAX_INCLUSIVE" in codes(issues) and "TOTAL_MISMATCH" not in codes(issues)


def test_decimal_precision_no_float_error():
    # 3 x 0.10 = 0.30 exactly in Decimal (0.1*3 != 0.3 in binary floating point)
    items = [row(quantity=3, unit_price="0.10", line_total="0.30")]
    assert "LINE_MISMATCH" not in codes(validate(DocKind.INVOICE, F(subtotal="0.30", total="0.30"), items, OPTS))


def test_rounding_tolerance_scales_with_line_count():
    # 3 lines each off by 0.01 from rounding is tolerated
    items = [row(quantity=1, unit_price="33.333", line_total="33.34") for _ in range(3)]
    issues = validate(DocKind.INVOICE, F(subtotal="100.02", total="100.02"), items, OPTS)
    assert codes(issues, Severity.ERROR) == []


def test_credit_note_negative_total_ok_but_invoice_flagged():
    f = F(subtotal=-100, tax_total=-18, total=-118)
    assert "NEGATIVE_TOTAL" not in codes(validate(DocKind.CREDIT_NOTE, f, [], OPTS))
    assert "NEGATIVE_TOTAL" in codes(validate(DocKind.INVOICE, f, [], OPTS))


def test_missing_required_fields_flagged_never_invented():
    issues = validate(DocKind.INVOICE, {}, [], OPTS)
    missing = {i.field for i in issues if i.code == "MISSING_REQUIRED"}
    assert {"supplier_name", "invoice_number", "invoice_date", "total", "currency"} <= missing
    assert any(i.field == "total" and i.severity == Severity.ERROR for i in issues)


def test_purchase_order_does_not_require_invoice_number():
    missing = {i.field for i in validate(DocKind.PURCHASE_ORDER, {}, [], OPTS) if i.code == "MISSING_REQUIRED"}
    assert "invoice_number" not in missing and "po_number" in missing


def test_due_before_invoice_and_future_date():
    f = F(invoice_date="2025-05-10", due_date="2025-05-01")
    assert "DUE_BEFORE_INVOICE" in codes(validate(DocKind.INVOICE, f, [], OPTS))
    assert "FUTURE_DATE" in codes(validate(DocKind.INVOICE, F(invoice_date="2026-01-01"), [], OPTS))


def test_validation_does_not_modify_inputs():
    f = F(subtotal=1000, tax_total=999, total=5)
    snapshot = {k: v.model_copy(deep=True) for k, v in f.items()}
    validate(DocKind.INVOICE, f, [], OPTS)
    assert f == snapshot


def test_gstin_checksum_known_vectors():
    # Public sample GSTIN used in GST portal documentation examples
    assert gstin_checksum_ok("27AAPFU0939F1ZV")
    assert not gstin_checksum_ok("27AAPFU0939F1ZX")  # wrong check character
    assert not gstin_checksum_ok("1234")


def test_gstin_checksum_hand_calculation():
    # Hand calc for "07AAAAA0000A1Z?" is long; instead confirm the algorithm matches the vector above
    # by recomputing independently: chars 0-13 weights alternate 1,2 ; value = q + r of (code*weight)/36
    g = "27AAPFU0939F1Z"
    alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    s = 0
    for i, ch in enumerate(g):
        p = alphabet.index(ch) * (1 if i % 2 == 0 else 2)
        s += p // 36 + p % 36
    assert alphabet[(36 - s % 36) % 36] == "V"
