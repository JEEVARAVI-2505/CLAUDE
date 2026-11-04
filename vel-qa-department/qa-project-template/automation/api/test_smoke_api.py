"""Starter API tests (Project 5 starts here). Needs QA_EMAIL and QA_PASSWORD in the environment."""
import pytest
import requests
from api_helpers import schema_errors

T = 10


@pytest.mark.smoke
def test_health(base_url):
    r = requests.get(f"{base_url}/api/health", timeout=T)
    assert r.status_code == 200 and r.json()["status"] == "ok"


def test_login_with_wrong_password_returns_401(base_url):
    r = requests.post(f"{base_url}/api/auth/login", json={"email": "qa@example.test", "password": "wrong"}, timeout=T)
    assert r.status_code == 401


def test_product_1_matches_schema(base_url):
    r = requests.get(f"{base_url}/api/products/1", timeout=T)
    assert r.status_code == 200
    assert schema_errors(r.json()) == []


def test_create_requires_token(base_url):
    r = requests.post(f"{base_url}/api/products", json={"name": "x", "price": 1}, timeout=T)
    assert r.status_code == 401
