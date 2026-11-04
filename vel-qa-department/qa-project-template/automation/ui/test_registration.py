"""Starter UI tests (Project 4 starts here). Extend them: boundaries, negative cases, login, shop, session, responsive, accessibility."""
import pytest
from markers import known
from playwright.sync_api import expect
from test_data import unique_email


@pytest.mark.smoke
def test_valid_registration_succeeds(register_page, login_page):
    register_page.open().register("Asha Rao", unique_email(), 35, "Passw0rdOK")
    expect(login_page.status).to_contain_text("Registration successful")            # REQ-007


@pytest.mark.parametrize("age", [17, "abc", "", "25.5", pytest.param(61, marks=known("DEF-002: age 61 accepted although REQ-003 maximum is 60"))])
def test_invalid_age_is_rejected(register_page, age):                                # REQ-003: invalid classes + neighbours of the boundaries
    register_page.open().register("Asha Rao", unique_email(), age, "Passw0rdOK")
    expect(register_page.alerts.first).to_be_visible()


@known("DEF-001: age 18 rejected although REQ-003 allows 18-60 inclusive")
def test_age_lower_boundary_18_accepted(register_page, login_page):
    register_page.open().register("Asha Rao", unique_email(), 18, "Passw0rdOK")
    expect(login_page.status).to_contain_text("Registration successful")
