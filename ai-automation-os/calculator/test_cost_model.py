"""Golden tests. Each expected value is hand-calculated in the comment."""
import pytest
import cost_model as cm

M = {"in_per_mtok_usd": 3.0, "out_per_mtok_usd": 15.0}


def test_token_cost_golden():
    # 3,000 in x $3/M = $0.009 ; 600 out x $15/M = $0.009 ; total $0.018
    assert cm.token_cost_usd(M, 3000, 600) == pytest.approx(0.018)


def test_ai_cost_with_retry_and_escalation():
    cfg = cm.load()
    cfg["models"]["economical"].update(in_per_mtok_usd=1.0, out_per_mtok_usd=5.0)
    cfg["models"]["balanced"].update(in_per_mtok_usd=3.0, out_per_mtok_usd=15.0)
    cfg["fx"]["usd_inr"] = 100.0
    # base: 3000*1/1e6 + 600*5/1e6 = 0.003+0.003 = 0.006 ; x1.08 retry = 0.00648
    # esc : 0.15 * (3500*3/1e6 + 700*15/1e6) = 0.15 * (0.0105+0.0105) = 0.00315
    # total $0.00963 x 100 = INR 0.963
    assert cm.ai_cost_per_attempt_inr(cfg) == pytest.approx(0.963)


def test_human_review_cost_golden():
    cfg = cm.load()
    # 2 min x 0.20 x INR250/h / 60 = 1.6667
    assert cm.human_review_cost_per_exec_inr(cfg) == pytest.approx(100 / 60)


def test_failures_raise_cost_per_success():
    cfg = cm.load()
    v = cm.variable_cost_per_attempt_inr(cfg)
    assert cm.cost_per_successful_exec_inr(cfg) == pytest.approx(v / 0.97)


def test_dev_cost_golden():
    cfg = cm.load()
    # BA 40x500=20000; arch 32x1200=38400; AI 80x900=72000; dev 120x700=84000;
    # QA 60x500=30000; devops 24x800=19200; sec 16x1000=16000; PM 40x900=36000 => 315,600
    d = cm.dev_cost(cfg)
    assert d["labour"] == 315600
    assert d["loaded"] == pytest.approx(315600 * 1.2)
    assert d["pessimistic"] == pytest.approx(315600 * 1.2 * 1.4)


def test_project_price_golden():
    cfg = cm.load()
    # (378,720 + 0) x 1.15 / 0.65
    assert cm.project_price_inr(cfg)["price"] == pytest.approx(378720 * 1.15 / 0.65)


def test_overage_revenue():
    cfg = cm.load()
    # professional: 14,999 + (1,700-1,500) x 4.5 = 15,899
    assert cm.tier_revenue_inr(cfg, "professional", 1700) == pytest.approx(15899)


def test_roi_golden():
    cfg = cm.load()
    # 2000 inv x 6 min = 200 h ; x INR200 x 0.6 = 24,000 ; net 24,000-14,999 = 9,001
    r = cm.customer_roi(cfg, 2000, 14999)
    assert r["hours_saved"] == pytest.approx(200)
    assert r["net_monthly"] == pytest.approx(9001)
    assert r["payback_months"] == pytest.approx(40000 / 9001)


def test_roi_negative_has_no_payback():
    assert cm.customer_roi(cm.load(), 100, 14999)["payback_months"] is None


def test_quote_blocked_while_unverified():
    with pytest.raises(RuntimeError):
        cm.assert_quotable(cm.load())


def test_quote_allowed_when_all_verified():
    cfg = cm.load()
    def mark(n):
        if isinstance(n, dict):
            if "verified" in n: n["verified"] = True
            for v in n.values(): mark(v)
    mark(cfg)
    cm.assert_quotable(cfg)


def test_volume_table_monotonic():
    rows = cm.volume_table(cm.load())
    assert [r["total_inr"] for r in rows] == sorted(r["total_inr"] for r in rows)
    assert rows[-1]["per_success_inr"] < rows[0]["per_success_inr"]


def test_maintenance_package_golden():
    cfg = cm.load()
    # basic: 2 dev x 700 + 4 support x 400 = 1,400 + 1,600 = 3,000 ; / 0.65 = 4,615.38
    b = cm.maintenance_package_price_inr(cfg, "basic")
    assert b["cost"] == 3000 and b["price"] == pytest.approx(3000 / 0.65)
    # enterprise: 16x700 + 20x400 + 4x1200 = 11,200 + 8,000 + 4,800 = 24,000
    assert cm.maintenance_package_price_inr(cfg, "enterprise")["cost"] == 24000
    # extra work: 700 / 0.65 = 1,076.92 per hour
    assert b["extra_hour_rate"] == pytest.approx(700 / 0.65)
