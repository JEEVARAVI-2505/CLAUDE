"""Page object: Login view. The e-mail field has no label in the app (a11y defect), so it is located by id."""
from playwright.sync_api import Page


class LoginPage:
    def __init__(self, page: Page, url: str):
        self.page, self.url = page, url
        self.email = page.locator("#login-email")          # no label in the app (a11y defect) - use the id
        self.password = page.locator("#login-password")
        self.submit_btn = page.get_by_role("button", name="Log in")
        self.alerts = page.get_by_role("alert")
        self.status = page.get_by_role("status")

    def open(self) -> "LoginPage":
        self.page.goto(f"{self.url}#/login")
        return self

    def login(self, email: str, password: str) -> None:
        self.email.fill(email)
        self.password.fill(password)
        self.submit_btn.click()
