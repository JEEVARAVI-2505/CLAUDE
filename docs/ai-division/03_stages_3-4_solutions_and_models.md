# STAGES 3–4 — Solution Catalogue, then Model Selection

---
# STAGE 3 — What we are able to build (Solution Catalogue)

**Goal:** pick the *simplest architecture that meets the PRD*. Fancy ≠ better.

## 3.1 Decision ladder (always start at the top)

```
1. Can a deterministic rule/workflow do it?     → build that (no AI)
2. Is one LLM call with a good prompt enough?   → single call + schema
3. Does it need documents?                      → RAG
4. Does it need to take actions with tools?     → single agent
5. Are sub-tasks truly independent/specialised
   AND the single agent measurably fails?       → multi-agent
```

Multi-agent systems cost more, fail in more ways and are harder to debug. You need **benchmark evidence** (single agent vs multi-agent on the golden set) to justify one.

## 3.2 Catalogue

| Family | Variants we deliver | Must-have building blocks | Key risks |
|---|---|---|---|
| **A. Chatbots** | Website, support, WhatsApp, internal HR/IT, document chat, multilingual, e-commerce, lead-gen | KB retrieval, conversation memory, escalation to human, analytics | Wrong answers, off-topic, abuse, WhatsApp template/opt-in rules |
| **B. Agents** | Research, data analysis, sales, support, document processing, scheduling, ops, reporting, tool-using | Tool registry, permissions, step/time/token caps, approvals, traces | Runaway loops, wrong tool use, injection |
| **C. Multi-agent** | Planner→Researcher→Executor→Validator→Reporter | Orchestrator, shared state, message schema, retries, fallbacks | Cost blow-up, error compounding |
| **D. RAG / knowledge** | PDF, DOCX, XLSX, CSV, websites, DBs, KBs, approved external | Ingestion, OCR, chunking, metadata, embeddings, hybrid search, rerank, citations, ACLs, versioning, deletion | Leakage across tenants, stale docs, bad parsing |
| **E. Generative** | Writing, reports, documents, image/video integrations, slides, coding, summarisation, translation, data interpretation | Templates, style guides, human review, provider-terms check | IP/licence, factual errors, policy violations |
| **F. Voice** | Voice assistants, STT, TTS, call summaries, voice data collection | Streaming STT/TTS, barge-in, consent prompt, recording policy, escalation | Latency (>1 s feels broken), accents, call-recording laws |
| **G. Data intelligence** | NL→SQL, BI assistant, forecasting, anomaly detection, extraction, decision support | Semantic layer, read-only DB role, query allow-list, row limits | Destructive/expensive SQL, wrong numbers |

## 3.3 Per-family minimum design rules

* **Chatbots:** every answer cites or says "I don't know"; "talk to a human" always visible; transcripts retained per policy; rating buttons feed evals.
* **Agents:** tools declared in a registry with JSON-schema args, a role allow-list and a risk class (A–D). Max steps default 8, timeout 60 s, token budget set per run.
* **Multi-agent:** one *orchestrator* owns state; sub-agents never talk freely to each other — only via typed messages the orchestrator logs. Recovery: retry once → fallback model → degrade to human.
* **RAG:** measure retrieval (recall@k, precision@k, MRR) **separately** from generation (correctness, groundedness).
* **Generative:** disclose AI generation where required; check each provider's current usage policy and output-ownership terms; keep human review before publishing.
* **Voice:** play consent notice before recording; keep end-to-end latency budget (STT + LLM + TTS) ≤ ~1.5 s to first audio; escalate on anger/repeat failure/legal topics.
* **Data intelligence — SQL safety (mandatory):**
  1. Database user is **read-only**, restricted to a schema/views the tenant may see.
  2. Model never writes free SQL against production: it fills a **query template / semantic-layer request**, or generated SQL is parsed (AST) and allowed only if it is a single `SELECT`, uses allow-listed tables/columns, has `LIMIT`, and a statement timeout.
  3. Deny `INSERT/UPDATE/DELETE/DROP/ALTER/GRANT`, multiple statements, comments tricks, functions off the allow-list.
  4. Show the SQL and the result to the user; log both.

## 3.4 Stage 3 gate

- [ ] Chosen architecture level (1–5) justified in an ADR
- [ ] Multi-agent only if benchmark evidence attached
- [ ] Each tool has risk class + approval rule
- [ ] Safety rules for the family (above) copied into the PRD as SEC-xxx requirements

---
# STAGE 4 — Model Research, Selection and Benchmark

**Owner:** AI Architect + QA. **Duration:** 2 weeks. **Output:** `AI Model Selection and Benchmark Report`.

## 4.1 Candidate providers (verify each on its official site — the model list changes monthly)

| Provider | Docs | Notes to check |
|---|---|---|
| OpenAI | platform.openai.com/docs | Models, structured outputs, data-retention controls, regional processing |
| Anthropic | docs.anthropic.com | Models, tool use, prompt caching, retention terms |
| Google Gemini | ai.google.dev/gemini-api/docs | Long context, multimodal, free-vs-paid data-use terms |
| Meta Llama | llama.com | **Open weights; read the licence** (usage thresholds, restrictions) |
| Mistral | docs.mistral.ai | Open + hosted models, EU hosting |
| Hugging Face | huggingface.co/docs | Hub models: check each model's own licence |
| Others | — | Qualify like the rest |

## 4.2 Comparison table (14 criteria — fill with sourced, dated values)

| Criterion | How to measure (not "read a blog") |
|---|---|
| 1 Quality | Our golden set (§4.3) |
| 2 Reasoning | Multi-step questions subset |
| 3 Context window | Official docs; test a real long document |
| 4 Latency | Measure p50/p95 time-to-first-token and total, 100 calls, at our region |
| 5 Structured output | % of 200 calls returning schema-valid JSON |
| 6 Tool calling | Tool selection accuracy + argument validity on 100 tasks |
| 7 Multimodal | Only if PRD needs images/audio; test on real files |
| 8 Token pricing | Official pricing page (input/output/cached), dated |
| 9 Rate limits | Official tier table; compare to peak concurrency |
| 10 Availability | Status page history; do we have a fallback provider? |
| 11 Data retention | DPA/terms: stored? used for training? zero-retention option? |
| 12 Privacy | Region, encryption, sub-processors, compliance reports |
| 13 Deployment | API only / cloud marketplace / self-host |
| 14 Commercial licence | May we resell inside a SaaS? Attribution? Restrictions? |

## 4.3 Benchmark on **our** task (procedure)

1. Build the **golden set**: 150–300 real-style cases (questions + correct answer + source passages), split 50% easy / 35% medium / 15% hard, plus 30 adversarial and 30 "should-refuse/unknown" cases. Domain expert writes ground truth.
2. Freeze the prompt, retrieval settings and temperature. Only the model changes.
3. Run each candidate 3× (variance). Log tokens, latency, cost, output.
4. Score with **deterministic checks first** (schema valid, citation present, exact-match facts, forbidden content absent), then **human review of a sample (≥ 50 cases)**. An LLM-as-judge may *assist* triage but never decides alone.
5. Compute **cost per successful task** = total spend ÷ number of tasks passing all checks.
6. Choose three configurations:

| Config | Use when | Selection rule |
|---|---|---|
| **Economical** | Simple FAQ, classification, routing | Cheapest model that meets the threshold on easy tier |
| **Balanced** *(default)* | Most traffic | Best cost per successful task meeting thresholds on easy+medium |
| **High capability** | Hard reasoning, long docs, escalations | Best quality; used for ≤ ~5–10% of traffic |

7. **Routing:** a cheap classifier (or rules) sends each request to a tier; fall back upward when validation fails; fall back to a second provider when the first is down.
8. Write the decision as an ADR including rejected options and the date prices were checked.

## 4.4 Model governance

* Pin exact model version strings; never use a floating "latest" alias in production.
* Any model change = re-run the full golden set + security set; release only if no metric regresses beyond the agreed tolerance (Stage 10).
* Keep `model_registry.yaml` (id, provider, version, price, limits, licence, data terms, date verified, owner).

## 4.5 Stage 4 gate

- [ ] ≥ 3 candidate models from ≥ 2 providers tested on the golden set
- [ ] Cost per successful task computed for each
- [ ] Three configs (economical/balanced/high) + routing + fallback defined
- [ ] Licence and data-retention terms recorded and approved by Legal/Security
- [ ] ADR signed; `model_registry.yaml` committed
