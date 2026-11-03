"""Golden tests. Every expected value is worked out by hand in the comment above it."""
from datetime import date
from decimal import Decimal as D

from app.rules import Bill, gstin_check_char, is_valid_gstin, validate_bill

GOOD = "27AAPFU0939F1ZV"  # widely cited sample GSTIN (Maharashtra, state code 27)
TODAY = date(2026, 11, 3)


def bill(**kw):
    base = dict(vendor_gstin=GOOD, invoice_no="INV-001", invoice_date=date(2026, 10, 1),
                taxable_value=D("10000"), cgst=D("900"), sgst=D("900"), igst=D("0"), total=D("11800"))
    base.update(kw)
    return Bill(**base)


def test_check_char_of_known_gstin():
    assert gstin_check_char("27AAPFU0939F1Z") == "V"


def test_valid_gstin_accepts_known_good():
    assert is_valid_gstin(GOOD)
    assert is_valid_gstin(GOOD.lower())  # case-insensitive input is normalised


def test_gstin_rejects_wrong_check_char_length_state_and_symbols():
    assert not is_valid_gstin("27AAPFU0939F1ZW")   # wrong check character
    assert not is_valid_gstin("27AAPFU0939F1Z")    # 14 chars
    assert not is_valid_gstin("99AAPFU0939F1ZV")   # state code 99 does not exist
    assert not is_valid_gstin("27AAPFU0939F1Z-")   # illegal character
    assert not is_valid_gstin("")


def test_intra_state_bill_passes():
    # 10,000 + 9% CGST 900 + 9% SGST 900 = 11,800
    assert validate_bill(bill(), TODAY) == []


def test_inter_state_bill_passes():
    # 10,000 + 18% IGST 1,800 = 11,800
    assert validate_bill(bill(cgst=D("0"), sgst=D("0"), igst=D("1800")), TODAY) == []


def test_total_mismatch_detected_beyond_rounding():
    # computed 11,800 vs stated 11,810 -> difference 10 > 1.00 tolerance
    assert "TOTAL_MISMATCH" in validate_bill(bill(total=D("11810")), TODAY)


def test_rounding_within_one_rupee_is_allowed():
    assert validate_bill(bill(total=D("11800.50")), TODAY) == []


def test_cgst_and_igst_together_is_a_conflict():
    errs = validate_bill(bill(igst=D("1800"), total=D("13600")), TODAY)
    assert "TAX_TYPE_CONFLICT" in errs


def test_cgst_must_equal_sgst():
    assert "CGST_NE_SGST" in validate_bill(bill(sgst=D("800"), total=D("11700")), TODAY)


def test_future_date_and_long_invoice_number():
    errs = validate_bill(bill(invoice_date=date(2027, 1, 1), invoice_no="X" * 17), TODAY)
    assert "DATE_IN_FUTURE" in errs and "INVOICE_NO_INVALID" in errs


def test_bad_gstin_reported():
    assert "GSTIN_INVALID" in validate_bill(bill(vendor_gstin="27AAPFU0939F1ZW"), TODAY)
