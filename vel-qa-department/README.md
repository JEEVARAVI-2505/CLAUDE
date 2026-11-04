# VEL Software Solutions — QA Department package

| Deliverable | File |
|---|---|
| **QA Department Handbook** (PDF, A4) | `dist/VEL-QA-Department-Handbook.pdf` |
| **Handbook, standalone HTML** (searchable, responsive, printable, dark mode) | `dist/VEL-QA-Department-Handbook.html` |
| 90-day employee training plan | `dist/VEL-QA-90-Day-Training-Plan.pdf` |
| Templates pack + QA sign-off checklist | `dist/VEL-QA-Templates-Pack.pdf` |
| Practical exercise workbook (A0 + 8 projects) | `dist/VEL-QA-Practical-Exercise-Workbook.pdf` |
| Assessment and competency framework | `dist/VEL-QA-Assessment-Competency-Framework.pdf` |
| Repository skeleton, templates (MD/CSV), starter automation, CI, practice targets | `qa-project-template/` |
| Answer key + reference solution (**trainers only**) | `instructor-only/` |

Rebuild everything: `python3 build/build.py` (Python 3.10+, Pillow, Node + Playwright with Chromium; fonts via `build/fonts.py`).
Browser QA of the HTML: `node build/test_web.js dist/VEL-QA-Department-Handbook.html`.
