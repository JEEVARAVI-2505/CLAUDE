# Part 3: The Corporate Workflow: 16 Stages from Lead to Maintenance

## 3.0 How the workflow runs

Each stage has an **owner**, **inputs**, **steps**, a **deliverable** and a **gate**. A gate is a written sign-off. If the gate fails, the project stays in the stage.

| # | Stage | Owner | Main deliverable | Gate |
|---|---|---|---|---|
| 01 | Lead qualification | PM | Lead qualification report | G1 Go / no-go to discovery |
| 02 | Client discovery | Architect | Discovery notes + questionnaire answers | G2 Discovery complete |
| 03 | Requirements freeze | PM | Integration specification v1.0 | G3 Client signs spec |
| 04 | API feasibility | Sr. Engineer | API feasibility report | G4 Feasible / conditional / not feasible |
| 05 | Architecture design | Architect | Architecture document + diagrams | G5 Architecture approved |
| 06 | Cost estimation | PM | Costed estimate, quotation, SOW | G6 Founder approves price |
| 07 | Environment setup | DevOps | Dev, staging and production environments | G7 Environments verified |
| 08 | Repository structure | Sr. Engineer | Repository from the VEL template | G7 (same) |
| 09 | API development | Engineer | Working, tested, documented API | G8 Code review + tests green |
| 10 | System integration | Engineer | Working connectors with retry and idempotency | G8 |
| 11 | API security | Security | Security review report | G9 No open high-severity issue |
| 12 | Testing and QA | QA | Test report | G9 |
| 13 | CI/CD and deployment | DevOps | Staging live; production release approved | G10 Production release approval |
| 14 | Production monitoring | DevOps | Dashboards, alerts, incident process live | G10 |
| 15 | Client handover | PM | Handover pack + signed acceptance | G11 Client acceptance |
| 16 | Maintenance | PM / Sr. Engineer | Monthly support and change log | G12 Quarterly service review |

---

## Stage 01: Lead Qualification

**Goal:** decide quickly and honestly whether this is a project we can deliver profitably.

**Capture:**

| Item | Question to answer |
|---|---|
| Client industry | Which sector? Any regulation (healthcare, finance)? |
| Existing systems | Names, versions, cloud or on-premises |
| Integration requirements | What should connect to what, and why? |
| Business problem | What does the problem cost them today (hours, errors, delays)? |
| Number of applications | Two, or a web of ten? |
| Expected data volume | Records per day / month; peak bursts |
| Required automation | Real-time, hourly, daily? Manual approval steps? |
| Budget | Range or ceiling; who approves? |
| Timeline | Hard deadline and its reason |

**Output: Lead qualification report** (template, Appendix A). Score the lead red / amber / green on: clear problem, reachable decision-maker, API likelihood, budget fit, timeline realism.

**Gate G1:** PM and Founder agree to invest discovery time.

---

## Stage 02: Client Discovery

**Goal:** understand the real process, not the assumed one.

**Discovery checklist** (the full questionnaire is in Appendix B):

- **Source systems** and **destination systems**: product, edition, version, hosting.
- **API availability:** does a documented API exist? Which edition or plan unlocks it?
- **Vendor documentation:** link, version, last-updated date.
- **Authentication method:** API key, OAuth 2.0, basic auth, certificates, VPN.
- **Data formats:** JSON, XML, CSV, flat files, EDI.
- **Rate limits** and quotas, per minute, per day, per tenant.
- **Existing workflows:** who does the task today, in which tool, how often.
- **Data ownership:** who is the system of record for each entity?
- **Failure scenarios:** what happens today when the data is wrong or late?
- **Expected frequency** and peak load.
- **Compliance:** personal data, financial data, health data, data residency, retention.

<div class="warn" markdown="1">
**Do not assume** a third-party platform exposes an API or allows unrestricted integration. Verify **access, permissions, licensing and vendor limitations** in writing. A client saying "yes, it has an API" is a lead, not a fact.
</div>

**Gate G2:** every question answered or recorded as an open risk with an owner.

---

## Stage 03: Integration Requirements Freeze

**Goal:** one written specification that both sides sign. Changes after this point go through change control.

**The integration specification must contain** (template in Appendix C):

| Section | Content |
|---|---|
| Source and destination | Application, version, environment |
| Data entities | e.g. Customer, Order, Invoice, Payment |
| Fields and mapping rules | Source field, destination field, type, required, transformation |
| Trigger conditions | Event, schedule or manual |
| Frequency | Real-time, every N minutes, nightly |
| Transformations | Currency, date format, unit, code lookup, merge/split |
| Authentication | Method per system, token lifetime, who rotates credentials |
| Error handling | Which errors are retried, which go to manual review |
| Retry policy | Attempts, back-off, maximum age |
| Expected response | Status codes and body for every call |
| Idempotency | The key that makes repeats safe |
| Logging and monitoring | What is logged (never secrets), what alerts fire |
| Acceptance criteria | Measurable pass conditions |

**Gate G3:** client signs specification v1.0. Further changes follow the change-request process.

---

## Stage 04: API Feasibility Assessment

**Goal:** prove, with evidence, that the specification can be built.

| Check | Evidence required |
|---|---|
| API documentation | Link and version recorded |
| API availability | Works in the client's licence tier |
| Access permissions | Credentials or app registration approved in writing |
| Authentication | Test token obtained in a sandbox |
| API version | Current version; support end date |
| Rate limits | Documented numbers; compare with expected peak |
| Pagination | Style (offset, cursor, page token) and maximum page size |
| Webhook support | Events available; signature method; retry behaviour |
| Data limits | Payload size, field length, attachment limits |
| Vendor costs | API subscription, per-call fees, partner programme fees |
| Sandbox availability | Test environment exists; data realism |
| Deprecation policy | Notice period for breaking changes |

**If no API exists:** assess **approved alternatives** and report their reliability honestly: vendor-supported file export/import, a vendor-provided integration marketplace, database access *only with the vendor's and client's written consent*, or a vendor professional-services engagement. **Do not bypass access restrictions.**

**Output: API feasibility report** with one verdict per integration: **Feasible**, **Feasible with conditions**, or **Not feasible**.

**Gate G4:** Senior Engineer and Architect sign. Not-feasible items are removed from scope or re-planned.

---

## Stage 05: Architecture Design

**Deliverables:** system context diagram · API architecture · data-flow diagram · authentication flow · database architecture · integration sequence diagram · error-handling flow · monitoring architecture.

**Pattern selection table** (choose one, justify it):

| Requirement | Best-fit pattern | Why |
|---|---|---|
| User waits for the answer; low volume | **Direct API integration** | Simple, immediate, easy to debug |
| Vendor has no webhooks; data can be minutes old | **Scheduled synchronisation** | Predictable load, easy retries |
| Vendor pushes events; fast reaction needed | **Webhook** (with signature verification and quick acknowledgement) | Low latency, low polling cost |
| Bursts, slow destination, must not lose events | **Message queue** (RabbitMQ or cloud queue) | Buffering, retry, dead-letter handling |
| 3+ systems, shared transformations | **Middleware** | One place for mapping, monitoring and security |
| Many consumers need the same events; replay | **Event-driven / streaming** (Kafka) | Decoupling, replay, scale |

**Always decide and document:** the system of record for each entity · conflict rule when both sides edit · idempotency key · retry/dead-letter behaviour · what "eventually consistent" means for this client (how many seconds or minutes of lag are acceptable) · why **distributed transactions are avoided** (use sagas or compensating actions, and say so in writing).

**Gate G5:** Architect and Security sign. Client sees the diagrams.

---

## Stage 06: Cost Estimation

Distinguish **one-time**, **recurring** and **usage-based** costs. Use **official vendor pricing pages**, recorded with date and link. Never quote from memory.

| Cost line | Type | Basis |
|---|---|---|
| Discovery | One-time | Hours × blended rate |
| API development | One-time | Endpoints × complexity |
| Integration development | One-time | Connectors × entities × complexity |
| Database | Recurring | Hosting tier + backup |
| API subscriptions | Recurring / usage | Vendor price sheet |
| Vendor licences | Recurring | Vendor price sheet |
| Cloud infrastructure | Recurring | Calculator from cloud provider |
| Message broker | Recurring | Managed service or self-host effort |
| Monitoring | Recurring | Tool plan or self-host effort |
| Testing and documentation | One-time | Hours |
| Maintenance and support | Recurring | Retainer hours |

**Per-integration cost** = (all one-time costs for that integration) + 12 × (its monthly recurring costs).
**Per-execution cost** = (variable vendor fees + compute + queue + log storage per run).

Worked pricing and margin example: Part 9. **Gate G6:** Founder approves quotation and SOW.

---

## Stage 07: Development Environment Setup

Install and verify: **VS Code · Python 3.12+ or Node.js LTS · Git · GitHub account · Postman (and/or Insomnia) · PostgreSQL · Docker · required packages · API testing tools.** Detailed commands: Part 5.

Maintain **three separated environments**:

| | Development | Staging | Production |
|---|---|---|---|
| Purpose | Build and experiment | Rehearse the release with client sandbox | Real client data |
| Data | Fake / sample only | Anonymised or sandbox | Real |
| Credentials | Personal sandbox | Staging secrets store | Production secrets store, restricted |
| Who may deploy | Developer | CI pipeline | CI pipeline with human approval |

**Gate G7:** each environment starts from a clean checkout using the README alone.

---

## Stage 08: Professional Repository Structure

Use the VEL template. Adapt folder names to the framework, but keep the intent.

```text
api-integration-project/
│
├── README.md                  # What this is, how to run, how to test, who to ask
├── .gitignore                 # Keeps .env, venvs, caches, build output out of Git
├── .env.example               # Names of every variable, with fake values. Real .env never committed
│
├── docs/                      # Everything a new engineer or the client needs to understand the system
│   ├── project-overview.md    # Business problem, scope, stakeholders
│   ├── requirements.md        # Frozen specification and change log
│   ├── api-specification.md   # Human-readable API behaviour (generated reference lives in api-docs/)
│   ├── architecture.md        # Diagrams and the reasons for each choice
│   ├── data-mapping.md        # Field-by-field mapping and transformation rules
│   ├── authentication.md      # How each system authenticates; rotation process (no secrets)
│   ├── error-handling.md      # Error catalogue, retry rules, manual-review queue
│   ├── deployment.md          # Release, rollback, environment variables
│   └── troubleshooting.md     # Symptom → cause → fix, built from real incidents
│
├── src/
│   ├── api/
│   │   ├── routes/            # URL paths and HTTP verbs only; no business logic
│   │   ├── controllers/       # Translate HTTP to service calls and back
│   │   ├── services/          # Business logic; no HTTP concepts
│   │   └── schemas/           # Request/response models and validation
│   │
│   ├── integrations/          # One sub-folder per external system
│   │   ├── crm/               # Client for the CRM: auth, calls, mapping
│   │   ├── erp/
│   │   ├── payments/
│   │   └── email/
│   │
│   ├── authentication/        # Token validation, API keys, OAuth flows, RBAC
│   ├── middleware/            # Request IDs, logging, rate limiting, CORS, error handling
│   ├── database/              # Models, repositories, migrations
│   ├── transformations/       # Pure functions that map one system's data to another's
│   ├── webhooks/              # Inbound receivers, signature checks, replay protection
│   ├── messaging/             # Producers, consumers, retry and dead-letter handling
│   ├── monitoring/            # Metrics, health checks, tracing setup
│   └── utils/                 # Small shared helpers (time, ids, hashing)
│
├── tests/
│   ├── unit/                  # Pure logic: mappings, validators
│   ├── integration/           # Real database/queue, mocked or sandbox vendor
│   ├── contract/              # Checks our calls and responses match the OpenAPI / vendor contract
│   ├── security/              # Authn/authz, injection, rate-limit and tenant-isolation tests
│   └── performance/           # Load and soak tests
│
├── deployment/
│   ├── docker/                # Dockerfile, docker-compose for local and staging
│   ├── nginx/                 # Reverse proxy, TLS, headers, limits
│   └── scripts/               # Release, rollback, backup, restore scripts
│
├── api-docs/
│   └── openapi.yaml           # Machine-readable contract; source for Swagger UI and contract tests
│
├── monitoring/
│   └── alerts/                # Alert rules (error rate, queue depth, auth failures, vendor down)
│
└── .github/
    └── workflows/             # CI (tests, lint, scan, build) and approval-gated CD
```

**Layering rule:** *routes → controllers → services → integrations/database.* A lower layer never imports from a higher layer. Transformations are pure functions with no I/O, so they can be unit-tested exhaustively.

**Never commit** real credentials, tokens, private keys or client data. A pre-commit secret scan and the CI secret scan both run on every push.

---

## Stage 09: API Development

Implement, in this order, for each resource: **schema → test → route → service → persistence → error responses → logging → documentation.**

Must be present: routes · request validation · response schemas · business logic · database operations · authentication · authorisation · consistent error responses · structured logging · API documentation. Follow the company standards in Part 4. Assignment 01 (Part 8) is your first complete example.

---

## Stage 10: System Integration

For every external connector implement: **authentication → extraction → mapping → transformation → validation → synchronisation → webhook handling → retry → idempotency → error queue.**

Handle these as normal conditions:

| Situation | Required behaviour |
|---|---|
| HTTP 429 (rate limited) | Honour `Retry-After`; slow down; never hammer |
| HTTP 5xx / timeout | Retry with exponential back-off and jitter; give up after the policy limit |
| HTTP 4xx (not 429) | **Do not retry blindly**: record, route to manual review |
| Partial batch failure | Process good records; park bad ones with the reason |
| Vendor downtime | Queue and replay on recovery; alert if outage exceeds threshold |
| Duplicate event | Detected by idempotency key; processed once |
| Out-of-order events | Use version/timestamp; ignore stale updates |
| Token expiry | Refresh automatically; alert on repeated refresh failure |

---

## Stage 11: API Security

Apply the **OWASP API Security Top 10 (2023)** as the review checklist:

| Code | Risk | Our standard control |
|---|---|---|
| API1 | Broken Object Level Authorization | Every object access checks the caller's right to *that* object |
| API2 | Broken Authentication | Short-lived tokens, secure key storage, lockout and alert on repeated failure |
| API3 | Broken Object Property Level Authorization | Explicit response schemas; never return whole database rows |
| API4 | Unrestricted Resource Consumption | Rate limits, page-size caps, payload limits, timeouts |
| API5 | Broken Function Level Authorization | Role / scope check on every endpoint, deny by default |
| API6 | Unrestricted Access to Sensitive Business Flows | Throttle and monitor high-value flows (payments, exports) |
| API7 | Server Side Request Forgery | Allow-list outbound hosts; validate user-supplied URLs |
| API8 | Security Misconfiguration | Hardened defaults, no debug in production, strict CORS, security headers |
| API9 | Improper Inventory Management | Maintain the API inventory; retire old versions |
| API10 | Unsafe Consumption of APIs | Validate data from vendors as strictly as user input |

Also required: **HTTPS only · authentication · authorisation · input validation · output filtering · rate limiting · CORS restrictions · secrets management · audit logging · token expiry · least privilege · tenant isolation where applicable.**

---

## Stage 12: Testing and QA

Test **failure scenarios, not just successful calls.** The test report must show a result for each:

| # | Area | Example test |
|---|---|---|
| 1 | API functionality | Each endpoint returns the documented response |
| 2 | Request validation | Missing/invalid fields give 422 with field names |
| 3 | Authentication | No/expired/forged token gives 401 |
| 4 | Authorisation | User A cannot read user B's object (403/404) |
| 5 | Data accuracy | 1,000 sample records map exactly as per the mapping document |
| 6 | Integration functionality | End-to-end source → destination in staging |
| 7 | Duplicate event handling | Same webhook delivered 3× creates 1 record |
| 8 | Retry handling | Destination fails twice then succeeds; one record |
| 9 | Rate limits | Vendor 429 triggers slow-down, no data loss |
| 10 | Vendor downtime | Destination off for 30 minutes; backlog drains on recovery |
| 11 | Performance | Meets agreed p95 latency at expected peak |
| 12 | Security | OWASP checklist, dependency and secret scan clean |
| 13 | Logging | Every request traceable by request ID; no secrets in logs |
| 14 | Recovery | Restore from backup; replay from dead-letter queue |

---

## Stage 13: CI/CD and Deployment

| Step | Content |
|---|---|
| Git workflow | `main` is protected. Work on `feature/*` branches. Pull request + one approval + green CI. Conventional commit messages |
| Automated tests | Unit, integration and contract tests on every push |
| Code quality | Formatter, linter, type check (e.g. `ruff`, `mypy`, or ESLint + `tsc`) |
| Security scans | Dependency vulnerability scan, secret scan, container image scan |
| Docker build | Reproducible, minimal, non-root image tagged with the commit |
| Staging deployment | Automatic on merge to `main` |
| Production release | **Manual approval** by PM/Founder after staging sign-off |
| Rollback | One documented command to redeploy the previous image; database migrations are backward-compatible for one release |

---

## Stage 14: Production Monitoring

Monitor and alert on: **API response times · error rates · request volumes · failed synchronisations · authentication failures · rate-limit responses · queue depth · retry counts · webhook failures · vendor availability.**

| Signal | Example alert |
|---|---|
| Error rate | 5xx above agreed threshold for 5 minutes |
| Latency | p95 above target for 10 minutes |
| Failed syncs | Any record in dead-letter queue older than 1 hour |
| Queue depth | Growing for 15 minutes |
| Auth failures | Spike versus baseline |
| Vendor availability | Health probe failing |

Tools: **Prometheus + Grafana** for metrics, **OpenTelemetry** for traces/metrics/logs instrumentation, a suitable log platform for search. Generate an **incident report** for every Sev-1/Sev-2 (template, Appendix F).

---

## Stage 15: Client Handover

Deliver and obtain a signature on: API documentation · integration architecture · data mapping document · authentication guide · deployment guide · error handling guide · monitoring guide · troubleshooting guide · **test report** · **access register** (who has which access; no secrets) · **maintenance agreement**. Checklist: Appendix G.

---

## Stage 16: Maintenance

Provide: API version upgrades · **vendor API change monitoring** (subscribe to vendor changelogs and deprecation notices) · integration error resolution · security updates · performance optimisation · monitoring · documentation updates · monthly support report. Review SLAs with the client each quarter (Gate G12).
