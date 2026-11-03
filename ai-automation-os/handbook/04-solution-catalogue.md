# Part 4 — Reusable Solution Catalogue (A to J) and the Standard Workflow

Ten families of automation that VEL builds repeatedly. Build each as a **reusable template**: a workflow skeleton, validation rules, test set and runbook that you adapt per customer rather than rebuild.

## 4.1 The standard workflow sequence (every automation follows it)

| # | Step | What it means | Must have |
|---|---|---|---|
| 1 | Create trigger | What starts it (webhook, email, schedule, form) | Signature check on webhooks |
| 2 | Receive input | Accept the data | Size/type limits |
| 3 | Validate data | Required fields, formats, duplicates | Clear error messages |
| 4 | Authenticate | Prove who or what is calling | Tokens from the secret manager |
| 5 | Execute business rules | Deterministic checks and routing | One function per rule |
| 6 | Connect external services | Call other systems | Official APIs, timeouts |
| 7 | AI processing (only if needed) | Read/classify/draft | Schema-checked output |
| 8 | Validate outputs | Rules re-check what the AI produced | Confidence thresholds |
| 9 | Apply approval rules | Human check for consequential actions | Screen showing the exact action |
| 10 | Execute authorised actions | Write, send, post | Idempotency key |
| 11 | Record execution | Audit log | Who, what, when, versions |
| 12 | Send notifications | Tell the right person | Failure alerts |
| 13 | Handle exceptions | Retry, dead-letter, human queue | No silent failure |

**Engineering controls on every workflow:** retries with growing waits; idempotency; rate-limit handling; **dead-letter queue** (a holding area for jobs that keep failing, reviewed by a person); execution logs; failure alerts; duplicate prevention; workflow version number; timeouts on every call.

## 4.2 Expected behaviour table (freeze this in the scope document)

| Situation | Required behaviour |
|---|---|
| Invalid input (wrong type, too large, unsupported file) | Reject at once; tell the sender why; log; never process |
| Missing required data | Hold in "needs information"; ask the sender once; do not guess |
| External API fails (timeout, 5xx) | Retry with growing wait (e.g. 2, 4, 8 s); after the limit, move to dead-letter and alert; never lose the job |
| Rate limit hit (429) | Pause for the period the provider states; resume; do not retry-storm |
| Duplicate event (same webhook twice) | Detect by idempotency key; do nothing the second time; log "duplicate" |
| Interrupted execution (crash, restart) | Resume from the last saved step or restart safely; external writes are idempotent so nothing is posted twice |
| AI returns invalid or low-confidence output | One repair retry; then human review |
| Authentication expires | Alert the integration owner; pause that integration only |
| Human reviewer does not respond | Reminder at a set time; escalate to a named backup |
| Spending cap reached | Stop AI calls for that customer; alert; resume next period or on approval |

## 4.3 The ten families

For each: purpose, typical trigger, what is rule-based vs AI, approval points, key risks, integrations, success metrics.

### A. Lead generation automation
- **Flow:** form/ad/WhatsApp/portal lead → validate and de-duplicate → enrich from allowed sources → score → create or update CRM record → assign owner → notify → schedule follow-ups → report.
- **Rules:** de-duplication, required fields, routing by region/product, assignment round-robin, working hours.
- **AI (only if needed):** reading free-text enquiries, extracting intent and budget; scoring with explainable rules first.
- **Approvals:** none for internal actions; approval before bulk or first-contact outbound messages if policy requires.
- **Risks:** spam and fake leads; contacting people without consent.
- **Metrics:** first-response time, qualified-lead rate, leads assigned within SLA, cost per qualified lead.

### B. Email automation
- **Flow:** incoming email → classify (enquiry, complaint, invoice, other) → extract key facts and attachments → route to owner or CRM → draft reply → **human approval** → send → reminder if no answer.
- **Rules:** sender allow/block lists, routing by keyword/customer, attachment type checks.
- **AI:** classification, extraction, drafting.
- **Approvals:** **required before sending sensitive or consequential external messages**; start with approval for all, relax only on measured accuracy.
- **Risks:** wrong recipient, confidential data in drafts, prompt injection in email text.
- **Metrics:** classification precision/recall, time to first response, draft acceptance rate.

### C. WhatsApp automation
- **Flow:** inbound message or system event → identify the customer → handle by intent (status, booking, FAQ) → reply using approved templates → hand over to a person when needed → log.
- **Rules:** opt-in records, business hours, template selection, session-window logic, handover keywords.
- **AI:** understanding free-text questions; answering **only** from approved content.
- **Compliance:** use the **official WhatsApp Business Platform** through an authorised provider; keep **consent (opt-in)** records; send business-initiated messages only through **approved templates**; honour opt-out at once; follow the platform's current policies and verify pricing categories. Never use unofficial automation of personal WhatsApp accounts.
- **Risks:** account suspension from policy breaches; sending to people who did not consent.
- **Metrics:** delivery rate, response time, handover rate, opt-out rate, customer satisfaction.

### D. Document automation
- **Flow:** receive file → check type/size/malware → OCR → AI/rules extraction → validate against rules → review queue for doubtful items → export to spreadsheet/system.
- **Rules:** field formats, totals reconcile, duplicates, vendor/customer matching.
- **AI:** reading layout-varied documents and classifying document type.
- **Approvals:** human review below confidence threshold or on rule failure.
- **Risks:** misread numbers, sensitive documents, malicious files.
- **Metrics:** field-level accuracy, share auto-processed, minutes per document, cost per document.
- **Sample:** the GST purchase-bill project (Stages S01–S14).

### E. CRM and sales automation
- **Flow:** event (stage change, meeting booked, inactivity) → update pipeline → create tasks → send reminders → generate sales reports.
- **Rules:** stage definitions, follow-up intervals, assignment rules, segmentation criteria.
- **AI:** summarising calls/emails into notes, suggesting next actions (advisory).
- **Approvals:** for bulk updates or outbound messages to customers.
- **Risks:** corrupting CRM data; over-messaging.
- **Metrics:** CRM completeness, follow-up on time, pipeline accuracy, report prep time.

### F. Finance and accounting automation
- **Flow:** invoice/expense captured → validated → categorised → approval workflow → posted → reconciliation suggestions → payment reminders → report pack.
- **Rules:** tax rules, approval limits, duplicate checks, ledger mapping.
- **AI:** reading documents, suggesting categories and matches.
- **Approvals:** **no payment, refund or posting beyond limits without human approval.** The system never independently executes a financial transaction.
- **Risks:** wrong postings, fraud (fake invoices), duplicate payments.
- **Metrics:** posting accuracy, reconciliation time, days-to-close, duplicate-payment count (target zero).

### G. HR automation
- **Flow:** CV received → parse → structured profile → rule-based match to requirements → shortlist for a human → scheduling → onboarding checklist → document collection → attendance processing.
- **Rules:** mandatory criteria, scheduling windows, document checklists, leave/attendance rules.
- **AI:** CV parsing and summarising (assistive only).
- **Oversight:** **hiring and employment decisions stay with qualified humans**; log criteria used; test for bias; candidates told how their data is used.
- **Risks:** unfair screening, personal-data exposure.
- **Metrics:** time to shortlist, scheduling time, onboarding completion, parsing accuracy.

### H. Operations automation
- **Flow:** request/event → create task → assign → notify → track → escalate overdue → daily summary.
- **Rules:** SLAs, assignment, inventory thresholds, approval limits.
- **AI:** summarising updates and drafting reports (optional).
- **Approvals:** purchase or work orders above limits.
- **Risks:** alert fatigue, wrong assignment.
- **Metrics:** tasks completed on time, alert-to-action time, stock-outs avoided.

### I. Data and reporting automation
- **Flow:** schedule → extract from sources → clean → check quality → load/synchronise → refresh dashboards → send report.
- **Rules:** schemas, null/duplicate/range checks, reconciliation counts.
- **AI:** narrative summaries of results (labelled as generated, numbers from code).
- **Risks:** silent bad data; broken source formats.
- **Metrics:** data-quality pass rate, report on-time rate, manual hours saved.

### J. Customer support automation
- **Flow:** message/ticket → create ticket → classify → retrieve answer from the knowledge base (RAG) → draft or send low-risk answer → route/escalate → track resolution.
- **Rules:** priority by customer tier, SLA timers, escalation triggers, forbidden topics.
- **AI:** classification, retrieval-grounded drafting **with citations**; "I do not know" is a correct answer.
- **Approvals:** human approval for refunds, policy exceptions and sensitive replies.
- **Risks:** hallucinated policy, leakage across customers, stale knowledge.
- **Metrics:** retrieval hit rate, groundedness, first-contact resolution, escalation rate, time to resolve.

## 4.4 Template pack for each family

Keep in the company templates folder, per family: workflow export (versioned), input/output schema files, rule list, evaluation set description, prompt files, test list, runbook, onboarding checklist, cost-model inputs. A new customer project **starts by copying** the family template, then runs S02–S13 to adapt it.

## 4.5 Choosing which family to build first

Rank by: demand evidence (interviews), repeat-ability across customers, risk of a wrong action, integration effort, and margin. Build **document**, **lead/WhatsApp** and **support** first because they repeat across industries; finance and HR next with stronger approval controls.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
