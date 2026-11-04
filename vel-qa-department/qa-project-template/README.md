# qa-project — VEL Software Solutions QA project template

Skeleton repository for a client QA project, plus two **practice targets** used for training:

| Path | What it is |
|---|---|
| `docs/` | Strategy, plan, traceability, environment, release report, QA sign-off (fill from `templates/`) |
| `templates/` | Controlled templates: Markdown + CSV (test case, scenario, RTM, execution log, defect log, sign-off checklist) |
| `test-cases/` | `functional/`, `regression/`, `integration/`, `acceptance/` |
| `automation/` | `ui/`, `api/`, `fixtures/`, `page-objects/`, `reports/` (generated, git-ignored) |
| `performance/` | k6 and Locust scripts (local practice target only) |
| `accessibility/`, `test-data/`, `evidence/`, `defects/` | As described in the handbook |
| `practice-app/` | **VEL Practice Shop** — web app with deliberate defects (training only) |
| `practice-api/` | **VEL Practice API** — REST service with deliberate defects (training only) |
| `.github/workflows/qa.yml` | CI pipeline |

> **Never commit** passwords, tokens, customer data or production extracts. See the handbook, chapter *Professional QA Repository Structure*.

## Run the practice targets (local only)

```bash
python3 -m http.server 8000 --directory practice-app      # terminal 1 -> http://localhost:8000
python3 practice-api/server.py --port 8080                 # terminal 2 -> http://127.0.0.1:8080/api/health
```

The practice app accepts testing aids in the URL: `?build=1.0|1.1`, `?idle=10` (session timeout in seconds), `?lock=5` (lock duration in seconds).
The requirements under test are in `practice-app/SPEC.md`; the API contract is in `practice-api/README.md`.

## Run the starter automation

```bash
python3 -m venv .venv && source .venv/bin/activate
pip install -r automation/requirements.txt
playwright install chromium firefox
export QA_EMAIL=qa@example.test QA_PASSWORD='<see practice-api/README.md>'   # never commit real credentials
pytest -m smoke          # fast critical-path checks
pytest                   # everything
```

Optional: `CHROMIUM_EXECUTABLE=/path/to/chrome` uses an existing browser; `BUILD=1.1` targets the second app build; `BASE_URL`, `API_BASE_URL` override the targets.

## Conventions

IDs `REQ-nnn`, `TS-<area>-<nn>`, `TC-<area>-<nnn>`, `DEF-nnn`; evidence `<yyyy-mm-dd>_<TC-id>_<PASS|FAIL|BLOCKED>_<n>.<ext>`; branches `qa/<initials>/<topic>`;
commits `<type>(<scope>): <summary>`. Author is never the sole reviewer.
