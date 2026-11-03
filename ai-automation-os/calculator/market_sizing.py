"""Transparent bottom-up TAM / SAM / SOM.

TAM = sum over segments of firms x ACV                       (everyone who could buy)
SAM = sum over segments of firms x f_geo x f_fit x ACV       (reachable with our channels AND product fit)
SOM = min(SAM customers x share, capacity x months) x ACV    (what we can realistically win and onboard)

Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com
"""
from __future__ import annotations
import json
from pathlib import Path


def load(path=None) -> dict:
    return json.loads(Path(path or Path(__file__).with_name("market_inputs.json")).read_text())


def size(cfg: dict) -> dict:
    acv = cfg["acv_inr"]["value"]
    tam_firms = sum(s["firms"] for s in cfg["segments"])
    sam_firms = sum(s["firms"] * s["f_geo"] * s["f_fit"] for s in cfg["segments"])
    som = cfg["som"]
    som_customers = min(sam_firms * som["share_of_sam_in_3_years"], som["onboarding_capacity_per_month"] * som["months"])
    return {"tam_firms": tam_firms, "tam_inr": tam_firms * acv,
            "sam_firms": sam_firms, "sam_inr": sam_firms * acv,
            "som_customers": som_customers, "som_inr": som_customers * acv,
            "binding_limit": "capacity" if som["onboarding_capacity_per_month"] * som["months"] < sam_firms * som["share_of_sam_in_3_years"] else "market share"}


def unverified(cfg: dict) -> list[str]:
    out = [k for k in ("acv_inr", "som") if cfg[k].get("verified") is not True]
    out += [s["name"] for s in cfg["segments"] if s.get("verified") is not True]
    return out


if __name__ == "__main__":
    c = load(); r = size(c); crore = 1e7
    print("UNVERIFIED INPUTS:", len(unverified(c)))
    print(f"TAM {r['tam_firms']:,.0f} firms  = INR {r['tam_inr']/crore:,.1f} crore / year")
    print(f"SAM {r['sam_firms']:,.0f} firms  = INR {r['sam_inr']/crore:,.2f} crore / year")
    print(f"SOM {r['som_customers']:,.0f} customers = INR {r['som_inr']/crore:,.2f} crore ARR (limited by {r['binding_limit']})")
