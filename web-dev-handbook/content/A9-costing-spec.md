This is the specification for the **Project Costing Workbook**. A working implementation is supplied as `VEL-Web-Project-Costing-Workbook.xlsx` (built to this specification, with live formulas). Use the workbook for every quotation; keep one copy per client version in the project folder.

## 1. Principles

- **Inputs are blue cells; formulas are black.** Inputs live only on `Assumptions`, `Estimate` hours, `Operating` vendor lines and `VendorLog`.
- **No vendor price is typed without a `VendorLog` row** (source URL, date checked, region, currency, plan, assumptions, checked-by).
- Currency is stated once on `Assumptions`; all other sheets inherit it. Taxes are excluded from every figure and added in the quotation.
- Margin and markup are separate, labelled outputs. Prices round **up** to the configured step.
- The workbook is **read-only for finished quotes** (save as PDF + archive); changes create a new version number.

## 2. Sheet specification

| Sheet | Purpose | Key contents |
|---|---|---|
| `README` | Instructions, colour legend, warnings, version | Placeholder warning; how to use; change log |
| `Assumptions` | Global inputs | Currency; rate card (role, cost/hour); contingency % per size; target margin % per size; commission %; annual fixed costs; price rounding; service contingency and margin; renewal inflation (vendor, wages) |
| `Estimate` | Project cost and price | Work items x role x hours (Small/Medium/Enterprise), cost per line, labour subtotal, third-party one-time, subtotal, contingency, total cost, selling price, gross profit, gross margin %, markup %, variable cost, contribution margin, break-even projects, break-even hours |
| `Operating` | Recurring cost and service price | Vendor pass-through lines, service labour (hours x rate), service contingency, monthly operating cost, annual operating cost, monthly fee, MRR, ARR, service margin, renewal fee |
| `Scenarios` | Usage-based cost at 100 / 1,000 / 10,000 / 100,000 users | Compute, database, storage, network, monitoring (inputs from calculator), total, cost per user, margin check |
| `VendorLog` | Evidence for every external price | Vendor, item, plan, region, currency, unit price, quantity assumption, URL, date checked, checked by, reviewer |
| `Summary` | One-page result for the quotation | Price, GM %, CM, break-even, monthly fee, annual fee, renewal |

## 3. Formulas (Excel notation)

| Output | Formula |
|---|---|
| Line cost | `=Hours * VLOOKUP(Role, RateCard, 2, FALSE)` |
| Labour | `=SUM(line costs)` |
| Subtotal | `=Labour + ThirdPartyOneTime` |
| Contingency | `=Subtotal * ContingencyPct` |
| Total delivery cost | `=Subtotal + Contingency` |
| Selling price | `=CEILING(TotalCost / (1 - TargetMargin), RoundTo)` |
| Gross profit | `=Price - TotalCost` |
| Gross margin % | `=GrossProfit / Price` |
| Markup % | `=GrossProfit / TotalCost` |
| Variable cost | `=ThirdPartyOneTime + Commission * Price` |
| Contribution margin | `=Price - VariableCost` |
| Break-even projects / year | `=AnnualFixed / ContributionMargin` |
| Blended rate | `=Labour / TotalHours` |
| Break-even hours | `=(Price - ThirdParty - Contingency - Commission*Price) / BlendedRate` |
| Monthly operating cost | `=(VendorTotal + ServiceLabour) * (1 + ServiceContingency)` |
| Annual operating cost | `=MonthlyCost * 12` |
| Monthly service fee | `=CEILING(MonthlyCost / (1 - ServiceMargin), 5)` |
| MRR / ARR | `=SUM(monthly fees)` / `=MRR * 12` |
| Next-year cost | `=(VendorTotal*(1+VendorInflation) + ServiceLabour*(1+WageInflation)) * (1 + ServiceContingency)` |
| Renewal fee / month | `=MAX(CurrentFee, CEILING(NextYearCost / (1 - ServiceMargin), 5))` |
| Cost per active user | `=TotalMonthlyCost / ActiveUsers` |

## 4. Validation and controls

| Control | Rule |
|---|---|
| Data validation | Margins and contingency between 0 and 0.9; hours >= 0; role chosen from the rate card list |
| Conditional formatting | Gross margin below policy minimum -> red; vendor price older than 30 days -> amber; missing `VendorLog` URL -> red |
| Protection | Formula cells locked; inputs unlocked |
| Cross-check | `Summary` totals equal the sum of detail sheets (a check cell shows OK/ERROR) |
| Review | Technical Lead reviews hours; PM reviews commercial terms; a second person re-opens at least 20% of vendor links |
| Versioning | File name `client-project-costing-vN.xlsx`; changes logged in `README` |

## 5. Quotation output checklist

- [ ] Scope and assumptions match the SOW
- [ ] Every vendor price traces to a `VendorLog` row dated within 30 days
- [ ] Contingency included; margin stated as **margin**, not markup
- [ ] Taxes excluded and stated; currency stated
- [ ] Monthly service fee, annual fee and renewal rule included
- [ ] Break-even and gross-margin check passed policy minimum
- [ ] Reviewed by Technical Lead and PM; approved by the authorised commercial representative
