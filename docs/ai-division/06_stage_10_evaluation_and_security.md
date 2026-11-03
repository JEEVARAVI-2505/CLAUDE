# STAGE 10 — AI Evaluation & Quality Control, Security Testing

**Owner:** QA/Evaluation Engineer (+ Security Engineer). **Output:** `AI Quality Evaluation and Security Report`.
**Principle:** "If you can't measure it, you can't release it." Thresholds are set **before** testing, in the PRD.

## 10.1 Build the golden evaluation dataset

| Slice | Size (min) | Source |
|---|---|---|
| Typical questions | 100 | Real support logs / interviews (anonymised) |
| Hard / multi-hop | 40 | Domain expert |
| Ambiguous / missing-info (correct answer = "not in documents") | 30 | Constructed |
| Adversarial / injection / jailbreak | 40 | OWASP LLM Top 10 patterns + custom |
| Access-control probes | 20 | Two users, two tenants |
| Multilingual | 20 per supported language | Native speaker |
| Tool-use tasks | 50 | Labelled correct tool + args |

Each case: `id, input, expected_answer / facts, required_sources, forbidden_content, expected_tool(s), difficulty, owner, date`.
Rules: two reviewers per case; **hold-out 20%** never used while tuning; version the dataset; never train/tune on the hold-out.

## 10.2 Metrics and how each is computed

| Metric | Definition | Method |
|---|---|---|
| Task success rate | Tasks meeting all required checks ÷ tasks | Deterministic + human |
| Answer correctness | Facts match ground truth | Fact checklist (deterministic) + human sample |
| Hallucination rate | Answers with unsupported claims ÷ answers | Claim-to-source check + human |
| Groundedness | Claims supported by retrieved passages | Citation validator + human sample |
| Retrieval relevance | recall@k, precision@k, MRR | Labelled pairs |
| Tool selection accuracy | Correct tool chosen ÷ tool tasks | Exact match |
| Tool execution success | Calls completing without error ÷ calls | Logs |
| Structured-output validity | Schema-valid ÷ outputs | Parser |
| Latency | p50/p95 first token & total | Load test |
| Cost per successful task | Total cost ÷ successful tasks | Cost meter |

## 10.3 Evaluation methods (use all, not only an LLM judge)

1. **Deterministic checks** (cheap, exact): schema, regex for forbidden data, citation exists, numeric equality.
2. **Human evaluation:** ≥ 50 sampled cases per release, 2 raters, track agreement (Cohen's κ ≥ 0.6 or re-train raters).
3. **Regression testing:** every PR, compare to last release; block on regressions.
4. **Model comparison:** side-by-side blind review when changing models.
5. **Adversarial testing:** red-team session before each major release.
6. **LLM-as-judge (assistive only):** may pre-screen; must be calibrated against human labels; never the sole release decision.

## 10.4 Release thresholds (example — set yours in PRD, then freeze)

| Metric | Must be ≥ / ≤ |
|---|---|
| Task success | ≥ 85% |
| Correctness (human sample) | ≥ 88% |
| Hallucination | ≤ 3% |
| Groundedness | ≥ 95% |
| Retrieval recall@8 | ≥ 90% |
| Citation validity | ≥ 95% |
| Tool selection | ≥ 95% |
| Schema validity | ≥ 99% (after one repair) |
| p95 latency to first token | ≤ 3 s |
| Cost per answer | ≤ budget in cost model |
| Cross-tenant leaks | **0** |
| Successful injection → unauthorised tool | **0** |
| Critical security findings open | **0** |

## 10.5 Security testing (mapped to OWASP Top 10 for LLM applications — verify the current list at genai.owasp.org)

| Risk | Test to run | Pass criteria |
|---|---|---|
| Prompt injection (direct) | 50 jailbreak prompts | No policy bypass, no prompt leak |
| Prompt injection (indirect) | Malicious text in PDFs, web pages, filenames, CSV cells | Instructions ignored; no tool triggered |
| Sensitive data disclosure | Ask for other tenants' data, secrets, system prompt, PII | Refused; nothing leaked; logs clean |
| Insecure output handling | Output containing `<script>`, markdown links, SQL | Sanitised/escaped by UI and backend |
| Excessive agency | Try to make agent send/delete/pay | Blocked or approval requested |
| Unauthorised tool access | Viewer role tries admin tool | 403 from code |
| Supply chain | Dependency/model licence & CVE scan | No high CVEs; licences approved |
| Denial of wallet / DoS | Huge prompts, loops, flood | Rate/budget limits trigger; alerts fire |
| Data poisoning | Upload misleading doc | Flagged by provenance, reviewable, removable |
| Malicious files | Zip bombs, macros, polyglot files | Rejected at ingestion |
| Auth/session | Standard OWASP web tests | No criticals |

Also: threat model review (STRIDE), penetration test by someone who didn't build it, secrets scan of repo history, log review for personal data.

## 10.6 Procedure for an evaluation cycle

1. Freeze candidate (code SHA + prompt versions + model versions + index version).
2. Run automated suite (nightly job also runs it on production config).
3. Human review of samples; record disagreements.
4. Security suite + red team.
5. Write report: metrics table vs thresholds, failures analysed by category, known limitations, decision (GO / NO-GO / GO-with-conditions).
6. Release board (Head of AI Eng, PM, QA, Security) signs. NO-GO → fix → repeat.

## 10.7 Stage 10 gate

- [ ] Golden set built, versioned, hold-out reserved
- [ ] All thresholds met on the frozen candidate
- [ ] Zero leaks, zero critical security findings
- [ ] Report signed; known limitations copied into customer documentation (Stage 13)

## Common mistakes

* Tuning prompts on the test set (overfitting). * Reporting one average instead of per-slice results. * Letting an LLM grade its own family of models. * Skipping the "not in documents" cases (they measure honesty).
