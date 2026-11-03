# AI Automation Engineering Department — Operating System

Separate from the WPF "AI Building Studio" project in this repo.

| File | Purpose |
|---|---|
| `00-intake-and-assumptions.md` | Intake answers, assumptions to confirm, opportunity ranking |
| `01-operating-framework.md` | 12 gates, tech-selection rules, workflow standard, roles, security, deployment, maintenance, portal build order |
| `02-sample-lifecycle-gst-bill-automation.md` | Full 24-stage lifecycle with costs, pricing, ROI, break-even |
| `calculator/` | `cost_model.py` + golden tests, `pricing_inputs.json`, `report.py`, `opportunity_score.py` |

Run: `cd calculator && pip install pytest && python -m pytest -q && python report.py`

**Pricing status: UNVERIFIED.** Provider/vendor pricing could not be fetched (network allowlist). `assert_quotable()` raises until every price input is verified from its official source.
