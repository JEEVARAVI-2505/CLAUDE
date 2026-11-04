"""UI fixtures. Environment: BASE_URL (default http://localhost:8000), BUILD (1.0 or 1.1)."""
import os

import pytest
from login_page import LoginPage
from register_page import RegisterPage
from shop_page import ShopPage
from test_data import valid_user

BASE = os.getenv("BASE_URL", "http://localhost:8000").rstrip("/")
BUILD = os.getenv("BUILD", "1.0")


@pytest.fixture(scope="session")
def browser_type_launch_args(browser_type_launch_args):
    """Optional: point Playwright at an existing Chromium/Chrome (CHROMIUM_EXECUTABLE) when `playwright install` is not possible."""
    exe = os.getenv("CHROMIUM_EXECUTABLE")
    return {**browser_type_launch_args, **({"executable_path": exe} if exe else {})}


@pytest.fixture(scope="session")
def build() -> str:
    return BUILD


@pytest.fixture
def app_url() -> str:
    return f"{BASE}/?build={BUILD}"


@pytest.fixture
def register_page(page, app_url):
    return RegisterPage(page, app_url)


@pytest.fixture
def login_page(page, app_url):
    return LoginPage(page, app_url)


@pytest.fixture
def registered_user(page, app_url) -> dict:
    """Register a fresh synthetic user through the UI (each test has its own browser context, so its own storage)."""
    user = valid_user()
    reg = RegisterPage(page, app_url).open()
    reg.register(**user)
    LoginPage(page, app_url).status.wait_for()
    return user


@pytest.fixture
def shop(page, app_url, registered_user) -> ShopPage:
    login = LoginPage(page, app_url)
    login.login(registered_user["email"], registered_user["password"])
    page.get_by_role("heading", name="Welcome", exact=False).wait_for()
    return ShopPage(page, app_url)
