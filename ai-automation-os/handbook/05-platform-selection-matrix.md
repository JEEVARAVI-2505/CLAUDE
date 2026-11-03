# Part 5 — Technology and Platform Selection Matrix

**Evidence status:** ratings are the author's qualitative hypotheses from general knowledge (no live research was possible). Replace each with evidence from the vendor's current documentation and your own trial, and record the date. **Verify the licence of any platform before using it in a customer-facing or resold solution.**

## 5.1 How to compare

Score each option 1–5 on the ten criteria (**5 = best for VEL**). Weight the criteria for the project (a regulated customer weights security and deployment higher; a pilot weights cost and ease higher). Totals below are **unweighted** and are only a starting point, never the decision.

| # | Criterion | 5 means |
|---|---|---|
| 1 | Cost | Low cost at 10× today's volume |
| 2 | Licensing | Clear, permits our commercial and resale use |
| 3 | Ease of development | Junior staff productive quickly |
| 4 | Integration availability | Ready connectors for our target systems |
| 5 | Scalability | Handles growth without redesign |
| 6 | Security | Strong control of data, access and secrets |
| 7 | Maintenance | Low ongoing effort |
| 8 | Workflow execution limits | Few or no restrictive limits |
| 9 | Deployment flexibility | Cloud, self-host, on-premise all possible |
| 10 | Vendor lock-in | Easy to leave (5 = low lock-in) |

## 5.2 Workflow platforms

| Platform | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | Total /50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| n8n | 4 | 3 | 4 | 4 | 4 | 4 | 3 | 4 | 5 | 4 | **39** |
| Temporal (code-based) | 4 | 5 | 2 | 2 | 5 | 4 | 2 | 5 | 5 | 4 | **38** |
| Activepieces | 4 | 4 | 3 | 3 | 3 | 4 | 3 | 4 | 5 | 4 | **37** |
| Make | 3 | 2 | 5 | 4 | 3 | 3 | 4 | 3 | 2 | 2 | **31** |
| Microsoft Power Automate | 3 | 2 | 4 | 4 | 4 | 4 | 3 | 3 | 2 | 2 | **31** |
| Zapier | 2 | 2 | 5 | 5 | 3 | 3 | 4 | 2 | 1 | 2 | **29** |

**Reading the table**

- **Temporal** is a developer framework, not a visual builder; it scores high on durability and low on speed of build. Use it for long-running processes with strict retry semantics, not simple flows.
- **n8n** is our default for custom builds. Its licence is *source-available* with conditions; **read the current licence text and get legal advice before offering it as a hosted service to customers.**
- **Make, Zapier, Power Automate** are strong when the **customer already owns** them and data is low-risk; execution/usage limits and per-use pricing need checking at the customer's volume.
- **Activepieces** has an open-source core; check which edition and licence you would deploy, and connector coverage for your target apps.

**Decision procedure:** (1) list must-haves (self-hosting? data location? specific connectors?); (2) remove options that fail a must-have; (3) weight the rest; (4) build a one-day proof of concept of the riskiest step in the top two; (5) record the decision in `decisions.md`.

## 5.3 Programming languages and frameworks

| Option | Strengths | Weaknesses | Use for |
|---|---|---|---|
| Python | Largest AI/data ecosystem, readable, strong testing tools | Slower than compiled languages; packaging care needed | Core services, AI steps, data processing |
| FastAPI (Python) | Fast to build typed APIs, automatic documentation, validation | Needs async care | The service layer (see `reference-project/`) |
| TypeScript | Typed JavaScript; shares code with front ends | Smaller AI/data ecosystem | Dashboards, front ends, some services |
| Node.js | Good for I/O-heavy services and tooling | Callback/async complexity | Webhook receivers, tooling |
| JavaScript | Used inside workflow tools' code nodes | No types | Small snippets only |
| C# (where appropriate) | Strong in .NET estates | Smaller AI-tool ecosystem | When a customer runs a .NET environment |

**Default:** Python + FastAPI for services, TypeScript for dashboards.

## 5.4 Databases and storage

| Option | Choose when | Avoid when |
|---|---|---|
| PostgreSQL | Default: relational data, audit logs, JSON fields, vector search with pgvector | — |
| MySQL | Customer mandates it; simple relational needs | You need richer JSON/extensions |
| MongoDB | Truly document-shaped, varied data | Data is relational and needs strict integrity |
| Redis | Cache, rate limits, queues, short-lived state | As the only store of important records |
| SQLite | Local development and tests | n8n or any multi-user production |
| Object storage | Files and attachments, encrypted, with retention rules | Structured queries |
| Dedicated vector database | Very large retrieval workloads that pgvector cannot serve | Small knowledge bases (use pgvector) |

## 5.5 Integration technologies

| Technology | Use | Rule |
|---|---|---|
| REST API | Default way to talk to services | Use official APIs; handle timeouts, rate limits, errors |
| Webhooks | Receive events | Verify signatures; reject replays; respond fast, process in a queue |
| OAuth 2.0 | Delegated access to customer accounts | Minimum scopes; secure token storage; plan renewal |
| Message queues | Decouple and absorb bursts | Dead-letter queue; idempotent consumers |
| Email APIs | Send/receive email | Approval for sensitive messages; deliverability setup |
| WhatsApp Business Platform | Official messaging | Consent, templates, current policy |
| CRM and ERP APIs | Customer and finance data | Sandbox first; least privilege |
| MQTT | Device and sensor data | Only for IoT-style projects |

## 5.6 Architecture selection (recommend one per project)

| Pattern | Pick when | Cost and risk notes |
|---|---|---|
| Simple workflow automation | Rules explicit, few steps | Cheapest; brittle if rules change |
| API integration automation | Systems have good APIs | Watch rate limits and credential renewal |
| Event-driven | Many triggers, bursts | Needs queues, idempotency |
| AI-assisted workflow | One unstructured step | Token and review cost |
| RAG application | Answers from private documents | Embedding/storage cost; stale content risk |
| Tool-using agent | Path depends on findings; tools bounded | Variable cost; needs strict limits |
| Multi-agent | Separate responsibilities and permissions | Complexity; justify with evidence |
| Hybrid RPA + AI | No API; stable screens; permitted | Maintenance when screens change |

Write the justification, limitations, estimated cost and operational risks for the chosen pattern in `architecture.md` (Stage S05).

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
