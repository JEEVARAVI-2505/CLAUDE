// Build the playbook: concatenates parts/*.html, embeds the VEL logo, renders the PDF
// with header/footer and a repeating logo watermark.  Usage: node build.js
const fs = require('fs'), path = require('path');
const { chromium } = require('playwright');
const here = __dirname;
const uri = p => 'data:image/jpeg;base64,' + fs.readFileSync(path.join(here, p)).toString('base64');
(async () => {
  const parts = fs.readdirSync(path.join(here, 'parts')).filter(f => f.endsWith('.html')).sort();
  let html = parts.map(f => fs.readFileSync(path.join(here, 'parts', f), 'utf8')).join('');
  html = html.split('{{LOGO}}').join(uri('assets/vel-logo.jpg')).split('{{WM}}').join(uri('assets/vel-watermark.jpg'));
  const outHtml = path.join(here, 'VEL-Cloud-DevOps-Playbook.html');
  fs.writeFileSync(outHtml, html);
  const S = 'style="font:7pt Helvetica,Arial,sans-serif;color:#5a6573;width:100%;padding:0 15mm;display:flex;justify-content:space-between"';
  const header = `<div ${S}><span>VEL Software Solutions &middot; Cloud &amp; DevOps Playbook (VEL-CD-001 v1.0)</span><span>Confidential &middot; internal use</span></div>`;
  const footer = `<div ${S}><span>Developed by V. LOKESHWARAN &middot; velsoftwaresolutions.in@gmail.com</span><span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>`;
  const b = await chromium.launch(); const pg = await b.newPage();
  await pg.goto('file://' + outHtml); await pg.waitForTimeout(500);
  await pg.pdf({ path: path.join(here, 'VEL-Cloud-DevOps-Playbook.pdf'), format: 'A4', printBackground: true,
    displayHeaderFooter: true, headerTemplate: header, footerTemplate: footer,
    margin: { top: '20mm', bottom: '18mm', left: '15mm', right: '15mm' } });
  await b.close(); console.log('built');
})();
