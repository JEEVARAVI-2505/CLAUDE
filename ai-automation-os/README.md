<p align="center"><img src="assets/vel-logo-small.webp" alt="VEL Software Solutions" width="140"></p>

# AI Automation Engineering Department — Operating System

Separate from the WPF "AI Building Studio" project in this repo.

**New employees start here: `dist/VEL-Delivery-Handbook.pdf` (100 pages, printable) — source in `handbook/`.**

| File | Purpose |
|---|---|
| `00-intake-and-assumptions.md` | Intake answers, assumptions to confirm, opportunity ranking |
| `01-operating-framework.md` | 12 gates, tech-selection rules, workflow standard, roles, security, deployment, maintenance, portal build order |
| `02-sample-lifecycle-gst-bill-automation.md` | Full 24-stage lifecycle with costs, pricing, ROI, break-even |
| `03-sop-and-template-pack.md` | 14 fill-in templates: interview guide, process sheet, feasibility, I/O freeze, agent spec, eval plan, security checklist, risks, commercial docs, status, readiness, maintenance, sign-off |
| `04-technology-competitors-and-costing-reference.md` | Competitor map, platform/framework/model selection, every costing formula, sensitivity checks |
| `05-people-portal-and-support.md` | Role charters, RACI by gate, portal spec, support levels, operating rhythm |
| `handbook/` | **Delivery Handbook**: how a software company works, folder structure, market research and TAM/SAM/SOM, 15 industry pages, ten solution families (A–J), platform matrix, disaster recovery and maintenance packages, 15 stages (S00–S14), appendices incl. a coverage map of the master prompt |
| `reference-project/` | Small working project (23 tests): GST rules, AI-step safety wrapper, FastAPI service, Dockerfile |
| `dist/` | Built handbook (PDF and HTML); rebuild with `python scripts/build_handbook.py && node scripts/make_pdf.js && python scripts/build_handbook_web.py` (`handbook.html` is the responsive web version) |
| `calculator/` | `cost_model.py` + golden tests, `pricing_inputs.json`, `report.py`, `sensitivity.py`, `market_sizing.py`, `opportunity_score.py` |

Run: `cd calculator && pip install pytest && python -m pytest -q && python report.py`

**Pricing status: UNVERIFIED.** Provider/vendor pricing could not be fetched (network allowlist). `assert_quotable()` raises until every price input is verified from its official source.

---

<p align="center"><img src="assets/vel-logo-small.webp" alt="VEL Software Solutions" width="120"><br>
<sub>Developed by <b>V. LOKESHWARAN</b> · VEL Software Solutions · <a href="mailto:velsoftwaresolutions.in@gmail.com">velsoftwaresolutions.in@gmail.com</a></sub></p>
