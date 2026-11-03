const { chromium } = require('playwright');
(async () => {
  const [src, out] = process.argv.slice(2);
  const b = await chromium.launch(); const p = await b.newPage();
  await p.goto('file://' + src); await p.emulateMedia({ media: 'print' });
  await p.pdf({ path: out, format: 'A4', printBackground: true, displayHeaderFooter: true,
    margin: { top: '16mm', bottom: '16mm', left: '13mm', right: '13mm' },
    headerTemplate: '<div style="font-size:7px;width:100%;padding:0 13mm;color:#667">VEL Software Solutions · SaaS Company Operating System v1.1</div>',
    footerTemplate: '<div style="font-size:7px;width:100%;padding:0 13mm;color:#667;display:flex;justify-content:space-between"><span>Developed by V.LOKESHWARAN · velsoftwaresolutions.in@gmail.com · verify all ASSUMPTIONS before use</span><span>Page <span class="pageNumber"></span> / <span class="totalPages"></span></span></div>' });
  await b.close();
})();
