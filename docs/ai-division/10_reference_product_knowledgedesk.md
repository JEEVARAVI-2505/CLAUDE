# WORKED EXAMPLE — "KnowledgeDesk" from Idea to Paying Customer

A complete walk-through using every stage. Numbers marked *(assumption)* come from `00_START_HERE.md §5` and `tools/cost_model.py`; replace them with real data.

## 0. Intake (Stage 0)

Budget $170k · team 5 · segment SMEs 20–500 staff · idea: private assistant that answers employee/customer questions from company documents, cites sources, and hands over to a human.

## 1. Research summary (Stage 1) — *illustrative hypotheses to validate*

* Pain: staff spend hours searching PDFs/SOPs; support repeats answers.
* Alternatives: intranet search, ChatGPT-style tools with uploads, helpdesk bots, enterprise search vendors. Gaps to test: price for SMEs, data-privacy guarantees, per-document permissions, citation trust, local-language support.
* WTP test: $99–$399/month plans; 3 pilot LOIs required.
* SOM base ≈ $0.8M ARR in year 3 (bottom-up, see Stage 1).

## 2. Product definition (Stage 2, condensed PRD)

| Item | Value |
|---|---|
| Users | Employees (Viewer), Knowledge managers (Editor), Tenant admin |
| Problem | Baseline measured in pilot: 25 min/week/employee searching |
| Journeys | Ask → answer + citations → 👍/👎 → "Talk to human" → ticket draft |
| Inputs | PDF, DOCX, XLSX, CSV, TXT ≤ 25 MB; questions ≤ 4,000 chars |
| Outputs | `AnswerResponse` JSON → UI (answer, citations, confidence) |
| Tools | `search_kb`, `get_document_snippet`, `handoff_to_human` (draft ticket) |
| Autonomous | Answering (class A), drafting ticket (class B) |
| Needs approval | Sending ticket/email externally (class D) — human clicks send |
| Out of scope v1 | NL→SQL, voice, WhatsApp, auto-sending, image generation |
| Targets | Correctness ≥ 88%, hallucination ≤ 3%, p95 first token ≤ 3 s, leak = 0 |
| Cost limit | ≤ $0.0107/answer; ≤ $0.10/conversation hard cap |

## 3. Architecture (Stage 5)

**Level 3 on the decision ladder: single LLM call + RAG** (no multi-agent). One `kd-answer-agent` (spec in Stage 5 §5.6) behind a deterministic workflow:

```
validate input → authz → query rewrite → ACL-filtered hybrid search → rerank
→ context build → answer (LLM, schema) → validate citations → [repair once] → respond / hand-off
```

Stack: FastAPI · PostgreSQL 16 + pgvector · Redis · S3-compatible storage · React · OpenTelemetry · provider SDKs via own model gateway.

## 4. Model selection (Stage 4)

Three configs from the benchmark (names are placeholders until measured):

| Tier | Share of traffic | Used for |
|---|---|---|
| Economical | 70% | Short factual questions, high retrieval score |
| Balanced | 25% | Multi-passage synthesis, tables |
| High | 5% | Low-confidence re-try, long documents |

Fallback: second provider on timeout/5xx. Embeddings: one multilingual model, version-pinned.

## 5. Database (core tables)

```sql
tenants(id uuid pk, name, plan, status, monthly_token_budget, created_at)
users(id uuid pk, tenant_id fk, email, role check (role in ('admin','editor','viewer')), mfa_enabled)
groups(id, tenant_id, name)           user_groups(user_id, group_id)
documents(id, tenant_id, title, status, current_version, retention_until, deleted_at)
document_versions(id, document_id, version int, storage_key, sha256, uploaded_by, active bool)
document_acl(document_id, group_id)    -- who may read
chunks(id, tenant_id, document_version_id, ord, text, page, section, acl_group_ids uuid[],
       embedding vector(1024), embedding_model text, tsv tsvector)
conversations(id, tenant_id, user_id, created_at)   messages(id, conversation_id, role, content_redacted, created_at)
answers(id, message_id, prompt_id, prompt_version, model_id, tokens_in, tokens_out, cost_usd,
        latency_ms, confidence, needs_human, citations jsonb, feedback smallint)
usage_events(id, tenant_id, kind, units, cost_usd, created_at)      -- idempotent key
approvals(id, tenant_id, run_id, tool, args jsonb, status, decided_by, decided_at)
audit_log(id, tenant_id, actor, action, target, at, details jsonb)
-- Row-level security: USING (tenant_id = current_setting('app.tenant_id')::uuid) on every table
```

## 6. API (FastAPI, versioned `/v1`)

| Method & path | Role | Purpose |
|---|---|---|
| `POST /v1/auth/login` (OIDC callback) | – | Sign-in |
| `POST /v1/chat` (SSE stream) | viewer+ | Ask a question → `AnswerResponse` |
| `POST /v1/chat/{id}/feedback` | viewer+ | 👍/👎 + comment |
| `POST /v1/documents` | editor+ | Upload (multipart) → ingestion job |
| `GET /v1/documents` / `GET /v1/documents/{id}` | editor+ | List, status, versions |
| `PUT /v1/documents/{id}/acl` | editor+ | Set groups |
| `DELETE /v1/documents/{id}` | editor+ | Delete + purge |
| `GET /v1/usage` | admin | Usage vs plan |
| `POST /v1/handoff` | viewer+ | Create ticket draft |
| `POST /v1/approvals/{id}/decision` | editor+ | Approve/reject a pending action |
| `GET /healthz`, `/readyz` | internal | Probes |

Common rules: JWT → tenant/user from token; Pydantic validation; 429 on limits with `Retry-After`; idempotency header on POST; request-id in every response.

## 7. UI screens

1. **Chat** — message box, streaming answer, citation chips (click → source snippet with page), 👍/👎, "Talk to a human", permanent disclaimer line.
2. **Documents** — upload area, status (queued/parsing/OCR/indexed/failed), version history, access groups, delete.
3. **Admin → Users & roles**, **Usage & budget** (bar vs plan), **Audit log**.
4. **Review queue** — pending approvals (draft tickets), approve/edit/reject.
5. **Feedback dashboard** — thumbs-down questions with the retrieved chunks (for fixing knowledge gaps).
Accessibility: keyboard navigation, contrast, language switcher.

## 8. Build plan (Stages 7–9) — 8-week sprint plan

| Sprint | Deliverable |
|---|---|
| S1 (wk 1–2) | Repo, CI, schemas, auth skeleton, tenant RLS, model gateway with mock provider |
| S2 (wk 3–4) | Ingestion (PDF/DOCX/TXT), chunking, embeddings, pgvector, hybrid search, ACL filter |
| S3 (wk 5–6) | Answer workflow, citation validator, chat UI, documents UI, usage metering |
| S4 (wk 7–8) | XLSX/CSV/OCR, hand-off + approvals, admin screens, budgets/rate limits, eval harness at full strength |
Evaluation runs from S2 onward; do not wait until the end.

## 9. Testing & security (Stage 10)

Golden set 300 cases; thresholds as in Stage 10 §10.4; cross-tenant leak tests in CI; injection documents in `data/evaluation/adversarial/`; deletion test; load test 50 concurrent users; pen-test before first customer.

## 10. Production cost and price (Stage 11)

| Item | Value |
|---|---|
| Development one-time | $105,760 |
| Cost per answer | ≈ $0.0107 |
| Cost per user/month | $0.72–$2.45 depending on scale |
| Cost per successful task | ≈ $0.0126 |
| Selling price | Starter $99 · Professional $399 · Business $1,199 · Enterprise from $3,500 |
| Gross margin | 81% / 70% / 52% |
| LTV : CAC | ≈ 10 (assumed churn 3%; validate) |
| Break-even | ≈ 50 Professional customers; profit +$14k/month at 100 |

## 11. Deployment (Stage 12) · Customer onboarding (Stage 13) · Maintenance (Stage 14)

Start on the Lean MVP model ($180/month infra) with 3 pilot tenants; move to Professional when a scaling trigger fires. Onboard each customer using the 3-week plan; review weekly; run model benchmark quarterly.

## 12. What the new employee does first (first 30 days)

| Week | Tasks |
|---|---|
| 1 | Read handbook 00–03; set up laptop; "hello PR"; shadow a customer interview |
| 2 | Write 20 golden-set cases for a sample domain; run eval harness; read ADRs |
| 3 | Implement one small tool end-to-end with tests (e.g. `get_document_snippet`) following Stage 8 |
| 4 | Fix one retrieval-quality issue with measured before/after; present at the demo |
