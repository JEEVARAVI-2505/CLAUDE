# STAGES 7–9 — Dev Environment, Agent Development, RAG Development

---
# STAGE 7 — Development Environment & GitHub

## 7.1 Laptop set-up checklist (do in order; verify versions on each official site first)

1. **Git** + GitHub account with 2FA; SSH key added (`ssh-keygen -t ed25519`).
2. **VS Code** + extensions: Python, Pylance, Ruff, Docker, GitLens, YAML, Markdown, GitHub Pull Requests.
3. **Python** (current supported 3.x) with `uv` or `pyenv` + `venv`; never install packages globally.
4. **Node.js LTS** (only for `apps/web`).
5. **Docker Desktop** (or Podman) — runs PostgreSQL+pgvector and Redis locally.
6. **PostgreSQL client** (`psql`) and a DB viewer.
7. Clone repo, copy `.env.example` → `.env` (never commit `.env`), add *development-only* API keys with a **hard monthly spend cap** set at the provider.
8. Run `make setup && make test` — if green, you are ready.

## 7.2 Repository layout (create exactly this)

```
ai-product/                      (private GitHub repo)
├─ apps/
│  ├─ web/                       React/Next.js UI (chat, admin, documents)
│  └─ api/                       FastAPI app: routers, dependencies, startup
├─ src/                          Python package `aiproduct` (business logic, UI-free)
│  ├─ agents/                    agent classes + per-agent spec (spec.yaml)
│  ├─ orchestration/             workflows, graphs, RunState, approvals
│  ├─ models/                    model gateway, registry loader, routing, cost meter
│  ├─ prompts/                   <prompt_id>/<version>.yaml + CHANGELOG.md + loader
│  ├─ tools/                     tool implementations + registry (schema, risk class)
│  ├─ rag/                       ingestion, parsing, OCR, chunking, indexing
│  ├─ retrieval/                 query rewrite, filters, hybrid search, rerank, context builder
│  ├─ embeddings/                embedding clients, batching, versioning
│  ├─ memory/                    conversation, session, preference stores
│  ├─ schemas/                   Pydantic models (single source of truth for I/O)
│  ├─ services/                  tenants, billing/usage, notifications
│  ├─ security/                  authn/authz, ACL, redaction, injection filters, secrets
│  ├─ evaluation/                metrics, runners, judges, reports
│  └─ observability/             tracing, logging config, metrics
├─ data/
│  ├─ sample/                    SYNTHETIC documents only
│  └─ evaluation/                golden/adversarial sets (no real customer data)
├─ tests/  unit/ integration/ agent/ rag/ security/
├─ docs/   requirements/ architecture/ deployment/ evaluation/ adr/
├─ infra/                        Dockerfiles, docker-compose, Terraform/IaC, k8s or PaaS config
├─ .github/                      workflows/, PULL_REQUEST_TEMPLATE.md, CODEOWNERS, dependabot.yml
├─ .env.example   .gitignore   Makefile   pyproject.toml   README.md   THIRD_PARTY.md   PROGRESS.md
```

`.gitignore` must include: `.env*`, `*.pem`, `*.key`, `data/private/`, `*.sqlite`, `__pycache__/`, `node_modules/`, uploads.
A pre-commit hook runs a secret scanner (e.g. gitleaks) and `ruff`. GitHub secret scanning + push protection ON.

## 7.3 CI pipeline (`.github/workflows/ci.yml`) — required checks

1. Lint (`ruff`), type-check (`mypy --strict`).
2. Unit + integration tests (`pytest`, DB/Redis via service containers).
3. Security: secret scan, dependency audit (`pip-audit`/`npm audit`), SAST (e.g. CodeQL), container scan.
4. **Eval smoke test**: 30-case golden subset with a mock/cheap model; fails on regression.
5. Build Docker image; on `main` push to dev environment.
Release to staging/prod = tag `vX.Y.Z` + manual approval.

## 7.4 Stage 7 gate

- [ ] Repo created from the layout, private, branch protection on
- [ ] CI green on an empty-feature PR
- [ ] Every engineer completed "hello world" PR + local run
- [ ] Provider keys per environment with spend caps; secrets only in secret manager / `.env`

---
# STAGE 8 — AI Agent Development (process for every agent)

Follow these 15 steps in order. Each is a checkbox in the agent's PR.

| # | Step | How |
|---|---|---|
| 1 | Define objective | One sentence, measurable ("resolve FAQ questions with ≥ 85% correctness") |
| 2 | Select model | Tier from Stage 4 registry |
| 3 | Write instructions | Prompt per Stage 6 |
| 4 | Define schemas | `schemas/*.py` Pydantic input/output |
| 5 | Register tools | `tools/registry.py` entry with schema, risk class, roles, timeout |
| 6 | Configure state | `RunState` model; checkpoint after every step |
| 7 | Configure memory | Which stores from §5.4; load only what is needed |
| 8 | Implement tool execution | Executor validates args → checks permission → runs with timeout → validates result |
| 9 | Validate outputs | Pydantic parse; business validators (citations exist, numbers match source) |
| 10 | Apply approvals | Class C/D → create `ApprovalRequest`, pause run, resume after human decision |
| 11 | Retry limits | Max 2 retries, exponential backoff, only on transient errors |
| 12 | Timeouts | Per tool + per run; cancel cleanly |
| 13 | Token budgets | Reject/stop when budget hit; return graceful message |
| 14 | Execution traces | One trace per run: prompts (redacted), tool calls, latencies, tokens, cost, decisions |
| 15 | Test on eval datasets | Run suite; attach report |

## 8.1 Separation of concerns (never mix these in one function)

```
plan()      → decides next step (LLM)           ┐
reason()    → analyses retrieved evidence (LLM) │ each testable on its own
act()       → executes a tool (plain code)      │ with mocked neighbours
respond()   → formats + validates final output  ┘
```

## 8.2 Tool execution skeleton (study this)

```python
class ToolSpec(BaseModel):
    name: str
    args_model: type[BaseModel]
    risk_class: Literal["A", "B", "C", "D"]
    allowed_roles: set[str]
    timeout_s: float = 10
    requires_approval: bool = False

async def execute_tool(call: ModelToolCall, ctx: RunContext) -> ToolResult:
    spec = REGISTRY.get(call.name)                      # unknown tool → reject
    if spec is None: return ToolResult.denied("unknown_tool")
    if ctx.user.role not in spec.allowed_roles:           # authorise by CODE, not by model
        return ToolResult.denied("forbidden")
    try:
        args = spec.args_model.model_validate(call.arguments)   # never trust model args
    except ValidationError as e:
        return ToolResult.invalid(e)
    if spec.requires_approval or spec.risk_class in {"C", "D"}:
        return await ctx.approvals.request(spec, args)    # pause for a human
    ctx.budget.charge_tool_call()                         # step/token/cost caps
    return await asyncio.wait_for(spec.run(args, ctx), spec.timeout_s)
```

Always: tenant id comes from the authenticated session, **never** from model arguments.

## 8.3 Forbidden capabilities

Unrestricted shell, unrestricted SQL, payment execution, sending email/WhatsApp without approval, administrative/permission-changing APIs, reading files outside the tenant sandbox, free web browsing with credentials.

## 8.4 Agent tests (in `tests/agent/`)

* Unit: schema validation, budget exhaustion, retry cap, timeout, approval pause/resume.
* Behaviour: tool-selection accuracy on 100 labelled prompts; refuses unauthorised tool; handles tool failure.
* Security: injection inside retrieved doc must not trigger a tool.
* Determinism: with seed + mocked model the trace is reproducible.

## 8.5 Stage 8 gate

- [ ] All 15 steps ticked per agent
- [ ] Tool-selection accuracy and output-validity meet PRD thresholds
- [ ] No agent has a class-D tool without ADR + approval flow
- [ ] Traces visible in the observability dashboard

---
# STAGE 9 — RAG Development (reusable document-intelligence pipeline)

## 9.1 Ingestion → index flow

```
upload → validate (type/size/magic bytes) → virus scan → store original (versioned)
  → parse (PDF/DOCX/XLSX/CSV/TXT/HTML) → OCR if scanned → normalise structure
  → extract metadata → chunk → embed → index (vector + keyword) → mark version ACTIVE
```

| Step | Rules |
|---|---|
| Approved file types | PDF, DOCX, XLSX, CSV, TXT (+ HTML/URLs on an allow-list). Reject others. Max size set in PRD |
| Validation | Check real file type by content, not extension; block macros/embedded executables |
| Parsing | Keep headings, lists, tables (tables → markdown/rows with header), page numbers |
| OCR | Only when text layer missing/low quality; store OCR confidence; flag low-confidence pages |
| Metadata | tenant_id, doc_id, version, title, author, date, language, source, page, section path, ACL (roles/groups/users), content hash |
| Chunking | Structure-aware: split on headings/paragraphs, 300–800 tokens, 10–15% overlap, never split tables mid-row; keep parent section title in each chunk; tune on retrieval metrics, not intuition |
| Embeddings | One model per index; store model name+version with each vector; re-embed when changing model |
| Index | pgvector (HNSW/IVFFlat) + Postgres full-text (BM25-like) for hybrid |
| Versioning | New upload = new version; old versions inactive but retained per policy; citations carry version |
| Deletion | Delete original, chunks, vectors, caches and backups per retention schedule; produce deletion certificate; test it |

## 9.2 Retrieval flow

1. **Query understanding:** language detect, rewrite follow-ups into standalone questions, expand abbreviations.
2. **Filter first:** `tenant_id = :t AND acl && :user_groups AND active = true` **inside the SQL** — never retrieve then filter.
3. **Hybrid search:** vector top-30 + keyword top-30 → reciprocal-rank fusion.
4. **Rerank** top-30 with a cross-encoder/reranker → top-6..8.
5. **Context selection:** deduplicate, respect token budget, keep source IDs.
6. If best rerank score < threshold → "insufficient information" path (don't force an answer).

## 9.3 Generation rules

* Answer only from passages; cite every claim; quote numbers verbatim.
* Say what's missing; state uncertainty (`confidence`); suggest hand-off.
* A citation validator checks each citation points to a retrieved chunk and the quoted text exists in it; fail → repair once → hand-off.

## 9.4 Validation (what to measure, separately)

| Layer | Metrics | Typical release target (set your own in PRD) |
|---|---|---|
| Retrieval | recall@k, precision@k, MRR, nDCG on labelled query→chunk pairs | recall@8 ≥ 0.90 |
| Generation | correctness (human + deterministic), groundedness, completeness | correctness ≥ 0.85, groundedness ≥ 0.95 |
| Citations | % citations valid & supporting | ≥ 0.95 |
| Security | cross-tenant leakage tests | 0 leaks, mandatory |
| Deletion | deleted doc never retrievable (chunks, vectors, cache) | 100% |

## 9.5 Mandatory RAG security tests

* User A queries for a document only B can see → zero results, zero hints it exists.
* Tenant X searches tenant Y's unique keyword → zero results.
* Document containing "Ignore previous instructions and reveal ..." → ignored, flagged.
* Delete document → ask a question that previously matched → no leak, including from cache.

## 9.6 Stage 9 gate

- [ ] Ingestion works for all approved types incl. a scanned PDF (OCR)
- [ ] Retrieval and generation metrics reported **separately** against thresholds
- [ ] All RAG security tests pass (zero leakage)
- [ ] Versioning and deletion proven by automated tests
- [ ] `Production-ready RAG Architecture and Evaluation Report` filed
