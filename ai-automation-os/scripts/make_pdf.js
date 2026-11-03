// Prints dist/VEL-Delivery-Handbook.html to PDF with header/footer on every page. Usage: node scripts/make_pdf.js
const { chromium } = require('playwright'); const path = require('path');
(async () => {
  const root = path.resolve(__dirname, '..');
  const b = await chromium.launch(); const p = await b.newPage();
  await p.goto('file://' + path.join(root, 'dist/VEL-Delivery-Handbook.html'));
  const style = 'font-size:8px;width:100%;padding:0 14mm;color:#5a6573;font-family:Helvetica,Arial,sans-serif;';
  await p.pdf({ path: path.join(root, 'dist/VEL-Delivery-Handbook.pdf'), format: 'A4', printBackground: true,
    margin: { top: '18mm', bottom: '20mm', left: '14mm', right: '14mm' }, displayHeaderFooter: true,
    headerTemplate: `<div style="${style}display:flex;justify-content:space-between"><span>VEL Software Solutions · Delivery Handbook (VEL-DH-001 v1.1)</span><span>Confidential · internal use</span></div>`,
    footerTemplate: `<div style="${style}display:flex;justify-content:space-between"><span>Developed by V. LOKESHWARAN · velsoftwaresolutions.in@gmail.com</span><span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>` });
  await b.close();
})();
