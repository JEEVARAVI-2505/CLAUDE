# Part 2 — The Folder Structure, Explained

A tidy folder structure is how a team finds things without asking. Every project at VEL uses the **same** structure, so you can open any project and know where everything is. There are two levels: the **company drive** (documents, contracts, research) and the **project repository** (code).

## 2.1 Level 1 — Company drive (documents)

```
VEL-Company/
├── 01-Management/            policies, budgets, hiring, templates owned by the Founder
├── 02-Sales-and-Customers/
│   ├── Leads/                one file per prospect
│   └── Customers/
│       └── <Customer-Name>/
│           ├── contracts/    SOW, agreement, DPA (signed copies only)
│           ├── meetings/     notes, dated YYYY-MM-DD
│           └── onboarding/
├── 03-Research/              market reports, competitor notes, interview recordings
├── 04-Projects/
│   └── P-2026-001_GST-Bill-Automation/     one folder per project, named with Project ID
│       ├── 00-admin/         project.yaml, status reports, risk register, change requests, gate sign-offs
│       ├── 01-discovery/     interviews, opportunity score
│       ├── 02-analysis/      process sheet, AS-IS, TO-BE, bottleneck analysis
│       ├── 03-requirements/  scope freeze, input/output spec, acceptance criteria
│       ├── 04-design/        architecture, data model, UI mockups, agent spec
│       ├── 05-testing/       test plan, evaluation set descriptions, reports
│       ├── 06-security/      checklist, test evidence
│       ├── 07-commercial/    cost sheet, price sheet, proposal, ROI report
│       ├── 08-deployment/    readiness checklist, runbook, release notes
│       └── 09-handover/      manuals, training material, acceptance sign-off
├── 05-Templates/             blank forms (copy, never edit the original)
├── 06-Finance/               invoices, expenses (Founder and accounts only)
└── 07-Training/              this handbook, videos, onboarding material
```

**Rules**

- One project, one folder, named `P-<year>-<number>_<Short-Name>`.
- Copy a template; never edit the master.
- Files are named `YYYY-MM-DD_description_vN.ext`, e.g. `2026-11-03_process-sheet_v2.docx`.
- Signed or final documents are **read-only**; changes create a new version.
- Customer data files are **never** stored here unless the contract allows it and the folder is access-restricted.

## 2.2 Level 2 — Project repository (code)

This is the layout every code repository follows.

```
gst-bill-automation/                  the repository (one per project)
├── README.md                         what this is, how to run it (first thing anyone reads)
├── CONTRIBUTING.md                   how to work on it: branches, commits, reviews
├── .gitignore                        files Git must never save (secrets, caches)
├── .env.example                      list of required settings with FAKE values
├── apps/
│   ├── api/                          the web service that other systems call
│   └── admin-dashboard/              the screen staff use (review queue, reports)
├── workflows/
│   ├── n8n/                          exported workflow files (JSON), versioned
│   └── automation-definitions/       written description of each workflow
├── agents/                           only if an AI agent is truly needed
├── src/
│   ├── core/                         business rules and domain logic (pure, easy to test)
│   ├── integrations/                 code that talks to WhatsApp, email, Tally, etc.
│   ├── models/                       data structures and database tables
│   ├── services/                     coordinates the above into use cases
│   ├── tools/                        helper utilities
│   ├── prompts/                      AI instructions, versioned like code
│   ├── schemas/                      JSON schemas for inputs and outputs
│   ├── security/                     authentication, permission, secret handling
│   └── observability/                logging, metrics, alerts
├── data/
│   └── sample/                       FAKE sample data only
├── tests/
│   ├── unit/                         tests of single functions
│   ├── integration/                  tests of parts working together
│   ├── workflow/                     end-to-end workflow tests
│   ├── evaluation/                   AI accuracy tests with labelled data
│   └── security/                     attack-style tests
├── docs/
│   ├── requirements/  architecture/  api/  deployment/  testing/  operations/
├── infra/                            server and deployment configuration (Docker, scripts)
├── scripts/                          helper commands (setup, backup, seed fake data)
└── .github/
    └── workflows/                    the automatic test-and-check robots (CI)
```

### What each part is for, in plain words

| Folder / file | Think of it as | Beginner tip |
|---|---|---|
| `README.md` | The front door | If a stranger cannot run the project from it, fix it |
| `.gitignore` | A "do not photograph" list | Add `.env` and `__pycache__/` before your first commit |
| `.env.example` | A blank form of settings | Copy to `.env`, fill with your own test values |
| `src/core/` | The brain: rules like "GSTIN must have a valid check character" | No internet, no database here; this keeps it testable |
| `src/integrations/` | The hands that reach other systems | One file per provider, so a provider change touches one place |
| `src/prompts/` | The AI's instructions | Treat like code: version, review, test |
| `src/schemas/` | The agreed data shapes | Every AI answer is checked against these |
| `tests/` | The safety net | Same folder shape as `src/` so tests are easy to find |
| `docs/` | The project's memory | Update it in the same pull request as the change |
| `infra/` | How to build and run it on servers | Only DevOps changes this without review |
| `.github/workflows/` | The robots | They run tests on every pull request |

### The small working example

`reference-project/` in this repository is a **small** real project with the same ideas:

```
reference-project/
├── app/
│   ├── rules.py       GSTIN check, tax-maths and date rules (the "core")
│   ├── extraction.py  the AI step wrapped in safety: validate, retry once, human fallback
│   └── main.py        FastAPI service that exposes the rules
├── tests/
│   ├── test_rules.py  tests of the rules with hand-calculated numbers
│   ├── test_extraction.py  tests of the AI step using a fake model (incl. prompt injection)
│   └── test_api.py    tests of the web service
├── requirements.txt   the libraries it needs
└── pytest.ini         test settings
```

Open each file while reading Stage 7.

## 2.3 Naming conventions

| Thing | Rule | Example |
|---|---|---|
| Project ID | `P-<year>-<3 digits>` | `P-2026-001` |
| Repository | lowercase, hyphens | `gst-bill-automation` |
| Branch | `type/ticket-short-name` | `feature/GBA-12-gstin-check` |
| Commit message | `<Ticket>: <what changed>` | `GBA-12: add GSTIN check-character validation` |
| Python files | lowercase, underscores | `rules.py` |
| Python classes | CapWords | `BillValidator` |
| Python functions/variables | lowercase, underscores | `validate_bill` |
| Constants | UPPER_CASE | `MAX_INVOICE_LENGTH` |
| Database tables | lowercase plural | `bills`, `review_tasks` |
| Documents | `YYYY-MM-DD_name_vN` | `2026-11-03_scope-freeze_v1.docx` |
| Release tags | semantic version | `v1.2.0` |

Branch types: `feature/` (new capability), `fix/` (bug), `docs/` (documents only), `chore/` (housekeeping), `hotfix/` (urgent production fix).

## 2.4 What must never be in the repository

- Passwords, API keys, tokens, certificates, `.env` files.
- Real customer data, real invoices, real GSTINs of customers.
- Large generated files, virtual environments, `node_modules`.
- Personal notes and screenshots with customer information.

If you commit one by mistake: **stop, tell the Technical Lead immediately.** The secret must be revoked and replaced even if you delete the file, because Git remembers history.

## 2.5 The three Git branches you will meet

| Branch | Meaning | Who can change it |
|---|---|---|
| `main` | Always working, always deployable | Only via reviewed pull request |
| `develop` (if used) | Integration area before release | Via pull request |
| your `feature/...` branch | Your own workspace | You |

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
