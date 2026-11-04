"""Generate the synthetic 'Sunrise Retail' sales dataset for Assignment A1.

All data is fake. A fixed seed makes the output identical on every machine.
Deliberate defects are injected so the trainee can practise a data audit.
"""
import csv
import random
from datetime import date, timedelta
from pathlib import Path

SEED = 2505
N_ROWS = 500
OUT = Path(__file__).parent / "data" / "sales_raw.csv"

REGIONS = ["North", "South", "East", "West"]
PRODUCTS = {"P01": ("Notebook", 120.0), "P02": ("Pen Pack", 60.0),
            "P03": ("Backpack", 850.0), "P04": ("Desk Lamp", 640.0),
            "P05": ("Water Bottle", 310.0)}


def build_rows(rng: random.Random) -> list[dict]:
    start = date(2025, 1, 1)
    rows = []
    for i in range(1, N_ROWS + 1):
        pid = rng.choice(list(PRODUCTS))
        qty = rng.randint(1, 10)
        price = PRODUCTS[pid][1]
        rows.append({
            "order_id": f"ORD{i:04d}",
            "order_date": (start + timedelta(days=rng.randint(0, 179))).isoformat(),
            "customer_id": f"C{rng.randint(1, 80):03d}",
            "region": rng.choice(REGIONS),
            "product_id": pid,
            "quantity": qty,
            "unit_price": price,
            "revenue": round(qty * price, 2),
        })
    return rows


def inject_defects(rows: list[dict], rng: random.Random) -> None:
    """Break the data on purpose (about 4% of rows)."""
    for r in rng.sample(rows, 10):
        r["region"] = ""                       # missing value
    for r in rng.sample(rows, 8):
        r["region"] = r["region"].lower() or "north"  # inconsistent case
    for r in rng.sample(rows, 5):
        r["quantity"] = -abs(int(r["quantity"]))      # impossible value
    for r in rng.sample(rows, 6):
        d = date.fromisoformat(r["order_date"])
        r["order_date"] = d.strftime("%d/%m/%Y")      # wrong date format
    for r in rng.sample(rows, 4):
        r["revenue"] = round(float(r["revenue"]) * 1.5, 2)  # wrong calculation
    rows.extend(dict(r) for r in rng.sample(rows, 5))        # duplicate rows


def main() -> None:
    rng = random.Random(SEED)
    rows = build_rows(rng)
    inject_defects(rows, rng)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    with OUT.open("w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=list(rows[0]))
        w.writeheader()
        w.writerows(rows)
    print(f"Wrote {len(rows)} rows to {OUT}")


if __name__ == "__main__":
    main()
