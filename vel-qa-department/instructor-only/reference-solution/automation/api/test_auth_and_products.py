"""Practice API contract tests. Seeded defects (A1-A10) are strict xfails: see instructor-only/ANSWER-KEY.md."""
import uuid

import pytest
import requests
from api_helpers import schema_errors
from markers import known

T = 10


def url(base, path):
    return f"{base}{path}"


# ---------------------------------------------------------------- health, auth
@pytest.mark.smoke
def test_health(base_url):
    r = requests.get(url(base_url, "/api/health"), timeout=T)
    assert r.status_code == 200 and r.json() == {"status": "ok", "service": "vel-practice-api"}


def test_login_wrong_password_401(base_url):
    r = requests.post(url(base_url, "/api/auth/login"), json={"email": "qa@example.test", "password": "nope"}, timeout=T)
    assert r.status_code == 401


@pytest.mark.parametrize("body", [{}, {"email": "qa@example.test"}, {"password": "x"}], ids=["empty", "no-password", "no-email"])
def test_login_missing_field_400(base_url, body):
    assert requests.post(url(base_url, "/api/auth/login"), json=body, timeout=T).status_code == 400


@known("A7: empty password accepted for a valid e-mail", builds=("1.0",))
def test_login_empty_password_rejected(base_url):
    r = requests.post(url(base_url, "/api/auth/login"), json={"email": "qa@example.test", "password": ""}, timeout=T)
    assert r.status_code in (400, 401)


# ---------------------------------------------------------------- reading
def test_list_products_shape(base_url):
    r = requests.get(url(base_url, "/api/products?limit=3"), timeout=T)
    assert r.status_code == 200
    body = r.json()
    assert body["limit"] == 3 and len(body["items"]) == 3 and body["total"] >= 7
    assert r.headers["Content-Type"].startswith("application/json")


@pytest.mark.parametrize("query", ["limit=0", "page=0", "limit=-1", "limit=abc"])
def test_list_invalid_paging_400(base_url, query):
    assert requests.get(url(base_url, f"/api/products?{query}"), timeout=T).status_code == 400


@known("A8: limit above 50 is accepted", builds=("1.0",))
def test_list_limit_over_50_rejected(base_url):
    assert requests.get(url(base_url, "/api/products?limit=51"), timeout=T).status_code == 400


def test_get_product_matches_schema(base_url):
    r = requests.get(url(base_url, "/api/products/1"), timeout=T)
    assert r.status_code == 200 and schema_errors(r.json()) == []


@known("A6: price returned as string for product 3", builds=("1.0",))
def test_every_listed_product_matches_schema(base_url):
    items = requests.get(url(base_url, "/api/products?limit=50"), timeout=T).json()["items"]
    assert [(i["id"], schema_errors(i)) for i in items if schema_errors(i)] == []


def test_unknown_product_404(base_url):
    assert requests.get(url(base_url, "/api/products/99999"), timeout=T).status_code == 404


@known("A10: 404 body is HTML, not JSON", builds=("1.0",))
def test_unknown_product_404_is_json(base_url):
    assert requests.get(url(base_url, "/api/products/99999"), timeout=T).headers["Content-Type"].startswith("application/json")


@known("A3: non-integer id returns 500 and leaks internals", builds=("1.0",))
def test_non_integer_id_is_400(base_url):
    r = requests.get(url(base_url, "/api/products/abc"), timeout=T)
    assert r.status_code == 400 and "Traceback" not in r.text and ".py" not in r.text


def test_unsupported_method_405_with_allow(base_url):
    r = requests.delete(url(base_url, "/api/health"), timeout=T)
    assert r.status_code == 405 and "GET" in r.headers["Allow"]


# ---------------------------------------------------------------- writing (auth required)
@pytest.mark.parametrize("method,path", [("post", "/api/products"), ("put", "/api/products/1"), ("delete", "/api/products/1")])
def test_write_without_token_401(base_url, method, path):
    kwargs = {"json": {"name": "x", "price": 1}} if method != "delete" else {}
    assert getattr(requests, method)(url(base_url, path), timeout=T, **kwargs).status_code == 401


def test_write_with_bad_token_401(base_url):
    r = requests.post(url(base_url, "/api/products"), json={"name": "x", "price": 1}, headers={"Authorization": "Bearer not-a-token"}, timeout=T)
    assert r.status_code == 401


def test_create_product_201_with_location(base_url, auth):
    r = requests.post(url(base_url, "/api/products"), json={"name": f"QA-{uuid.uuid4().hex[:8]}", "price": 3.5, "stock": 10}, headers=auth, timeout=T)
    try:
        assert r.status_code == 201 and schema_errors(r.json()) == []
        assert r.headers["Location"] == f"/api/products/{r.json()['id']}"
    finally:
        requests.delete(url(base_url, f"/api/products/{r.json()['id']}"), headers=auth, timeout=T)


@pytest.mark.parametrize("body", [{"price": 1}, {"name": "", "price": 1}, {"name": "x" * 61, "price": 1}, {"name": "x", "price": -1},
                                  {"name": "x", "price": "5"}, {"name": "x", "price": 1, "stock": -1}, {"name": "x", "price": 1, "stock": 1.5}],
                         ids=["no-name", "empty-name", "name-61", "price-neg", "price-text", "stock-neg", "stock-decimal"])
def test_create_invalid_400(base_url, auth, body):
    assert requests.post(url(base_url, "/api/products"), json=body, headers=auth, timeout=T).status_code == 400


@known("A1: price 0 accepted", builds=("1.0",))
def test_create_price_zero_rejected(base_url, auth):
    r = requests.post(url(base_url, "/api/products"), json={"name": f"QA-{uuid.uuid4().hex[:8]}", "price": 0}, headers=auth, timeout=T)
    assert r.status_code == 400


@known("A2: duplicate name accepted", builds=("1.0",))
def test_create_duplicate_name_409(base_url, auth, product):
    r = requests.post(url(base_url, "/api/products"), json={"name": product["name"], "price": 1}, headers=auth, timeout=T)
    assert r.status_code == 409


def test_create_wrong_content_type_415(base_url, auth):
    r = requests.post(url(base_url, "/api/products"), data="name=x&price=1", headers={**auth, "Content-Type": "application/x-www-form-urlencoded"}, timeout=T)
    assert r.status_code == 415


def test_update_changes_name_and_price(base_url, auth, product):
    r = requests.put(url(base_url, f"/api/products/{product['id']}"), json={"name": product["name"], "price": 12.5, "stock": 5}, headers=auth, timeout=T)
    assert r.status_code == 200 and r.json()["price"] == 12.5


@known("A9: stock silently ignored on update", builds=("1.0",))
def test_update_changes_stock(base_url, auth, product):
    r = requests.put(url(base_url, f"/api/products/{product['id']}"), json={"name": product["name"], "price": 9.99, "stock": 77}, headers=auth, timeout=T)
    assert r.json()["stock"] == 77


def test_delete_returns_204_then_get_404(base_url, auth):
    created = requests.post(url(base_url, "/api/products"), json={"name": f"QA-{uuid.uuid4().hex[:8]}", "price": 1}, headers=auth, timeout=T).json()
    assert requests.delete(url(base_url, f"/api/products/{created['id']}"), headers=auth, timeout=T).status_code == 204
    assert requests.get(url(base_url, f"/api/products/{created['id']}"), timeout=T).status_code == 404


@known("A4: deleting an already-deleted product returns 204", builds=("1.0",))
def test_delete_twice_second_is_404(base_url, auth):
    created = requests.post(url(base_url, "/api/products"), json={"name": f"QA-{uuid.uuid4().hex[:8]}", "price": 1}, headers=auth, timeout=T).json()
    requests.delete(url(base_url, f"/api/products/{created['id']}"), headers=auth, timeout=T)
    assert requests.delete(url(base_url, f"/api/products/{created['id']}"), headers=auth, timeout=T).status_code == 404


@known("A5: /api/orders readable without authentication (SECURITY - escalate to the coordinator, do not publish)", builds=("1.0",))
def test_orders_require_authentication(base_url):
    assert requests.get(url(base_url, "/api/orders"), timeout=T).status_code == 401


def test_orders_with_token_200(base_url, auth):
    assert requests.get(url(base_url, "/api/orders"), headers=auth, timeout=T).status_code == 200


def test_slow_endpoint_respects_delay(base_url):
    r = requests.get(url(base_url, "/api/slow?delay=150"), timeout=T)
    assert r.status_code == 200 and 0.14 < r.elapsed.total_seconds() < 1.5
    assert requests.get(url(base_url, "/api/slow?delay=6000"), timeout=T).status_code == 400
