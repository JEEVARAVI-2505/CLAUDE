"""REQ-001..REQ-007: registration. Known defects are strict xfails so the suite is green yet honest."""
import pytest
from markers import known
from playwright.sync_api import expect
from test_data import VALID_PASSWORD, unique_email


def register(reg, **kw):
    data = {"email": unique_email(), **kw}
    reg.open().register(**data)
    return data


@pytest.mark.smoke
def test_valid_registration_succeeds(register_page, login_page):
    register(register_page)
    expect(login_page.status).to_contain_text("Registration successful")          # REQ-007


@pytest.mark.parametrize("age", [pytest.param(17, id="17-below-min"), pytest.param("abc", id="text"), pytest.param("", id="empty"),
                                 pytest.param("25.5", id="decimal"), pytest.param("-5", id="negative"), pytest.param(10, id="10")])
def test_invalid_age_rejected(register_page, age):                                # REQ-003 invalid partitions + lower neighbour
    register(register_page, age=age)
    expect(register_page.alerts.first).to_be_visible()


@pytest.mark.parametrize("age", [19, 35, 59, 60])
def test_valid_age_accepted(register_page, login_page, age):                      # REQ-003 valid partition + inner boundaries
    register(register_page, age=age)
    expect(login_page.status).to_contain_text("Registration successful")


@known("D1: age 18 rejected", builds=("1.0",))
def test_age_lower_boundary_18_accepted(register_page, login_page):
    register(register_page, age=18)
    expect(login_page.status).to_contain_text("Registration successful")


@known("D2: age 61 accepted", builds=("1.0", "1.1"))
def test_age_upper_boundary_plus_one_61_rejected(register_page):
    register(register_page, age=61)
    expect(register_page.alerts.first).to_be_visible()


@known("D3: 'a@b' accepted as e-mail", builds=("1.0", "1.1"))
def test_email_without_domain_suffix_rejected(register_page):                     # REQ-002
    register_page.open().register(email="a@b")
    expect(register_page.alerts.first).to_be_visible()


@known("D4: password without uppercase accepted", builds=("1.0", "1.1"))
def test_password_requires_uppercase(register_page):                              # REQ-004
    register(register_page, password="password1x")
    expect(register_page.alerts.first).to_be_visible()


@pytest.mark.parametrize("password", ["Pass1", "Passw0rd" + "x" * 13], ids=["7-chars-or-less", "21-chars"])
def test_password_length_boundaries_rejected(register_page, password):           # REQ-004 BVA (5 and 21 chars)
    register(register_page, password=password)
    expect(register_page.alerts.first).to_be_visible()


def test_confirm_password_mismatch_rejected(register_page):                       # REQ-005 - REGRESSION: breaks on build 1.1
    register(register_page, confirm="Different1Z")
    expect(register_page.alerts.first).to_be_visible()


@known("D5: confirm password compared case-insensitively", builds=("1.0",))
def test_confirm_password_is_case_sensitive(register_page):
    register(register_page, password="Passw0rdOK", confirm="passw0rdok")
    expect(register_page.alerts.first).to_be_visible()


@known("D6: name of spaces accepted", builds=("1.0", "1.1"))
def test_name_of_spaces_only_rejected(register_page):                             # REQ-001
    register(register_page, name="   ")
    expect(register_page.alerts.first).to_be_visible()


@known("D7: terms not enforced", builds=("1.0",))
def test_terms_required(register_page):                                           # REQ-006
    register(register_page, terms=False)
    expect(register_page.alerts.first).to_be_visible()


def test_duplicate_email_rejected(register_page, registered_user):
    register_page.open().register(**registered_user)
    expect(register_page.alerts.first).to_contain_text("already registered")
