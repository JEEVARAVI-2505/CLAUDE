#!/usr/bin/env python3
"""Generate the costing workbook with live formulas from the same inputs as costing.py (placeholders only)."""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))
import costing as C
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Protection
from openpyxl.worksheet.datavalidation import DataValidation
from openpyxl.formatting.rule import CellIsRule

NAVY, INP = "14306B", "0000FF"
H = Font(bold=True, color="FFFFFF"); HF = PatternFill("solid", fgColor=NAVY)
I = Font(color=INP); B = Font(bold=True)
wb = Workbook()

def head(ws, row, vals):
    for i, v in enumerate(vals, 1):
        c = ws.cell(row, i, v); c.font, c.fill = H, HF; c.alignment = Alignment(wrap_text=True, vertical="top")

def inp(c, v, fmt=None):
    c.value, c.font = v, I
    c.fill = PatternFill("solid", fgColor="EEF2F9")
    if fmt: c.number_format = fmt

ws = wb.active; ws.title = "README"
for r, t in enumerate(["VEL Software Solutions - Web Project Costing Workbook (VEL-WD-001 SM-09)",
    "ALL rates and vendor figures are PLACEHOLDERS in generic currency units (CU). Replace from official vendor pages; log each in VendorLog.",
    "Blue cells on a tinted background are inputs; everything else is a formula. Taxes are excluded.",
    "Margin is not markup: price = cost / (1 - margin). Prices round UP to the step on Assumptions.",
    "Version 1.0 - see handbook Section 8 and Appendix A9 for the method."], 1):
    ws.cell(r, 1, t).font = Font(bold=(r == 1), size=13 if r == 1 else 11)
ws.column_dimensions["A"].width = 130

# ---- Assumptions
a = wb.create_sheet("Assumptions"); a.column_dimensions["A"].width = 46
for c in "BCD": a.column_dimensions[c].width = 16
head(a, 1, ["Rate card (PLACEHOLDER)", "Cost per hour (CU)"])
roles = list(C.RATES)
for i, (k, v) in enumerate(C.RATES.items(), 2):
    a.cell(i, 1, k); inp(a.cell(i, 2), v)
rate_rng = f"Assumptions!$A$2:$B${len(roles) + 1}"
head(a, 13, ["Parameter", "Small", "Medium", "Enterprise"])
rows = [("Contingency %", C.CONTINGENCY), ("Target gross margin %", C.TARGET_MARGIN), ("Third-party one-time cost (CU)", C.ONE_TIME_THIRD_PARTY)]
for r, (n, d) in enumerate(rows, 14):
    a.cell(r, 1, n)
    for j, s in enumerate(C.SIZES, 2):
        inp(a.cell(r, j), d[s], "0.0%" if "%" in n else "#,##0")
singles = [("Commission % (variable)", C.COMMISSION, "0.0%"), ("Annual fixed costs (CU)", C.FIXED_ANNUAL, "#,##0"), ("Price rounding step (CU)", C.ROUND_TO, "0"),
           ("Service contingency %", C.OPS_CONTINGENCY, "0.0%"), ("Service target margin %", C.OPS_MARGIN, "0.0%"),
           ("Vendor inflation % (renewal)", C.VENDOR_INFLATION, "0.0%"), ("Wage inflation % (renewal)", C.WAGE_INFLATION, "0.0%")]
for r, (n, v, f) in enumerate(singles, 18):
    a.cell(r, 1, n); inp(a.cell(r, 2), v, f)
COMM, FIX, RND, OCON, OMAR, VINF, WINF = [f"Assumptions!$B${r}" for r in range(18, 25)]
dv = DataValidation(type="decimal", operator="between", formula1="0", formula2="0.9"); a.add_data_validation(dv)
dv.add("B14:D15"); dv.add("B18"); dv.add("B21:B24")

# ---- Estimate
e = wb.create_sheet("Estimate"); e.column_dimensions["A"].width = 44; e.column_dimensions["B"].width = 22
for c in "CDEFGH": e.column_dimensions[c].width = 15
head(e, 1, ["Work item", "Role", "Small (h)", "Medium (h)", "Enterprise (h)", "Small cost", "Medium cost", "Enterprise cost"])
n = len(C.LINES); last = n + 1
dvr = DataValidation(type="list", formula1=f"=Assumptions!$A$2:$A${len(roles) + 1}"); e.add_data_validation(dvr)
for i, (name, role, *h) in enumerate(C.LINES, 2):
    e.cell(i, 1, name); inp(e.cell(i, 2), role); dvr.add(e.cell(i, 2))
    for j in range(3):
        inp(e.cell(i, 3 + j), h[j], "#,##0.##")
        col = "CDE"[j]
        e.cell(i, 6 + j, f"={col}{i}*VLOOKUP($B{i},{rate_rng},2,FALSE)").number_format = "#,##0"
R = last + 2
labels = ["Total hours", "Labour subtotal", "Third-party one-time", "Subtotal", "Contingency", "TOTAL DELIVERY COST", "SELLING PRICE", "Gross profit",
          "Gross margin %", "Markup %", "Variable costs", "Contribution margin", "Contribution margin %", "Break-even projects / year",
          "Blended cost / hour", "Break-even hours"]
for k, l in enumerate(labels): e.cell(R + k, 1, l).font = B
for j, s in enumerate(C.SIZES):
    hc, cc, ac = "CDE"[j], "FGH"[j], "BCD"[j]; col = "CDE"[j]
    r = lambda k: f"{col}{R + k}"
    f = {0: f"=SUM({hc}2:{hc}{last})", 1: f"=SUM({cc}2:{cc}{last})", 2: f"=Assumptions!{ac}16", 3: f"={r(1)}+{r(2)}",
         4: f"={r(3)}*Assumptions!{ac}14", 5: f"={r(3)}+{r(4)}", 6: f"=CEILING({r(5)}/(1-Assumptions!{ac}15),{RND})",
         7: f"={r(6)}-{r(5)}", 8: f"={r(7)}/{r(6)}", 9: f"={r(7)}/{r(5)}", 10: f"={r(2)}+{COMM}*{r(6)}", 11: f"={r(6)}-{r(10)}",
         12: f"={r(11)}/{r(6)}", 13: f"={FIX}/{r(11)}", 14: f"={r(1)}/{r(0)}",
         15: f"=({r(6)}-{r(2)}-{r(4)}-{COMM}*{r(6)})/{r(14)}"}
    for k, v in f.items():
        c = e.cell(R + k, 3 + j, v); c.number_format = "0.0%" if k in (8, 9, 12) else ("#,##0.00" if k in (13, 14) else "#,##0")
        if k in (5, 6): c.font = B
e.conditional_formatting.add(f"C{R + 8}:E{R + 8}", CellIsRule(operator="lessThan", formula=["0.30"], fill=PatternFill("solid", bgColor="F8CBAD")))

# ---- Operating
o = wb.create_sheet("Operating"); o.column_dimensions["A"].width = 52
for c in "BCD": o.column_dimensions[c].width = 16
head(o, 1, ["Vendor pass-through per month (PLACEHOLDER - from calculators)", "Small", "Medium", "Enterprise"])
for i, row in enumerate(C.OPS, 2):
    o.cell(i, 1, row[0])
    for j in range(3): inp(o.cell(i, 2 + j), row[1 + j], "#,##0.00")
v_end = len(C.OPS) + 1; V = v_end + 1
o.cell(V, 1, "Vendor total / month").font = B
head(o, V + 2, ["Service labour (hours per month)", "Small", "Medium", "Enterprise", "Role"])
for i, (nme, role, *h) in enumerate(C.OPS_LABOUR, V + 3):
    o.cell(i, 1, nme); o.cell(i, 5, role)
    for j in range(3): inp(o.cell(i, 2 + j), h[j], "#,##0.0")
l0, l1 = V + 3, V + 2 + len(C.OPS_LABOUR); L = l1 + 1
o.cell(L, 1, "Service labour cost / month").font = B
labels = ["Service contingency", "MONTHLY OPERATING COST", "ANNUAL OPERATING COST", "Monthly service fee (MRR)", "Annual service fee (ARR)", "Service gross margin %",
          "Next-year monthly cost", "RENEWAL FEE / MONTH", "RENEWAL FEE / YEAR"]
M = L + 2
for k, l in enumerate(labels): o.cell(M + k, 1, l).font = B
for j in range(3):
    col = "BCD"[j]
    o.cell(V, 2 + j, f"=SUM({col}2:{col}{v_end})").number_format = "#,##0.00"
    o.cell(L, 2 + j, "=" + "+".join(f"{col}{i}*VLOOKUP($E{i},{rate_rng},2,FALSE)" for i in range(l0, l1 + 1))).number_format = "#,##0.00"
    m = lambda k: f"{col}{M + k}"
    f = {0: f"=({col}{V}+{col}{L})*{OCON}", 1: f"=({col}{V}+{col}{L})+{m(0)}", 2: f"={m(1)}*12", 3: f"=CEILING({m(1)}/(1-{OMAR}),5)", 4: f"={m(3)}*12",
         5: f"=({m(3)}-{m(1)})/{m(3)}", 6: f"=({col}{V}*(1+{VINF})+{col}{L}*(1+{WINF}))*(1+{OCON})", 7: f"=MAX({m(3)},CEILING({m(6)}/(1-{OMAR}),5))", 8: f"={m(7)}*12"}
    for k, v in f.items(): o.cell(M + k, 2 + j, v).number_format = "0.0%" if k == 5 else "#,##0.00"

# ---- Scenarios / VendorLog / Summary
s = wb.create_sheet("Scenarios"); s.column_dimensions["A"].width = 16
head(s, 1, ["Active users", "Compute", "Database", "Storage", "Network/CDN", "Monitoring", "Total / month", "Cost per user"])
for i, u in enumerate([100, 1000, 10000, 100000], 2):
    inp(s.cell(i, 1), u, "#,##0")
    for c in range(2, 7): inp(s.cell(i, c), None, "#,##0.00")
    s.cell(i, 7, f"=SUM(B{i}:F{i})").number_format = "#,##0.00"
    s.cell(i, 8, f"=IF(A{i}=0,0,G{i}/A{i})").number_format = "#,##0.0000"
s.cell(7, 1, "Fill from the official calculators (region, date, assumptions in VendorLog).")
v = wb.create_sheet("VendorLog")
head(v, 1, ["Vendor", "Item", "Plan", "Region", "Currency", "Unit price", "Usage assumption", "Official URL", "Date checked", "Checked by", "Reviewer"])
for c, w in zip("ABCDEFGHIJK", [16, 24, 16, 14, 10, 12, 30, 48, 14, 14, 14]): v.column_dimensions[c].width = w
v.conditional_formatting.add("A2:K200", CellIsRule(operator="equal", formula=['""'], fill=PatternFill("solid", bgColor="FFFFFF")))
sm = wb.create_sheet("Summary", 1); sm.column_dimensions["A"].width = 44
head(sm, 1, ["Result (CU)", "Small", "Medium", "Enterprise"])
items = [("Total delivery cost", R + 5), ("Selling price", R + 6), ("Gross margin %", R + 8), ("Contribution margin", R + 11), ("Break-even projects / year", R + 13)]
for i, (n_, row) in enumerate(items, 2):
    sm.cell(i, 1, n_)
    for j in range(3): sm.cell(i, 2 + j, f"=Estimate!{'CDE'[j]}{row}").number_format = "0.0%" if "%" in n_ else "#,##0.0" if "Break" in n_ else "#,##0"
for k, (n_, row) in enumerate([("Monthly operating cost", M + 1), ("Annual operating cost", M + 2), ("Monthly service fee (MRR)", M + 3), ("Annual service fee (ARR)", M + 4), ("Renewal fee / month", M + 7)], 7):
    sm.cell(k, 1, n_)
    for j in range(3): sm.cell(k, 2 + j, f"=Operating!{'BCD'[j]}{row}").number_format = "#,##0"
for ws_ in wb.worksheets: ws_.freeze_panes = "A2" if ws_.title not in ("README",) else None
out = Path(__file__).parent.parent / "dist" / "VEL-Web-Project-Costing-Workbook.xlsx"
wb.save(out); print(out)
