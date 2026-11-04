"""API fixtures. Environment: API_BASE_URL (default http://localhost:8080), QA_EMAIL, QA_PASSWORD (never hard-code credentials)."""
import os
import uuid

import pytest
import requests

BASE = os.getenv("API_BASE_URL", "http://localhost:8080").rstrip("/")


@pytest.fixture(scope="session")
def base_url() -> str:
    return BASE


@pytest.fixture(scope="session")
def token(base_url) -> str:
    email, password = os.getenv("QA_EMAIL"), os.getenv("QA_PASSWORD")
    if not email or not password:
        pytest.skip("Set QA_EMAIL and QA_PASSWORD (see README) - credentials are never stored in the repository")
    r = requests.post(f"{base_url}/api/auth/login", json={"email": email, "password": password}, timeout=10)
    assert r.status_code == 200, r.text
    return r.json()["token"]


@pytest.fixture(scope="session")
def auth(token) -> dict:
    return {"Authorization": f"Bearer {token}"}


@pytest.fixture
def product(base_url, auth):
    """A product created for this test only; removed afterwards (tests stay independent)."""
    r = requests.post(f"{base_url}/api/products", json={"name": f"QA-{uuid.uuid4().hex[:8]}", "price": 9.99, "stock": 5}, headers=auth, timeout=10)
    assert r.status_code == 201, r.text
    p = r.json()
    yield p
    requests.delete(f"{base_url}/api/products/{p['id']}", headers=auth, timeout=10)
