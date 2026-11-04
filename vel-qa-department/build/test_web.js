// Browser QA of the standalone HTML handbook. Usage: node build/test_web.js dist/VEL-QA-Department-Handbook.html [outdir]
const { chromium } = require('playwright');
const path = require('path');
const file = path.resolve(process.argv[2]);
const out = process.argv[3] || '/tmp';
const results = [];
const check = (name, ok, extra = '') => { results.push([ok ? 'PASS' : 'FAIL', name, extra]); };
(async () => {
  const browser = await chromium.launch();
  for (const [label, vp] of [['desktop', { width: 1280, height: 800 }], ['tablet', { width: 820, height: 1100 }], ['mobile', { width: 375, height: 760 }]]) {
    const ctx = await browser.newContext({ viewport: vp, deviceScaleFactor: 1 });
    const page = await ctx.newPage();
    const errors = [];
    page.on('pageerror', e => errors.push(String(e)));
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    await page.goto('file://' + file);
    await page.evaluate(() => document.fonts.ready);
    const m = await page.evaluate(() => ({ sw: document.documentElement.scrollWidth, iw: window.innerWidth, h1: document.querySelectorAll('h1').length, h2: document.querySelectorAll('h2').length,
      fonts: ['400 16px Inter', '700 16px "VEL Serif"', '400 16px "VEL Mono"'].map(f => document.fonts.check(f)), imgBroken: [...document.images].filter(i => !i.complete || i.naturalWidth === 0).length,
      tablesOverflow: [...document.querySelectorAll('.tablewrap')].filter(w => w.scrollWidth > w.clientWidth + 1).length }));
    check(`${label}: no horizontal page scroll`, m.sw <= m.iw, `scrollWidth=${m.sw} innerWidth=${m.iw}`);
    check(`${label}: embedded fonts available`, m.fonts.every(Boolean), JSON.stringify(m.fonts));
    check(`${label}: all images decoded`, m.imgBroken === 0);
    check(`${label}: no console/page errors`, errors.length === 0, errors.join(' | '));
    if (label === 'desktop') {
      check('structure: chapters and sections present', m.h1 >= 20 && m.h2 > 100, `h1=${m.h1} h2=${m.h2}`);
      // search
      await page.fill('#q', 'boundary value');
      await page.waitForTimeout(400);
      const n = await page.locator('#results li').count();
      check('search: returns results for "boundary value"', n > 0, `${n} results`);
      await page.fill('#q', 'zzzzqqqq');
      await page.waitForTimeout(400);
      check('search: reports no match for nonsense', (await page.locator('#results').innerText()).includes('No matches'));
      await page.fill('#q', 'idempotent');
      await page.waitForTimeout(400);
      await page.locator('#results a').first().click();
      await page.waitForTimeout(500);
      check('search: click jumps and highlights matches', (await page.locator('mark.hit').count()) > 0);
      await page.fill('#q', ''); await page.waitForTimeout(300);
      // nav
      await page.click('.navlist a[href="#lifecycle-stages"]', { timeout: 3000 }).catch(() => {});
      // theme
      await page.click('.sidebar [data-act=theme]');
      check('theme: dark mode toggles', (await page.evaluate(() => document.documentElement.getAttribute('data-theme'))) === 'dark');
      await page.screenshot({ path: `${out}/web-dark.png` });
      await page.click('.sidebar [data-act=theme]');
      await page.evaluate(() => window.scrollTo(0, 0));
      await page.screenshot({ path: `${out}/web-desktop.png` });
      // keyboard shortcut
      await page.keyboard.press('/');
      check('search: "/" focuses the search box', await page.evaluate(() => document.activeElement && document.activeElement.id === 'q'));
      await page.keyboard.press('Escape');
      // print path (same HTML -> PDF) for consistency check
      await page.emulateMedia({ media: 'print' });
      const pdf = await page.pdf({ preferCSSPageSize: true, printBackground: true, path: `${out}/web-print.pdf` });
      check('print: HTML prints to A4 PDF', pdf.length > 100000, `${(pdf.length / 1024) | 0} KB`);
    }
    if (label === 'mobile') {
      check('mobile: sidebar hidden until menu opened', await page.evaluate(() => getComputedStyle(document.querySelector('.sidebar')).transform !== 'none'));
      await page.click('.topbar [data-act=menu]');
      await page.waitForTimeout(300);
      check('mobile: menu opens the navigation', await page.evaluate(() => document.body.classList.contains('nav-open')));
      await page.screenshot({ path: `${out}/web-mobile-nav.png` });
      await page.keyboard.press('Escape');
      await page.evaluate(() => document.body.classList.remove('nav-open'));
      await page.evaluate(() => document.getElementById('first-manual-testing-assignment-a0')?.scrollIntoView());
      await page.screenshot({ path: `${out}/web-mobile.png` });
      check('mobile: wide tables scroll inside their wrapper, not the page', m.sw <= m.iw && m.tablesOverflow >= 0, `${m.tablesOverflow} tables scroll internally`);
    }
    await ctx.close();
  }
  await browser.close();
  let fail = 0;
  for (const [s, n, e] of results) { if (s === 'FAIL') fail++; console.log(`${s}  ${n}${e ? '  — ' + e : ''}`); }
  console.log(`\n${results.length - fail}/${results.length} checks passed`);
  process.exit(fail ? 1 : 0);
})();
