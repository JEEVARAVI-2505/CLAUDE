# Part 9: Internal Portal, Commercial Pricing and Client Acquisition

## 9.1 Internal management portal: modules

| Module | Purpose | Key fields |
|---|---|---|
| Client management | Accounts, contacts, contracts | Industry, owner, SLA tier |
| API inventory | Section 4.13 register | Version, consumers, lifecycle |
| Integration inventory | Each connector and its systems | Source, destination, frequency |
| Project tracking | Stage and gate status | Stage, gate, risks |
| Employee management | Roles, grade, training level | Gate status, mentor |
| Task assignment | Tickets and time | Assignee, estimate, actual |
| API usage tracking | Calls per client/API | Volume, errors |
| Error monitoring | Failed syncs and exceptions | Count, age, owner |
| Incident tracking | Sev levels and reports | Timeline, root cause |
| Cost tracking | Infrastructure and vendor costs | Per client / integration |
| Billing | Invoices, usage charges | Plan, usage, status |
| Documentation | Links to handover packs | Version, owner |
| Support tickets | Client requests | Priority, SLA clock |
| SLA tracking | Response and resolution | Breach alerts |

<div class="warn" markdown="1">
**The portal never stores secrets.** It stores *references* (for example "stored in vault path X, owner Y, rotates 2027-01-15"), never the secret itself.
</div>

## 9.2 Pricing components

API development fee · integration setup fee · per-system integration fee · monthly monitoring fee · maintenance fee · API usage charges · cloud hosting (passed through or bundled, stated clearly) · third-party vendor expenses (passed through at cost, with the vendor named) · support charges. Add applicable taxes (for example GST in India) as required by current law; confirm rates with your accountant.

## 9.3 What to calculate

| Measure | Formula |
|---|---|
| Development labour | Σ(hours × cost rate) |
| Infrastructure cost | Hosting + database + broker + monitoring per month |
| Third-party cost | Vendor subscriptions + per-call fees |
| Cost per integration | (One-time costs ÷ planned months of life) + monthly recurring costs |
| Cost per transaction | Total monthly cost ÷ monthly transactions |
| Cost per customer | Total monthly cost for the client |
| Gross margin | (Revenue − direct cost) ÷ Revenue |
| Contribution margin | (Revenue − variable cost) ÷ Revenue |
| Break-even | Fixed monthly company cost ÷ average monthly contribution per client |
| Recurring revenue | Monitoring + maintenance + base usage fees per month |

## 9.4 Usage-based pricing model

Use a **base fee including an allowance**, then a **per-transaction charge** above it, so revenue rises when vendor and compute costs rise.

**Worked example (illustrative numbers only: replace with verified costs, Stage 06):**

| Item | Value |
|---|---|
| Monthly fixed cost for this client (hosting, monitoring, share of support) | ₹10,000 |
| Variable cost per transaction (compute, queue, vendor call, logs) | ₹0.40 |
| Monthly transactions | 50,000 |
| Base fee (includes 20,000 transactions) | ₹25,000 |
| Overage price | ₹1.20 per transaction |

| Result | Calculation | Value |
|---|---|---|
| Variable cost | 50,000 × 0.40 | ₹20,000 |
| Total cost | 10,000 + 20,000 | ₹30,000 |
| Cost per transaction | 30,000 ÷ 50,000 | ₹0.60 |
| Revenue | 25,000 + (30,000 × 1.20) | ₹61,000 |
| Gross margin | (61,000 − 30,000) ÷ 61,000 | 50.8% |
| Contribution margin | (61,000 − 20,000) ÷ 61,000 | 67.2% |
| Break-even (illustrative company overhead ₹3,00,000 per month) | 3,00,000 ÷ 41,000 contribution per client | 7.3, so **8 such clients** |

## 9.5 Target clients

SaaS businesses · e-commerce · healthcare · manufacturing · logistics · financial services · ERP and CRM users · any business running multiple disconnected applications. **Regulated sectors** (healthcare, finance) need extra compliance review at Stage 02; do not accept those projects without Security and Founder approval.

**Offer statement:** "We connect your existing business applications so your teams can exchange data automatically, reduce manual entry and maintain consistent records."

## 9.6 Sales document set

| Document | Stage | Owner |
|---|---|---|
| Discovery questionnaire | 02 | Architect |
| API feasibility report | 04 | Sr. Engineer |
| Integration proposal | 05 to 06 | PM |
| Architecture diagram | 05 | Architect |
| Commercial quotation | 06 | PM, approved by Founder |
| Statement of Work (SOW) | 06 | PM / Founder |
| Demo | 12 to 13 | Engineer |
| Test report | 12 | QA |
| Handover checklist | 15 | PM |

## 9.7 The ten practical projects

| # | Project | Skills proven | Week |
|---|---|---|---|
| 1 | Basic REST API | REST, validation, errors, tests | 4 |
| 2 | API connected to PostgreSQL | SQL, migrations, pooling | 6 |
| 3 | API authentication | API keys, OAuth/JWT, RBAC | 7 |
| 4 | Integrate two sample applications | Mapping, transformation, sync | 8 to 9 |
| 5 | Webhook receiver | Signatures, fast ack, replay defence | 10 |
| 6 | Scheduled synchronisation service | Cursors, pagination, scheduling | 11 |
| 7 | Retry and idempotency | Back-off, dedupe, dead-letter | 11 |
| 8 | API monitoring dashboard | Prometheus, Grafana, alerts | 12 |
| 9 | Containerise the integration | Docker, compose, health checks | 12 |
| 10 | Full simulated enterprise integration | Stages 03 to 15 end-to-end | 13 |

## 9.8 Official learning sources

| Topic | Source |
|---|---|
| Web fundamentals | MDN Web Docs: developer.mozilla.org |
| OpenAPI | openapis.org |
| FastAPI | fastapi.tiangolo.com |
| Django REST Framework | django-rest-framework.org |
| Node.js | nodejs.org/docs |
| PostgreSQL | postgresql.org/docs |
| Postman | learning.postman.com |
| Docker | docs.docker.com |
| OWASP API Security | owasp.org/www-project-api-security |
| RabbitMQ | rabbitmq.com/docs |
| Apache Kafka | kafka.apache.org/documentation |
| OpenTelemetry | opentelemetry.io/docs |
