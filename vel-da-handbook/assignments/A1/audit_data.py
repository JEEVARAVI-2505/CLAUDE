"""Data-quality audit for sales_raw.csv (Stage 04 practice).

Usage:  python audit_data.py [path/to/file.csv]
Read-only: never edits the source file.
"""
import sys
from pathlib import Path

import pandas as pd

path = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).parent / "data" / "sales_raw.csv"
df = pd.read_csv(path, dtype=str, keep_default_na=False)   # read everything as text first

print(f"File: {path.name}")
print(f"Rows: {len(df)}   Columns: {len(df.columns)}")

blank = {c: int((df[c].str.strip() == "").sum()) for c in df.columns}
print("\n1. Missing values (blank cells):")
for c, n in blank.items():
    if n:
        print(f"   {c}: {n}")

print(f"\n2. Fully duplicated rows: {int(df.duplicated().sum())}")
print(f"   Duplicate order_id values: {int(df['order_id'].duplicated().sum())}")

dates = pd.to_datetime(df["order_date"], format="%Y-%m-%d", errors="coerce")
print(f"\n3. order_date not in YYYY-MM-DD format: {int(dates.isna().sum())}")

qty = pd.to_numeric(df["quantity"], errors="coerce")
print(f"\n4. quantity <= 0: {int((qty <= 0).sum())}")

reg = df.loc[df["region"].str.strip() != "", "region"]
bad_case = int((reg != reg.str.title()).sum())
print(f"\n5. region values with inconsistent case: {bad_case}")

calc = (qty * pd.to_numeric(df["unit_price"], errors="coerce")).round(2)
mismatch = int((calc != pd.to_numeric(df["revenue"], errors="coerce")).sum())
print(f"\n6. revenue != quantity x unit_price: {mismatch}")

print("\nVerdict: DO NOT use for reporting until defects are resolved with the data owner.")
