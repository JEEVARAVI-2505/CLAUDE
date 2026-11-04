// usage: node render.js jobs.json  — jobs: [{html, pdf, left, right, footL}]
const { chromium } = require('playwright');
const fs = require('fs'), path = require('path');
(async () => {
  const jobs = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
  const browser = await chromium.launch();
  for (const j of jobs) {
    const page = await browser.newPage();
    await page.goto('file://' + path.resolve(j.html), { waitUntil: 'load' });
    await page.emulateMedia({ media: 'print' });
    const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
    const st = 'font-family:Inter,Arial,sans-serif;font-size:7.6px;color:#5a6480;width:100%;padding:0 15mm;display:flex;justify-content:space-between;';
    await page.pdf({
      path: j.pdf, format: 'A4', printBackground: true, displayHeaderFooter: true, preferCSSPageSize: true,
      outline: true, tagged: true,
      headerTemplate: `<div style="${st}margin-top:9mm"><span>${esc(j.left)}</span><span>${esc(j.right)}</span></div>`,
      footerTemplate: `<div style="${st}margin-bottom:7mm"><span>${esc(j.footL)}</span><span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>`,
    });
    await page.close();
    console.log('pdf', j.pdf);
  }
  await browser.close();
})();
