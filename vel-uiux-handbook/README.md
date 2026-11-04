# VEL UI/UX Department Handbook - Part 1 (VEL-UX-001 v1.0, draft)
- `dist/VEL-UIUX-Handbook-Part1.pdf` / `.html` - the deliverable (HTML is self-contained).
- `design-tokens/tokens.json` - palette, type, spacing tokens (source of truth).
- `parts/*.html` - content; `tools/build.py` builds HTML (contrast ratios computed); `tools/render_pdf.js` makes the PDF (needs Node + Playwright).
Rebuild: `python3 tools/build.py && node tools/render_pdf.js dist/VEL-UIUX-Handbook-Part1.html dist/VEL-UIUX-Handbook-Part1.pdf`
