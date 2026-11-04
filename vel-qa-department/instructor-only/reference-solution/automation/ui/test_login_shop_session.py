"""REQ-008..REQ-014: login, lock-out, shop, session, responsive, accessibility."""
import re

import pytest
from markers import known
from playwright.sync_api import expect

WELCOME = re.compile("Welcome")


@pytest.mark.smoke
def test_login_success_opens_shop(page, login_page, registered_user):
    login_page.open().login(registered_user["email"], registered_user["password"])
    expect(page.get_by_role("heading", name=WELCOME)).to_be_visible()             # REQ-008


def test_wrong_password_shows_generic_message(login_page, registered_user):
    login_page.open().login(registered_user["email"], "WrongPass1")
    expect(login_page.alerts.first).to_have_text("Invalid email or password")


@known("D8: unknown e-mail reveals 'Email not registered'", builds=("1.0", "1.1"))
def test_unknown_email_shows_same_generic_message(login_page):
    login_page.open().login("nobody@example.test", "WrongPass1")
    expect(login_page.alerts.first).to_have_text("Invalid email or password")


@known("D9: lock happens on the 4th failure, not the 3rd", builds=("1.0", "1.1"))
def test_account_locks_on_third_failure(login_page, registered_user):
    login_page.open()
    for _ in range(3):
        login_page.login(registered_user["email"], "WrongPass1")
    expect(login_page.alerts.first).to_contain_text("Account locked")


# ---- shop (REQ-010) ----
def test_cart_lists_added_item(shop, page):
    shop.add("Notebook", 3)
    expect(page.locator("#cart")).to_contain_text("Notebook × 3")


def test_quantity_10_accepted(shop, page):
    shop.add("Pen", 10)
    expect(page.locator("#cart")).to_contain_text("Pen × 10")


def test_quantity_6_accepted(shop, page):                                         # REGRESSION: build 1.1 caps quantity at 5
    shop.add("Pen", 6)
    expect(page.locator("#cart")).to_contain_text("Pen × 6")


@known("D12: quantity 11 accepted", builds=("1.0", "1.1"))
def test_quantity_11_rejected(shop):
    shop.add("Pen", 11)
    expect(shop.error_for("Pen")).to_be_visible()


@known("D12: quantity 0 accepted", builds=("1.0", "1.1"))
def test_quantity_0_rejected(shop):
    shop.add("Pen", 0)
    expect(shop.error_for("Pen")).to_be_visible()


@known("D10: total not shown to 2 decimals / floating-point noise", builds=("1.0", "1.1"))
@pytest.mark.parametrize("product,qty,expected", [("Notebook", 3, "30.30"), ("Pen", 3, "16.50")])
def test_total_is_two_decimals(shop, product, qty, expected):
    shop.add(product, qty)
    expect(shop.total).to_contain_text(expected)


@known("D11: GST floored instead of rounded", builds=("1.0", "1.1"))
def test_gst_rounded_to_two_decimals(shop):
    shop.add("Notebook", 1)                                                       # total 10.10 -> GST 1.818 -> 1.82
    expect(shop.gst).to_contain_text("1.82")


@known("D17: label typo 'Totl'", builds=("1.0", "1.1"))
def test_total_label_spelled_correctly(shop):
    shop.add("Pen", 1)
    expect(shop.total).to_contain_text("Total:")


# ---- session (REQ-011/012) ----
def _logged_in(page, base_url_with_params, user):
    from login_page import LoginPage
    LoginPage(page, base_url_with_params).open().login(user["email"], user["password"])
    expect(page.get_by_role("heading", name=WELCOME)).to_be_visible()


def test_session_expires_when_idle(page, app_url, registered_user):
    _logged_in(page, app_url + "&idle=3", registered_user)
    expect(page.get_by_role("alert")).to_contain_text("Session expired", timeout=6000)


@known("D13: idle timer is not reset by activity", builds=("1.0", "1.1"))
def test_activity_keeps_session_alive(page, app_url, registered_user):
    url = app_url + "&idle=4"
    page.goto(url + "#/login")
    from login_page import LoginPage
    LoginPage(page, url).login(registered_user["email"], registered_user["password"])
    expect(page.get_by_role("heading", name=WELCOME)).to_be_visible()
    page.wait_for_timeout(2500)                                                   # deliberate wait: this test is about elapsed time
    page.get_by_role("heading", name=WELCOME).click()                             # user activity at t = 2.5 s
    page.wait_for_timeout(2500)                                                   # t = 5 s: past the 4 s mark, but only 2.5 s since activity
    expect(page.get_by_role("heading", name=WELCOME)).to_be_visible()


@known("D14: Back button shows shop after logout", builds=("1.0", "1.1"))
def test_back_after_logout_does_not_show_shop(page, shop):
    page.get_by_role("link", name="Log out").click()
    expect(page.get_by_role("heading", name="Log in")).to_be_visible()
    page.go_back()
    expect(page.get_by_role("heading", name=WELCOME)).not_to_be_visible()


# ---- responsive and accessibility (REQ-013/014) ----
@known("D15: horizontal scroll at 360 px", builds=("1.0", "1.1"))
def test_no_horizontal_scroll_at_360px(page, register_page):
    page.set_viewport_size({"width": 360, "height": 740})
    register_page.open()
    assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth")


@known("D16a: login e-mail field has no label", builds=("1.0", "1.1"))
def test_login_email_has_accessible_label(page, login_page):
    login_page.open()
    expect(page.get_by_label("Email")).to_have_count(1)


@known("D16b: product images lack alt text", builds=("1.0", "1.1"))
def test_images_have_alt(shop, page):
    assert page.evaluate("[...document.images].every(i => i.hasAttribute('alt'))")


@known("D16c: focus indicator removed", builds=("1.0", "1.1"))
def test_focus_indicator_visible(page, register_page):
    register_page.open()
    page.locator("#reg-btn").focus()
    assert page.evaluate("getComputedStyle(document.querySelector('#reg-btn')).outlineStyle") != "none"
