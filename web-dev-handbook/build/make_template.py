#!/usr/bin/env python3
"""Generate project-template/ - the starter repository structure from handbook Section 6."""
from pathlib import Path
R = Path(__file__).resolve().parent.parent / "project-template"
PURPOSE = {
 "docs": "Requirements, architecture, database, API, deployment, security, user guide, ADRs.",
 "docs/adr": "One short Architecture Decision Record per decision (never edited after acceptance).",
 "frontend/public": "Static files served as-is. No secrets.", "frontend/src/components": "Reusable, typed, accessible UI components.",
 "frontend/src/pages": "Routes/screens (or app/ in Next.js App Router). Keep thin.", "frontend/src/layouts": "Page shells (header, footer, dashboard frame).",
 "frontend/src/hooks": "Reusable stateful logic.", "frontend/src/services": "API client - the only place that knows URLs.",
 "frontend/src/styles": "Design tokens and global CSS mirrored from Figma.", "frontend/src/assets": "Optimised images and fonts imported by code.",
 "frontend/tests": "Component and E2E tests (Playwright).", "backend/src/routes": "HTTP endpoints: parse, validate, call a service, respond. No business logic.",
 "backend/src/controllers": "Optional adapters between routes and services.", "backend/src/services": "Business logic - pure, tested functions.",
 "backend/src/models": "Database models and request/response schemas.", "backend/src/middleware": "Auth, request id, rate limit, error handling.",
 "backend/src/config": "Settings from environment variables; fail fast if missing.", "backend/tests": "Unit and integration tests (throw-away database).",
 "database/migrations": "Versioned schema changes. Never edit an applied migration.", "database/seeds": "Reference/demo data. No real personal data.",
 "deployment/docker": "Dockerfiles, nginx config, compose overrides.", "deployment/scripts": "Release, backup, restore, smoke-test scripts (idempotent, documented).",
 "qa": "Test plan, UAT script, reports, restore-test logs (evidence for acceptance).",
}
for d, t in PURPOSE.items():
    p = R / d; p.mkdir(parents=True, exist_ok=True)
    (p / "README.md").write_text(f"# {d}\n\n{t}\n\nHandbook reference: Section 6 - Project Structure and Repository Standards.\n")
W = lambda p, t: ((R / p).parent.mkdir(parents=True, exist_ok=True), (R / p).write_text(t))
W("README.md", "# Project name\n\nOne-paragraph description.\n\n## Getting started (must work in 30 minutes)\n```bash\ncp .env.example .env\ndocker compose up -d\n```\n\n## Test\n```bash\n# frontend: npm test   |   backend: pytest -q\n```\n\n## Documentation\nSee `docs/`. Contribution rules: `CONTRIBUTING.md`.\n")
W("CHANGELOG.md", "# Changelog\nFormat: Keep a Changelog; versions: Semantic Versioning.\n\n## [Unreleased]\n### Added\n### Changed\n### Fixed\n### Security\n")
W("CONTRIBUTING.md", "# Contributing\n- Branches: `feat/<id>-<name>`, `fix/<id>-<name>`, `hotfix/<id>-<name>`; never commit to `main` directly.\n- Commits: Conventional Commits (`feat(scope): summary`).\n- PRs: small, template completed, CI green, reviewed by someone else, squash-merge.\n- Never commit secrets or customer data. If it happens: rotate first, then tell the Technical Lead.\n- Definition of done: see handbook Section 6.\n")
W(".gitignore", "node_modules/\ndist/\n.next/\n__pycache__/\n.venv/\n.env\n.env.*\n!.env.example\n*.pem\n*.key\n*.sqlite3\n*.dump\nterraform.tfstate*\n.DS_Store\n.idea/\nplaywright-report/\ncoverage/\n")
W(".env.example", "APP_ENV=local\nDATABASE_URL=postgresql://app_user:change-me-dev@localhost:5432/app_dev\nJWT_SECRET=generate-a-long-random-value-per-environment\nSMTP_HOST=localhost\nSMTP_PORT=1025\nPAYMENT_KEY_ID=test_key_id_here\nPAYMENT_KEY_SECRET=test_secret_here\nALLOWED_ORIGINS=http://localhost:3000\n")
W("compose.yaml", "services:\n  db:\n    image: postgres:16\n    environment:\n      POSTGRES_PASSWORD: devonly\n      POSTGRES_DB: app_dev\n    ports: ['127.0.0.1:5432:5432']\n    volumes: [dbdata:/var/lib/postgresql/data]\n  mailpit:\n    image: axllent/mailpit\n    ports: ['127.0.0.1:8025:8025', '127.0.0.1:1025:1025']\nvolumes:\n  dbdata:\n")
for f in ["requirements", "architecture", "database-design", "api-specification", "deployment", "security", "user-guide"]:
    W(f"docs/{f}.md", f"# {f.replace('-', ' ').title()}\n\n_Template - complete per handbook Section 5 and Appendix A4._\n")
W(".github/pull_request_template.md", "## What and why\n\nCloses #\n\n## How tested\n- [ ] Unit tests  - [ ] Manual test in staging (screenshot)\n\n## Checklist\n- [ ] No secrets  - [ ] Migrations reviewed  - [ ] Docs/CHANGELOG updated  - [ ] Accessibility checked\n")
W(".github/CODEOWNERS", "# Replace with real GitHub handles\n* @techlead\n/deployment/ @devops\n/.github/ @techlead @devops\n/database/migrations/ @techlead\n")
W(".github/workflows/ci.yml", "name: CI\non:\n  pull_request:\n  push: { branches: [main] }\npermissions: { contents: read }\njobs:\n  frontend:\n    runs-on: ubuntu-latest\n    defaults: { run: { working-directory: frontend } }\n    steps:\n      - uses: actions/checkout@v4\n      - uses: actions/setup-node@v4\n        with: { node-version: 22, cache: npm, cache-dependency-path: frontend/package-lock.json }\n      - run: npm ci\n      - run: npm run lint\n      - run: npx tsc --noEmit\n      - run: npm test -- --run\n      - run: npm run build\n")
print(sum(1 for _ in R.rglob("*") if _.is_file()), "files")
