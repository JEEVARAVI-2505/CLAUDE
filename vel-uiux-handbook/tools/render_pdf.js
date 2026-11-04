// Usage: node render_pdf.js <in.html> <out.pdf>
const { chromium } = require('playwright'); const path = require('path');
(async () => {
  const b = await chromium.launch(); const p = await b.newPage();
  await p.goto('file://' + path.resolve(process.argv[2])); await p.emulateMedia({ media: 'print' });
  const s = 'font-family:Inter,Arial,sans-serif;font-size:7.5px;color:#4B5563;width:100%;padding:0 16mm;display:flex;justify-content:space-between;';
  await p.pdf({ path: process.argv[3], format: 'A4', printBackground: true, displayHeaderFooter: true,
    margin: { top: '18mm', bottom: '18mm', left: '16mm', right: '16mm' },
    headerTemplate: `<div style="${s}"><span>VEL Software Solutions · UI/UX Department Handbook (VEL-UX-001 v1.0 · Part 1)</span><span>Confidential · internal use</span></div>`,
    footerTemplate: `<div style="${s}"><span>velsoftwaresolutions.in@gmail.com</span><span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>` });
  await b.close();
})();
