"""Page object: Register view of the VEL Practice Shop. Locators and actions only; assertions belong in the tests."""
from playwright.sync_api import Page


class RegisterPage:
    def __init__(self, page: Page, url: str):
        self.page, self.url = page, url
        self.name = page.get_by_label("Full name")
        self.email = page.get_by_label("Email")
        self.age = page.get_by_label("Age")
        self.password = page.get_by_label("Password", exact=True)
        self.confirm = page.get_by_label("Confirm password")
        self.terms = page.get_by_label("I accept the terms")
        self.submit_btn = page.get_by_role("button", name="Register")
        self.alerts = page.get_by_role("alert")

    def open(self) -> "RegisterPage":
        self.page.goto(f"{self.url}#/register")
        return self

    def register(self, name="Asha Rao", email="qa@example.test", age=35, password="Passw0rdOK", confirm=None, terms=True):
        self.name.fill(name)
        self.email.fill(email)
        self.age.fill(str(age))
        self.password.fill(password)
        self.confirm.fill(password if confirm is None else confirm)
        if terms:
            self.terms.check()
        self.submit_btn.click()
