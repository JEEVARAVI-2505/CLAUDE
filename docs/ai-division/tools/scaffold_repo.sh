#!/usr/bin/env bash
# Creates the standard `ai-product/` repository skeleton (Stage 7).
# Usage: bash scaffold_repo.sh [target_dir]   (default: ./ai-product)
set -euo pipefail
ROOT="${1:-ai-product}"

dirs=(
  apps/web apps/api
  src/agents src/orchestration src/models src/prompts src/tools src/rag src/retrieval
  src/embeddings src/memory src/schemas src/services src/security src/evaluation src/observability
  data/sample data/evaluation
  tests/unit tests/integration tests/agent tests/rag tests/security
  docs/requirements docs/architecture docs/deployment docs/evaluation docs/adr
  infra .github/workflows
)
for d in "${dirs[@]}"; do mkdir -p "$ROOT/$d"; touch "$ROOT/$d/.gitkeep"; done

cat > "$ROOT/.gitignore" <<'EOF'
.env
.env.*
!.env.example
*.pem
*.key
data/private/
*.sqlite
__pycache__/
.venv/
node_modules/
uploads/
.mypy_cache/
.ruff_cache/
.pytest_cache/
EOF

cat > "$ROOT/.env.example" <<'EOF'
# Copy to .env (never commit .env). Real keys live in the secret manager.
APP_ENV=local
DATABASE_URL=postgresql://app:app@localhost:5432/aiproduct
REDIS_URL=redis://localhost:6379/0
LLM_PROVIDER_API_KEY=replace-me-dev-key-with-spend-cap
MONTHLY_TOKEN_BUDGET_USD=20
EOF

cat > "$ROOT/.github/PULL_REQUEST_TEMPLATE.md" <<'EOF'
## What & why (ticket / requirement ID)
## How tested (tests added, eval report attached)
## AI impact (prompt / model / retrieval changed? metrics before -> after)
## Security & privacy (secrets? personal data? new tools or permissions?)
- [ ] Tests pass  - [ ] Docs updated  - [ ] No secrets  - [ ] Reviewer assigned
EOF

cat > "$ROOT/.github/CODEOWNERS" <<'EOF'
/src/security/   @your-org/security
/src/prompts/    @your-org/ai-leads
/infra/          @your-org/devops
EOF

cat > "$ROOT/Makefile" <<'EOF'
.PHONY: setup test lint
setup: ; python -m venv .venv && . .venv/bin/activate && pip install -e ".[dev]"
test:  ; pytest -q
lint:  ; ruff check . && mypy --strict src
EOF

printf '# ai-product\n\nSee docs/ for requirements, architecture and runbooks.\n' > "$ROOT/README.md"
printf '# Third-party packages\n\n| Package | Version | Licence | Purpose | Approved by/date |\n|---|---|---|---|---|\n' > "$ROOT/THIRD_PARTY.md"
printf '# PROGRESS\n\n## Done\n\n## Next\n\n## Known issues\n' > "$ROOT/PROGRESS.md"
echo "Created $ROOT"
