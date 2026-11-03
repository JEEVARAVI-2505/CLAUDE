from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)
OK = {"vendor_gstin": "27AAPFU0939F1ZV", "invoice_no": "INV-001", "invoice_date": "2026-10-01",
      "taxable_value": "10000", "cgst": "900", "sgst": "900", "igst": "0", "total": "11800"}


def test_health():
    assert client.get("/health").json() == {"status": "ok"}


def test_good_bill_goes_to_auto_post():
    r = client.post("/bills/validate", json=OK)
    assert r.status_code == 200 and r.json() == {"passed": True, "errors": [], "route": "auto_post"}


def test_bad_total_goes_to_human_review():
    r = client.post("/bills/validate", json={**OK, "total": "12000"})
    assert r.json()["route"] == "human_review" and "TOTAL_MISMATCH" in r.json()["errors"]


def test_missing_field_is_rejected_with_422():
    bad = {k: v for k, v in OK.items() if k != "total"}
    assert client.post("/bills/validate", json=bad).status_code == 422
