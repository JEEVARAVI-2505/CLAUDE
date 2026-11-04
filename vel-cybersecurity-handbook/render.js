// Render handbook HTML -> PDF with Chromium (Playwright). Usage: node render.js in.html out.pdf "DOC-ID vX"
const { chromium } = require('playwright');
const path = require('path');
(async () => {
  const [, , inHtml, outPdf, docId] = process.argv;
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('file://' + path.resolve(inHtml), { waitUntil: 'load' });
  await page.emulateMedia({ media: 'print' });
  await page.evaluate(() => document.fonts.ready);
  const base = 'font-family:Inter,Arial,sans-serif;font-size:7pt;color:#566074;width:100%;padding:0 16mm;';
  await page.pdf({
    path: outPdf,
    format: 'A4',
    printBackground: true,
    displayHeaderFooter: true,
    margin: { top: '20mm', bottom: '18mm', left: '16mm', right: '16mm' },
    headerTemplate: `<div style="${base}display:flex;justify-content:space-between;"><span>VEL Software Solutions &middot; Cybersecurity Department Handbook (${docId})</span><span>Confidential &middot; internal use &middot; Authorised defensive security only</span></div>`,
    footerTemplate: `<div style="${base}display:flex;justify-content:space-between;"><span>Developed by V. LOKESHWARAN &middot; velsoftwaresolutions.in@gmail.com</span><span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>`,
  });
  await browser.close();
})().catch((e) => { console.error(e); process.exit(1); });
