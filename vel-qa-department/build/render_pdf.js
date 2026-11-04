// Render an HTML file to A4 PDF with Chromium (Playwright). Usage: node render_pdf.js in.html out.pdf
const { chromium } = require('playwright');
(async () => {
  const [, , src, dst] = process.argv;
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('file://' + require('path').resolve(src), { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.pdf({ path: dst, preferCSSPageSize: true, printBackground: true, tagged: true, outline: true });
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });
