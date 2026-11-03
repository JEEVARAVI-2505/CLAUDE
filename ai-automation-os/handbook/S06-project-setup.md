# Stage S06 — Project Setup (Repository, Environments, Robots)

## Purpose
Create the place where the code lives, the rules that protect it, and the automatic checks that run on every change, **before** the first line of project code.

## Who and when
- **Owner:** Technical Lead / DevOps. **You:** assist and learn. **Approver:** Technical Lead.
- **Time:** 1–2 days.

## Before you start
G5 passed; Project ID; repository name decided (Part 2, section 2.3).

## Step by step

1. **Create a private repository** in the company GitHub organisation named `<short-name>`.
2. **Create the folder structure** from Part 2 (section 2.2). Empty folders need a `.gitkeep` file so Git keeps them:
   ```bash
   mkdir -p apps/api apps/admin-dashboard workflows/n8n src/{core,integrations,models,services,prompts,schemas,security,observability} \
            tests/{unit,integration,workflow,evaluation,security} data/sample docs infra scripts .github/workflows
   find . -type d -empty -exec touch {}/.gitkeep \;
   ```
3. **Create `.gitignore` first** (before any commit), including at least:
   ```
   .env
   .venv/
   __pycache__/
   *.pyc
   .pytest_cache/
   node_modules/
   *.log
   ```
4. **Create `.env.example`** listing every setting with a fake value:
   ```
   DATABASE_URL=postgresql://user:password@localhost:5432/appdb
   LLM_API_KEY=replace-me
   WEBHOOK_SECRET=replace-me
   ENVIRONMENT=local
   ```
   Real values live only in each person's untracked `.env` (local) or the secret manager (staging/production).
5. **Write `README.md`**: what the project does, how to install, how to run tests, how to run the app, who to ask. Test it by following it on a clean machine.
6. **Write `CONTRIBUTING.md`**: branch names, commit format, pull request rules, review checklist (Appendix C).
7. **Protect `main`** in GitHub settings: require pull request, require at least 1 approval, require status checks to pass, block force-push.
8. **Add the CI robot** `.github/workflows/ci.yml` that on every pull request: installs dependencies, runs the tests, runs a secret scan and a dependency vulnerability scan. A minimal example:
   ```yaml
   name: CI
   on: [pull_request]
   jobs:
     test:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-python@v5
           with: { python-version: "3.12" }
         - run: pip install -r requirements.txt
         - run: python -m pytest -q
   ```
   (Add secret and dependency scanning steps per the Security Specialist's instructions.)
9. **Create three environments:** local, staging, production, each with its own database and its own secrets. Never reuse a production key elsewhere.
10. **Create the ticket board** with columns Backlog → Ready → In Progress → In Review → Testing → Done and import the requirement tickets from S04.
11. **Add the team** to the repository with the right permissions: developers write, only the Technical Lead and DevOps administer.
12. **First commit** (README, .gitignore, structure) on `main` through the normal process, then tag `v0.0.1`.

## Worked example
The repository `gst-bill-automation` is created from this structure; the first pull request "GBA-1: project skeleton" adds the folders, `.gitignore`, `.env.example`, `README.md`, CI file and is approved by the Technical Lead.

## Deliverables
- The repository with protected `main`, CI passing on an empty test.
- `README.md`, `CONTRIBUTING.md`, `.gitignore`, `.env.example`.
- Board with all tickets; environment list in `docs/deployment/environments.md`.

## Done checklist
- [ ] Repository is private; `main` is protected.
- [ ] `.gitignore` committed before any code.
- [ ] No secret in the repository (run the secret scan).
- [ ] CI runs on pull requests and is green.
- [ ] A new person can follow the README and run the tests.
- [ ] Environments and their secrets are separate.

## Common mistakes
- Committing `.env` "just once".
- Forgetting branch protection, then someone pushes straight to `main`.
- README that only the author can follow.

## Gate sign-off
Technical Lead confirms the checklist. (No numbered gate; required before S07.)

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
