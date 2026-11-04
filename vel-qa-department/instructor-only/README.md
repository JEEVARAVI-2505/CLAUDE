# instructor-only — DO NOT give this folder to trainees

* `ANSWER-KEY.md` — list of the seeded defects in the VEL Practice Shop and Practice API, with severity guidance, how each is found and which build fixes it.
* `reference-solution/` — a complete automated suite (UI, API, accessibility, load) in which **every seeded defect is a strict `xfail`**. It doubles as an automated proof that each defect really exists in the practice targets.

Run the proof (start both practice targets first, see the template README):

```bash
cd instructor-only/reference-solution
python3 -m venv .venv && source .venv/bin/activate && pip install -r automation/requirements.txt && playwright install chromium
export QA_EMAIL=qa@example.test QA_PASSWORD='Passw0rd!QA'      # synthetic training credential of the local server
BUILD=1.0 pytest automation -q            # expect: all pass, every known defect reported as xfailed, no XPASS
BUILD=1.1 pytest automation -q            # expect: the regressions R1/R2 (+ defects fixed by accident) fail
```
Accessibility scan needs `npm install --save-dev axe-core` (or set `AXE_PATH`). Rotate the practice defects periodically so trainees cannot memorise answers.
