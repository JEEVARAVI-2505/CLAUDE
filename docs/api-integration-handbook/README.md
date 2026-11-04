# VEL API & System Integration Handbook (VEL-API-001 v1.0)

- `VEL-API-Integration-Handbook.pdf` - print edition (A4, 53 pages, logo watermark)
- `VEL-API-Integration-Handbook.html` - self-contained web edition (open in any browser)
- `src/` - Markdown source parts; `style.css`, `build.py`, `render_pdf.js` - build tooling
- `assignment-01/` - tested reference solution for the first practical assignment (`pip install -r requirements.txt && python -m pytest -q`)
- `snippets/` - tested retry / idempotency reference code

Rebuild: `python build.py && NODE_PATH=$(npm root -g) node render_pdf.js` (needs `markdown`, `pygments`, Playwright).
