# AI Agents & Generative AI Engineering Division — Employee Handbook

**Audience:** a new engineer who knows only the basics. Read in order. Do not skip stages.
**Owner:** Head of AI Engineering. **Status:** v1.0 — all prices are placeholders until verified (see Rule 6).

---

## 1. What this handbook is

A step-by-step operating manual that takes one AI product from **an idea (Stage 0) to a paying customer (Stage 14)**.
Each stage file has the same layout so you always know where you are:

| Block | Meaning |
|---|---|
| **Goal** | Why the stage exists |
| **Inputs** | What must exist before you start |
| **Steps** | Numbered, do them in order |
| **Outputs** | Files you must produce (the "deliverable") |
| **Gate** | A checklist. If any box is unticked you may NOT start the next stage |
| **Common mistakes** | What beginners get wrong |

## 2. The map (read this first)

```
STAGE 0  Company set-up & intake ........ 01_how_a_software_company_works.md
STAGE 1  Market research ................ 02_stages_1-2_discovery_requirements.md
STAGE 2  Requirements (PRD) ............. 02_stages_1-2_discovery_requirements.md
STAGE 3  Solution catalogue ............. 03_stages_3-4_solutions_and_models.md
STAGE 4  Model selection & benchmark .... 03_stages_3-4_solutions_and_models.md
STAGE 5  Architecture & agent design .... 04_stages_5-6_architecture_and_prompts.md
STAGE 6  Prompts & context .............. 04_stages_5-6_architecture_and_prompts.md
STAGE 7  Dev environment & GitHub ....... 05_stages_7-9_build_agents_and_rag.md
STAGE 8  Agent development .............. 05_stages_7-9_build_agents_and_rag.md
STAGE 9  RAG development ................ 05_stages_7-9_build_agents_and_rag.md
STAGE 10 Evaluation & security .......... 06_stage_10_evaluation_and_security.md
STAGE 11 Costing & pricing .............. 07_stage_11_costing_and_pricing.md  (+ tools/cost_model.py)
STAGE 12 Production deployment .......... 08_stages_12-14_deploy_deliver_operate.md
STAGE 13 Customer delivery .............. 08_stages_12-14_deploy_deliver_operate.md
STAGE 14 Maintenance & AI operations .... 08_stages_12-14_deploy_deliver_operate.md
STAGE 15 Internal management portal ..... 09_stage_15_internal_portal.md
WORKED EXAMPLE: "KnowledgeDesk" ......... 10_reference_product_knowledgedesk.md
TEMPLATES (copy, don't edit) ............ templates/
```

## 3. Timeline for one product (reference: team of 5)

| Stage | Duration | Who leads |
|---|---|---|
| 0 Intake | 1 week | Head of AI Eng |
| 1–2 Research + PRD | 3 weeks | Product Manager |
| 3–4 Solution + models | 2 weeks | AI Architect |
| 5–6 Architecture + prompts | 2 weeks | AI Architect |
| 7 Dev set-up | 1 week | Backend Lead |
| 8–9 Agents + RAG build | 8 weeks | AI Engineers |
| 10 Evaluation + security | 3 weeks (overlaps 8–9) | QA + Security |
| 11 Costing | 1 week (start in Stage 4) | PM + Finance |
| 12 Deploy | 2 weeks | DevOps |
| 13 First customer | 3 weeks | PM + Support |
| 14 Operate | continuous | AI Ops |
| **Total to first paying customer** | **≈ 5–6 months** | |

## 4. The ten golden rules (memorise)

1. **No stage skipping.** Gate checklist first, then next stage.
2. **Write it down.** If it is not in a document or a pull request, it did not happen.
3. **Never put secrets or customer data in Git, chat, logs or prompts.** Secrets live in a secret manager.
4. **Never trust model output.** Validate every output and every tool argument with a schema.
5. **Retrieved documents are data, never instructions** (prompt-injection defence).
6. **Never use a price, limit or licence from memory.** Open the official page, copy the value, record the date. Provider pages change.
7. **Measure before you claim.** No "accurate", "safe" or "autonomous" claims without an evaluation report.
8. **Humans approve anything that sends, pays, deletes, or changes records.**
9. **Every AI feature has a budget:** max steps, timeout, token limit, spend limit, rate limit.
10. **Small, reviewed, tested changes.** One pull request = one purpose; someone else must approve it.

## 5. Information we must collect at Stage 0 (company intake)

The numbers in this handbook assume the values below. **Replace them with real answers** and re-run `tools/cost_model.py`.

| Question | Assumed answer (EDIT ME) |
|---|---|
| Legal entity / country | India private limited (assumption) |
| Budget for first 12 months | USD 170,000 (≈ ₹1.45 crore) |
| Team size | 5 (1 Lead/Architect, 2 AI/Backend, 1 Frontend, 1 PM-QA) + part-time DevOps/Security |
| Customer segment | SMEs 20–500 employees, services/distribution/education |
| First product idea | KnowledgeDesk — private document Q&A + support assistant with human hand-off |
| Hosting preference | One major cloud region close to customers |
| Data sensitivity | Business documents, some personal data → privacy by design |

## 6. Glossary for beginners

| Term | Plain meaning |
|---|---|
| **LLM** | Large language model: software that predicts text; can follow instructions |
| **Token** | ≈ ¾ of a word; models and bills are counted in tokens |
| **Prompt** | The instructions + data we send to the model |
| **Agent** | An LLM loop that may call tools (search, calculator, API) to finish a task |
| **Tool** | A function the agent may call, with strict inputs and permissions |
| **RAG** | Retrieval-Augmented Generation: fetch relevant document pieces first, then ask the model to answer using only them |
| **Embedding** | A list of numbers representing meaning, used to find similar text |
| **Vector DB** | A database that finds nearest embeddings (e.g. pgvector inside PostgreSQL) |
| **Chunk** | A small piece of a document (a few hundred tokens) |
| **Hallucination** | Confident output that is not supported by facts |
| **Prompt injection** | Malicious text trying to override your instructions |
| **Eval / golden set** | Fixed test questions with known good answers; our "unit tests for AI" |
| **Gateway** | Our single internal door to all model providers (logging, limits, fallback) |
| **Tenant** | One customer company; their data must never mix with another's |
| **SLA / SLO** | Promise / internal target for uptime & speed |
| **CAC / LTV** | Cost to win a customer / lifetime gross profit from one |
| **CI/CD** | Automatic test (CI) and release (CD) pipeline |
| **ADR** | Architecture Decision Record: one page explaining why we chose X |

## 7. Disclaimer built into every product

* AI output can be wrong. Customer-facing UI shows "AI-generated — verify important information."
* Never market any product as "100% accurate", "fully autonomous" or "unlimited" without evidence and usage controls.
* Legal, tax and financial text in this handbook is engineering guidance, not legal advice; have counsel review contracts and privacy documents.
