// Render print HTML to PDF with Chromium (same engine as the VEL reference playbook).
// usage: node render.js in.html out.pdf cfg.json "Document title"
const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const [, , input, output, cfgPath, title] = process.argv;
  const cfg = JSON.parse(fs.readFileSync(cfgPath, 'utf8'));
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('file://' + require('path').resolve(input), { waitUntil: 'load' });
  await page.evaluate(async () => {
    document.querySelectorAll('details').forEach(d => (d.open = true));
    await document.fonts.ready;
  });
  await page.emulateMedia({ media: 'print' });
  await page.pdf({
    path: output,
    format: 'A4',
    printBackground: true,
    displayHeaderFooter: true,
    headerTemplate: cfg.header,
    footerTemplate: cfg.footer,
    preferCSSPageSize: true,
    outline: true,
    tagged: true,
  });
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });
