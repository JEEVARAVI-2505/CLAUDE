import pytest
import market_sizing as ms

def cfg(**over):
    c = {"acv_inr": {"value": 100000, "verified": True},
         "segments": [{"name": "A", "firms": 1000, "f_geo": 0.5, "f_fit": 0.2, "verified": True},
                      {"name": "B", "firms": 2000, "f_geo": 0.1, "f_fit": 0.5, "verified": True}],
         "som": {"share_of_sam_in_3_years": 0.10, "onboarding_capacity_per_month": 2, "months": 36, "verified": True}}
    c.update(over); return c

def test_tam_sam_som_golden():
    r = ms.size(cfg())
    # TAM firms 3,000 x 100,000 = 300,000,000
    assert r["tam_inr"] == 300_000_000
    # SAM firms = 1000*.5*.2 + 2000*.1*.5 = 100 + 100 = 200 -> 20,000,000
    assert r["sam_firms"] == pytest.approx(200) and r["sam_inr"] == pytest.approx(20_000_000)
    # SOM: 200 x 10% = 20 customers (capacity 72 not binding) -> 2,000,000
    assert r["som_customers"] == pytest.approx(20) and r["som_inr"] == pytest.approx(2_000_000)
    assert r["binding_limit"] == "market share"

def test_capacity_can_bind():
    c = cfg(); c["som"]["onboarding_capacity_per_month"] = 0.25   # 0.25 x 36 = 9 customers
    r = ms.size(c)
    assert r["som_customers"] == pytest.approx(9) and r["binding_limit"] == "capacity"

def test_placeholder_file_is_flagged_unverified():
    assert len(ms.unverified(ms.load())) == 4
