# VEL Software Architecture & Consulting Handbook (VEL-SA-001 v1.0)

Branded handbook, templates and tooling for the VEL Software Architecture & Technical Consulting Department.

| Deliverable | File |
|---|---|
| Handbook (PDF, 92 pages, A4, watermark, bookmarks) | `output/VEL-Software-Architecture-Handbook.pdf` |
| Handbook (standalone responsive HTML: search, nav, dark mode, print) | `output/VEL-Software-Architecture-Handbook.html` |
| Handbook (Markdown) | `output/VEL-Software-Architecture-Handbook.md` |
| Employee onboarding guide | `output/templates/Employee-Onboarding-Guide.pdf` |
| 90-day training roadmap | `output/templates/90-Day-Training-Roadmap.pdf` |
| Client discovery questionnaire | `output/templates/Client-Discovery-Questionnaire.pdf` |
| Architecture assessment template | `output/templates/Architecture-Assessment-Template.pdf` |
| Requirements specification | `output/templates/Requirements-Specification-Template.pdf` |
| ADR template | `output/templates/ADR-Template.pdf` |
| Technical feasibility report template | `output/templates/Technical-Feasibility-Report-Template.pdf` |
| Technology comparison matrix | `output/templates/Technology-Comparison-Matrix.pdf` |
| Architecture review checklist | `output/templates/Architecture-Review-Checklist.pdf` |
| Cost estimation template (PDF + live-formula XLSX) | `output/templates/Cost-Estimation-Template.pdf`, `output/Cost-Estimation-Template.xlsx` |
| Client presentation template | `output/templates/Client-Presentation-Template.pdf` |
| Engineering handover checklist | `output/templates/Engineering-Handover-Checklist.pdf` |
| Extra: lesson, risk/assumption, stakeholder templates | `output/templates/*.pdf` |
| Editable Markdown copies of every template | `output/templates/*.md` |
| Architecture repository skeleton | `output/architecture-project-template/` |
| Link checker for official sources | `output/check-links.sh` |

## Rebuild
```bash
python3 build/build.py && python3 build/extras.py && node build/validate.js
```
Requires Python 3 (pypdf, openpyxl) and Node with Playwright/Chromium and the Inter font installed.

## Notes
- Prices, rates and the GreenLeaf client are **illustrative / fictional**.
- Official-source links could not be machine-checked in the build sandbox; run `output/check-links.sh`.
