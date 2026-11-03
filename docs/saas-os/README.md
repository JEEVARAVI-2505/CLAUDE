# SaaS & Web Development Company Operating System

- `saas-operating-system.html` – responsive web version (live break-even/LTV calculator, light/dark).
- `saas-operating-system.pdf` – A4 print version (33 pages).
- `build/` – generator. `python3 docs/saas-os/build/build.py` rebuilds both (needs Python 3 + Node Playwright).
  All financial tables are computed in `build/calc.py`; edit the assumptions there and rebuild.

All unit prices, rates, churn, CAC etc. are labelled **ASSUMPTION**. Replace them with cited, dated official figures
before commercial use. No competitor data, domain availability or DNS values are invented.

## Version 1.1
- Added App-store fees section. Apple fees were fetched from Apple's official pages on 2026-11-03.
  Google Play fees are search-reported only (Google help pages were unreachable from the build environment) – re-verify.
- Added VEL Software Solutions logo, watermark and "Developed by V.LOKESHWARAN" credit on every page (PDF header/footer + web footer).
- Added per-stage precision notes and a 150-day rollout plan.
