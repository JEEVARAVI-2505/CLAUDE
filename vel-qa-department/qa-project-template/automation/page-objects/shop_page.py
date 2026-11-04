"""Page object: Shop view (cart, quantity, totals)."""
from playwright.sync_api import Page


class ShopPage:
    def __init__(self, page: Page, url: str):
        self.page, self.url = page, url
        self.total = page.locator("#cart-total")
        self.gst = page.locator("#cart-gst")

    def open(self) -> "ShopPage":
        self.page.goto(f"{self.url}#/shop")
        return self

    def add(self, product: str, qty) -> None:
        self.page.get_by_label(f"Quantity for {product}").fill(str(qty))
        self.page.get_by_role("button", name=f"Add {product}").click()

    def error_for(self, product: str):
        return self.page.get_by_label(f"Quantity for {product}").locator("xpath=following-sibling::*[@role='alert']")
