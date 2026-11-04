Every project repository starts from the **company template** (also provided in this package as `project-template/`). The structure below is a real example for a **Next.js + FastAPI + PostgreSQL** project; adapt folder names to the chosen stack, but keep the same responsibilities. **Do not create empty folders for appearance** - every folder must have a purpose you can explain.

## Standard project structure

```
web-development-project/
├── README.md
├── CHANGELOG.md
├── CONTRIBUTING.md
├── .gitignore
├── .env.example
├── compose.yaml
├── docs/
│   ├── requirements.md
│   ├── architecture.md
│   ├── database-design.md
│   ├── api-specification.md
│   ├── deployment.md
│   ├── security.md
│   ├── user-guide.md
│   └── adr/                 (Architecture Decision Records)
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── components/
│   │   ├── pages/  (or app/ in Next.js App Router)
│   │   ├── layouts/
│   │   ├── hooks/
│   │   ├── services/
│   │   ├── styles/
│   │   └── assets/
│   └── tests/
├── backend/
│   ├── src/
│   │   ├── routes/
│   │   ├── controllers/
│   │   ├── services/
│   │   ├── models/
│   │   ├── middleware/
│   │   └── config/
│   └── tests/
├── database/
│   ├── migrations/
│   └── seeds/
├── deployment/
│   ├── docker/
│   └── scripts/
├── .github/
│   ├── workflows/
│   ├── CODEOWNERS
│   └── pull_request_template.md
└── qa/
```

## What every folder and key file is for

| Path | Purpose | Rules |
|---|---|---|
| `README.md` | Front door: what the project is, prerequisites, how to run, test, deploy, who to ask | A new joiner must run the app in 30 minutes using only this file |
| `CHANGELOG.md` | Human-readable history of releases (Keep a Changelog format, SemVer versions) | Updated in every release PR |
| `CONTRIBUTING.md` | Branching, commit, PR and review rules; definition of done | Mirrors this handbook |
| `.gitignore` | Files Git must never track - dependencies, build output, `.env`, keys, IDE files, OS files | Reviewed at repository creation |
| `.env.example` | Names and **fake/safe sample values** of every environment variable | Real values live only in the secret manager; update when a variable is added |
| `compose.yaml` | Local development stack - app, database, mail catcher | No production secrets |
| `docs/requirements.md` | Discovery notes, user stories (REQ-ids), acceptance criteria, input/output register | Source of truth for scope |
| `docs/architecture.md` | Diagrams, stack, environments, integrations, cost estimate | Updated with every architectural change |
| `docs/database-design.md` | ER diagram, data dictionary, indexes, retention, backup policy | Matches migrations |
| `docs/api-specification.md` | Endpoints, auth, request/response, error catalogue | Generated from OpenAPI where possible |
| `docs/deployment.md` | Environments, CI/CD, release, rollback, DNS/TLS, monitoring, runbooks | Used by DevOps and the client |
| `docs/security.md` | Threat model, controls, access matrix, scan results, incident contacts | Reviewed before every release |
| `docs/user-guide.md` | How the client's staff operate the system | Written in plain language with screenshots |
| `docs/adr/` | One short file per architecture decision | Never edited after acceptance - superseded instead |
| `frontend/public/` | Static files served as-is (favicon, robots.txt, images that need stable URLs) | No secrets, no private data |
| `frontend/src/components/` | Reusable UI building blocks (Button, Card, Form fields) | Small, typed, accessible, tested |
| `frontend/src/pages/` (or `app/`) | Routes/screens | One folder per route; thin - compose components |
| `frontend/src/layouts/` | Page shells (header, footer, dashboard frame) | |
| `frontend/src/hooks/` | Reusable stateful logic (`useDebounce`, `useAuth`) | |
| `frontend/src/services/` | API client and external calls | The only place that knows URLs |
| `frontend/src/styles/` | Design tokens, global CSS | Tokens mirror the Figma design system |
| `frontend/src/assets/` | Images/fonts imported by code | Optimised before commit |
| `frontend/tests/` | Component and E2E tests | Run in CI |
| `backend/src/routes/` | HTTP endpoints - parse, validate, call a service, return a response | No business logic |
| `backend/src/controllers/` | Optional adapter between routes and services (framework dependent) | |
| `backend/src/services/` | **Business logic** - pure, tested functions | Where rules live |
| `backend/src/models/` | Database models and schemas (ORM/Pydantic) | Match the migrations |
| `backend/src/middleware/` | Cross-cutting concerns - auth, request id, rate limit, error handling | |
| `backend/src/config/` | Settings loaded from environment variables | Fails fast when a variable is missing |
| `backend/tests/` | Unit and integration tests | Use a throw-away database |
| `database/migrations/` | Versioned schema changes | Never edit an applied migration |
| `database/seeds/` | Reference/demo data | No real personal data |
| `deployment/docker/` | Dockerfiles, nginx config, compose overrides | |
| `deployment/scripts/` | Release, backup, restore, smoke-test scripts | Documented and idempotent |
| `.github/workflows/` | CI/CD pipelines | Reviewed like production code |
| `.github/CODEOWNERS` | Auto-requests reviewers for sensitive paths | |
| `qa/` | Test plan, test cases, UAT script, reports, restore-test logs | Evidence for acceptance |

## Naming conventions

| Item | Convention | Example |
|---|---|---|
| Repositories | `client-project` lowercase, hyphens | `sunrise-dental-portal` |
| Branches | `type/issue-short-description` | `feat/42-booking-form` |
| Folders and files (frontend) | Components `PascalCase.tsx`; others `kebab-case` | `ProductCard.tsx`, `use-auth.ts` |
| Python files / functions | `snake_case` | `booking_service.py`, `create_booking()` |
| Python classes / TypeScript types | `PascalCase` | `BookingIn`, `OrderStatus` |
| Constants and env variables | `UPPER_SNAKE_CASE` | `DATABASE_URL` |
| Database tables / columns | `snake_case`, plural tables, singular columns | `bookings.starts_at` |
| API paths | Lowercase plural nouns, versioned | `/api/v1/bookings` |
| CSS classes | Short, descriptive; BEM or utility classes consistently | `.card__title` |
| Git tags | `vMAJOR.MINOR.PATCH` | `v1.4.0` |

## Environment separation

| Environment | Purpose | Data | Access | Deploy |
|---|---|---|---|---|
| **Local** | Daily development | Synthetic seeds | The developer | Manual (`docker compose up`) |
| **Development (shared)** | Integration of merged work | Synthetic | Team | Auto from `develop` |
| **Staging** | Production-like verification, UAT | Synthetic or anonymised | Team + client reviewers | Auto from `main`/release candidate |
| **Production** | Real users | Real | Minimum named people | Pipeline with manual approval |

Rules: separate cloud accounts or projects per environment where possible; separate databases, secrets, API keys and payment keys (test keys outside production); staging is protected and blocked from search engines; **production data is never copied to development**.

```
# .env.example  (committed - names and SAFE sample values only)
APP_ENV=local
DATABASE_URL=postgresql://app_user:change-me-dev@localhost:5432/app_dev
JWT_SECRET=generate-a-long-random-value-per-environment
SMTP_HOST=localhost
SMTP_PORT=1025
PAYMENT_KEY_ID=test_key_id_here
PAYMENT_KEY_SECRET=test_secret_here
ALLOWED_ORIGINS=http://localhost:3000
```

!!! danger "NEVER COMMIT"
    Credentials, API keys, private keys (`*.pem`, `id_ed25519`), production `.env` files, database dumps, customer data, screenshots showing tokens, `terraform.tfstate` with secrets. If a secret is committed: **(1)** revoke and rotate it immediately, **(2)** tell the Technical Lead, **(3)** only then clean the history. Deleting the file in a new commit does **not** remove it from history.

```
# .gitignore (essentials)
node_modules/
dist/
.next/
__pycache__/
.venv/
.env
.env.*
!.env.example
*.pem
*.key
*.sqlite3
*.dump
terraform.tfstate*
.DS_Store
.idea/
playwright-report/
coverage/
```

## Branching strategy

| Branch | Purpose | Rules |
|---|---|---|
| `main` | Always releasable; tagged for releases | Protected - PR + 1 approval + passing checks; no force push |
| `develop` | Integration of finished features (for multi-developer projects) | Protected |
| `feat/<id>-<name>` | One feature or fix | Short-lived (days, not weeks); branch from `develop` |
| `fix/<id>-<name>` | Bug fix | Same as above |
| `hotfix/<id>-<name>` | Urgent production fix | Branch from `main`; merge to `main` **and** `develop`; tag a patch release |
| `release/x.y.z` | Optional stabilisation branch for large releases | |

Small projects may use trunk-based flow (`main` + short-lived feature branches). The Technical Lead chooses at Stage 12 and records it in `CONTRIBUTING.md`.

## Commit standards (Conventional Commits)

Format: `type(scope): short imperative summary` - maximum about 72 characters; body explains **why**.

| Type | Use |
|---|---|
| `feat` | New user-visible capability |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `style` | Formatting, no logic change |
| `refactor` | Code change that is neither fix nor feature |
| `test` | Add or change tests |
| `perf` | Performance improvement |
| `build` / `ci` | Build system or pipeline |
| `chore` | Maintenance |
| `security` | Security fix (use a private channel for the details of unpatched vulnerabilities) |

```
feat(booking): validate slot availability on the server

Prevents double booking when two users submit at the same time.
Adds a unique constraint on (slot_id) and a regression test.
Closes #42
```

## Pull-request procedure

1. Branch from the latest `develop`/`main`; keep the change small (about 400 lines or fewer).
2. Run locally - format, lint, type-check, tests. Fix everything before pushing.
3. Open the PR using the template - **what and why, how tested, screenshots, checklist**; link the issue (`Closes #42`).
4. CI must be green. Request a reviewer who is **not** the author.
5. Reviewer applies the checklist (L5.4): correctness, tests, security, accessibility, performance, docs, no secrets.
6. Author answers every comment; reviewer approves; **squash-merge** with a conventional message.
7. Delete the branch. The merge to `main` triggers the pipeline to staging.

## Definition of done

- [ ] Meets every acceptance criterion in the requirement (REQ-id)
- [ ] Unit/integration tests added and passing; critical journey covered by E2E where relevant
- [ ] Lint and type checks pass; no new warnings
- [ ] Reviewed and approved by another person
- [ ] Accessibility and responsive checks done
- [ ] No secrets, no personal data in code or logs
- [ ] Documentation and `CHANGELOG.md` updated
- [ ] Deployed to staging and smoke-tested

## Versioning and releases

Use **Semantic Versioning**: `MAJOR.MINOR.PATCH` - MAJOR for breaking changes, MINOR for new backward-compatible features, PATCH for fixes. Release steps: update `CHANGELOG.md`, tag `vX.Y.Z`, build **once**, deploy the same artefact to staging and then production, and record the deployment evidence.
