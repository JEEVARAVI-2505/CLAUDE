const { chromium } = require('playwright'); const path = require('path');
(async () => {
  const b = await chromium.launch(); const f = 'file://' + path.resolve('output/VEL-Software-Architecture-Handbook.html'); const out = {};
  for (const [name, vp] of [['desktop', { width: 1366, height: 800 }], ['mobile', { width: 390, height: 800 }]]) {
    const p = await b.newPage({ viewport: vp }); const errs = [];
    p.on('pageerror', e => errs.push(String(e))); p.on('console', m => m.type() === 'error' && !/fonts\.googleapis|ERR_|net::/.test(m.text()) && errs.push(m.text()));
    await p.goto(f, { waitUntil: 'load' });
    const r = await p.evaluate(() => {
      const ids = new Set([...document.querySelectorAll('[id]')].map(e => e.id));
      const bad = [...document.querySelectorAll('a[href^="#"]')].map(a => a.getAttribute('href').slice(1)).filter(h => h && !ids.has(h));
      const ext = [...document.querySelectorAll('a[href^="http"]')].map(a => a.href);
      const imgsBroken = [...document.images].filter(i => !i.complete || !i.naturalWidth).length;
      return { chapters: document.querySelectorAll('section.chapter').length, svgs: document.querySelectorAll('svg.svgd').length, badAnchors: bad, extLinks: ext.length, uniqExt: new Set(ext).size, nonHttps: ext.filter(u => !u.startsWith('https://')), imgsBroken,
        overflowX: document.documentElement.scrollWidth - innerWidth, tables: document.querySelectorAll('table').length };
    });
    await p.fill('#q', 'trust boundary'); await p.waitForTimeout(400);
    r.searchHits = await p.evaluate(() => document.querySelectorAll('#res a').length);
    await p.screenshot({ path: `/tmp/lc/web-${name}.png` });
    if (name === 'mobile') { await p.fill('#q', ''); await p.click('#menu'); await p.waitForTimeout(300); await p.screenshot({ path: '/tmp/lc/web-mobile-menu.png' }); }
    r.errors = errs; out[name] = r; await p.close();
  }
  console.log(JSON.stringify(out, null, 1)); await b.close();
})();
