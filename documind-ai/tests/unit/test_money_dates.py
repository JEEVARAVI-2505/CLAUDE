from datetime import date
from decimal import Decimal

import pytest

from documind.domain.dates import find_dates, infer_date_order, parse_date
from documind.domain.money import format_decimal, parse_amount


@pytest.mark.parametrize("raw,expected", [
    ("1,234.56", "1234.56"),
    ("1.234,56", "1234.56"),          # European
    ("1,23,456.78", "123456.78"),     # Indian lakh grouping
    ("Rs. 12,500.00", "12500.00"),
    ("₹1,00,000", "100000"),
    ("(12.50)", "-12.50"),            # accounting negative
    ("12.50-", "-12.50"),             # trailing minus
    ("-7", "-7"),
    ("0.05", "0.05"),
    ("1.234.567", "1234567"),
    ("USD 99", "99"),
])
def test_parse_amount_golden(raw, expected):
    assert parse_amount(raw).value == Decimal(expected)


@pytest.mark.parametrize("raw", ["", "abc", "12a", "--5", "1,2,3,x", "١٢٣"])
def test_parse_amount_rejects_non_numbers(raw):
    assert parse_amount(raw).value is None


def test_single_comma_three_digits_is_ambiguous_thousands():
    r = parse_amount("1,234")
    assert r.value == Decimal(1234) and r.ambiguous


def test_comma_decimal_two_digits_not_ambiguous():
    r = parse_amount("12,50")
    assert r.value == Decimal("12.50") and not r.ambiguous


def test_format_decimal_canonical():
    assert format_decimal(Decimal("12.500")) == "12.5"
    assert format_decimal(Decimal("1E+3")) == "1000"
    assert format_decimal(None) is None


def test_iso_and_text_dates_unambiguous():
    assert parse_date("2025-03-04").value == date(2025, 3, 4)
    assert parse_date("4 March 2025").value == date(2025, 3, 4)
    assert parse_date("Mar 4, 2025").value == date(2025, 3, 4)
    assert parse_date("04-Mar-25").value == date(2025, 3, 4)


def test_numeric_date_ambiguity_flagged_and_uses_configured_order():
    p = parse_date("03/04/2025", "DMY")
    assert p.value == date(2025, 4, 3) and p.ambiguous
    q = parse_date("03/04/2025", "MDY")
    assert q.value == date(2025, 3, 4) and q.ambiguous


def test_numeric_date_unambiguous_when_day_over_12():
    p = parse_date("25/12/2024")
    assert p.value == date(2024, 12, 25) and not p.ambiguous
    q = parse_date("12/25/2024")
    assert q.value == date(2024, 12, 25) and not q.ambiguous


def test_same_day_and_month_not_ambiguous():
    assert not parse_date("05/05/2025").ambiguous


def test_invalid_dates_rejected():
    assert parse_date("31/02/2025").value is None
    assert parse_date("not a date").value is None


def test_infer_date_order_from_document():
    assert infer_date_order("Invoice 03/04/2025 due 25/04/2025") == "DMY"
    assert infer_date_order("Invoice 03/04/2025 due 04/25/2025", "DMY") == "MDY"
    assert infer_date_order("only 03/04/2025", "MDY") == "MDY"


def test_find_dates_orders_left_to_right():
    got = [d.value for d in find_dates("due 2025-05-01 issued 2025-04-01")]
    assert got == [date(2025, 5, 1), date(2025, 4, 1)]
