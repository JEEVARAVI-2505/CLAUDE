# VEL Web Development Department Handbook (VEL-WD-001 v1.0)

Training, engineering SOP and client-delivery operating system for the web department. Designed after the VEL Cloud & DevOps Playbook (A4, navy/magenta, Inter + serif headings, dark code blocks, logo watermark).

## Deliverables (`dist/`)
| File | What |
|---|---|
| `VEL-Web-Development-Handbook.pdf` | 151-page handbook: cover, TOC with page numbers, header/footer, logo watermark, diagrams, 46 teaching modules, 27 lifecycle stages |
| `VEL-Web-Development-Handbook.html` | Standalone responsive edition: sidebar, search (`/`), expandable modules, copy buttons, progress tick-boxes, print CSS |
| `supporting-materials/SM-01..09 *.pdf` + `VEL-Supporting-Materials-Kit.html` | Onboarding checklist, 90-day plan, discovery questionnaire, requirements, SOW, QA, deployment, handover, costing spec |
| `VEL-Web-Project-Costing-Workbook.xlsx` | Live-formula costing workbook (placeholders only) |
| `../project-template/` | Starter repository structure (Section 6) |

## Rebuild
```bash
pip install markdown pyyaml pillow openpyxl fonttools brotli   # plus Node + Playwright with Chromium
python3 build/build.py all && python3 build/make_xlsx.py && python3 build/make_template.py
```
Content: `content/*.md` (sections), `content/modules/*.yaml` (modules enforce the 15-step method; stages enforce 10 attributes; the build fails if a field is missing). Costing numbers come from `build/costing.py` (single source for text and workbook).

## Important
All rates and vendor figures are **placeholders**; verify against official vendor pages before quoting (Section 8.10).
