# accessibility

Automated: axe-core scan (`automation/ui/test_accessibility.py`, set `AXE_PATH` after `npm install --save-dev axe-core`) and Lighthouse
(`npx lighthouse http://localhost:8000 --only-categories=accessibility --output=html --output-path=accessibility/lighthouse.html`).
Manual (mandatory): keyboard-only walk-through, 200 % zoom/reflow, contrast, screen reader (NVDA / VoiceOver / TalkBack).
Record findings with the WCAG success criterion and the version/level agreed in the test strategy (normally WCAG 2.1 or 2.2 AA).
