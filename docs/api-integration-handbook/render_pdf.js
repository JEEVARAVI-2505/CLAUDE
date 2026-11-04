const { chromium } = require('playwright');
const path = require('path');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage();
  await p.goto('file://' + path.resolve(__dirname, 'VEL-API-Integration-Handbook.html'), { waitUntil: 'load' });
  const f = 'font-family:Inter,Arial,sans-serif;font-size:7.5px;color:#5a6275;width:100%;padding:0 14mm;';
  await p.pdf({
    path: path.resolve(__dirname, 'VEL-API-Integration-Handbook.pdf'),
    format: 'A4', printBackground: true, displayHeaderFooter: true,
    margin: { top: '20mm', bottom: '18mm', left: '14mm', right: '14mm' },
    headerTemplate: `<div style="${f}display:flex;justify-content:space-between"><span>VEL Software Solutions · API &amp; System Integration Handbook (VEL-API-001 v1.0)</span><span>Confidential · internal use</span></div>`,
    footerTemplate: `<div style="${f}display:flex;justify-content:space-between"><span>Developed by V. LOKESHWARAN · velsoftwaresolutions.in@gmail.com</span><span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>`,
  });
  await b.close();
})();
