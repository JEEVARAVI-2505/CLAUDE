// usage: node pdf.js input.html output.pdf "header-left" "footer-left"
const { chromium } = require('playwright');
const path = require('path');
(async () => {
  const [inp, out, hl, fl] = process.argv.slice(2);
  const b = await chromium.launch({ args: ['--no-sandbox'] });
  const p = await b.newPage();
  await p.goto('file://' + path.resolve(inp), { waitUntil: 'load' });
  await p.emulateMedia({ media: 'print' });
  const style = 'font-family:Inter,Arial,sans-serif;font-size:7.5px;color:#5b6578;width:100%;padding:0 16mm;display:flex;justify-content:space-between;';
  await p.pdf({
    path: out, format: 'A4', printBackground: true, displayHeaderFooter: true,
    margin: { top: '22mm', bottom: '20mm', left: '16mm', right: '16mm' },
    headerTemplate: `<div style="${style}"><span>${hl}</span><span>Confidential · internal use</span></div>`,
    footerTemplate: `<div style="${style}"><span>${fl}</span><span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>`,
  });
  await b.close();
})();
