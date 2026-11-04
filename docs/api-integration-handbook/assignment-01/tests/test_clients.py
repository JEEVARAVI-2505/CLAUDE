import pytest
from fastapi.testclient import TestClient

from app import main

KEY = {"X-API-Key": "test-key"}
GOOD = {"name": "Acme Traders", "email": "ops@acme.example", "industry": "retail"}


@pytest.fixture(autouse=True)
def fresh(monkeypatch):
    monkeypatch.setenv("CLIENT_API_KEY", "test-key")
    main.store.__init__()          # reset in-memory data between tests
    return TestClient(main.app)


@pytest.fixture
def c():
    return TestClient(main.app)


def test_health_is_public(c):
    assert c.get("/health").json() == {"status": "ok"}


def test_create_returns_201_and_location(c):
    r = c.post("/v1/clients", json=GOOD, headers=KEY)
    assert r.status_code == 201
    assert r.headers["Location"] == "/v1/clients/1"
    assert r.json()["status"] == "lead"


def test_missing_key_is_401_in_standard_shape(c):
    r = c.get("/v1/clients")
    assert r.status_code == 401
    assert r.json()["error"]["code"] == "unauthorized"


def test_wrong_key_is_401(c):
    assert c.get("/v1/clients", headers={"X-API-Key": "nope"}).status_code == 401


def test_validation_error_lists_fields(c):
    r = c.post("/v1/clients", json={"name": "A", "email": "bad"}, headers=KEY)
    assert r.status_code == 422
    fields = {d["field"] for d in r.json()["error"]["details"]}
    assert {"name", "email", "industry"} <= fields


def test_duplicate_email_is_409(c):
    c.post("/v1/clients", json=GOOD, headers=KEY)
    r = c.post("/v1/clients", json={**GOOD, "name": "Other"}, headers=KEY)
    assert r.status_code == 409


def test_get_unknown_is_404(c):
    assert c.get("/v1/clients/99", headers=KEY).status_code == 404


def test_patch_changes_only_sent_fields(c):
    c.post("/v1/clients", json=GOOD, headers=KEY)
    r = c.patch("/v1/clients/1", json={"status": "active"}, headers=KEY)
    assert r.json()["status"] == "active" and r.json()["name"] == "Acme Traders"


def test_delete_then_404(c):
    c.post("/v1/clients", json=GOOD, headers=KEY)
    assert c.delete("/v1/clients/1", headers=KEY).status_code == 204
    assert c.get("/v1/clients/1", headers=KEY).status_code == 404


def test_pagination_filter_sort(c):
    for i, (n, s) in enumerate([("Zeta Ltd", "active"), ("Alpha Ltd", "lead"), ("Mid Ltd", "active")]):
        c.post("/v1/clients", json={**GOOD, "name": n, "email": f"x{i}@e.example", "status": s}, headers=KEY)
    r = c.get("/v1/clients?status=active&sort=name&page_size=1&page=2", headers=KEY).json()
    assert r["total"] == 2 and [x["name"] for x in r["items"]] == ["Zeta Ltd"]


def test_bad_page_size_rejected(c):
    assert c.get("/v1/clients?page_size=1000", headers=KEY).status_code == 422


def test_request_id_echoed(c):
    r = c.get("/health", headers={"X-Request-ID": "abc-123"})
    assert r.headers["X-Request-ID"] == "abc-123"
