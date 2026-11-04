# Part 1: The Department: Mission, Services and Boundaries

## 1.1 Mission

> We connect your existing business applications so your teams can exchange data automatically, reduce manual entry and maintain consistent records.

The department exists because most businesses already own good software, but that software does not talk to each other. People re-type data, spreadsheets drift out of date and reports disagree. Our work is to make systems **exchange data reliably, securely and observably**.

## 1.2 Service portfolio

| Line | Services | Typical client outcome |
|---|---|---|
| **A. API development** | REST APIs, GraphQL APIs, webhooks, internal / public / partner APIs, documentation, versioning, maintenance | A product or service exposes clean, documented, secure interfaces |
| **B. System integration** | CRM, ERP, HRMS, accounting, payment gateway, e-commerce, inventory, email and communication, cloud and third-party SaaS integration | Two or more business systems share data without manual re-entry |
| **C. Integration automation** | Data synchronisation, automated transfer, workflow and event-driven integration, scheduled jobs, document transfer, cross-platform automation | Repetitive hand-offs run on their own, with an audit trail |
| **D. Enterprise integration** | API gateways, middleware, message queues, event streaming, microservices communication, enterprise integration architecture, legacy integration | Many systems communicate through a governed, scalable backbone |
| **E. Managed API services** | Monitoring, integration maintenance, error monitoring, performance tuning, security reviews, version management, support and incident handling | The integration keeps working after go-live, and the client has one number to call |

## 1.3 What we do and what we do not do

| We do | We do not |
|---|---|
| Verify that a vendor offers an API and that the client's licence allows our use | Assume an API exists or "try it and see" on a client's production tenant |
| Use official, documented, versioned vendor APIs | Scrape pages behind a login, defeat captchas or bypass rate limits |
| Store credentials in environment variables and secrets managers | Keep secrets in code, tickets, chats or the management portal |
| Offer approved alternatives when no API exists (file export, database read replica with consent, vendor professional services, RPA only if the vendor terms permit) | Promise a delivery date before feasibility (Stage 04) is signed off |
| Document every field mapping and failure behaviour | Ship an integration without monitoring and a runbook |
| State assumptions and risks in writing | Claim a guarantee the vendor's API cannot support |

## 1.4 The "simplest thing that works" principle

New engineers over-build. Use this ladder and stop at the **first** rung that satisfies the written requirement:

| Rung | Pattern | Use when | Typical tools |
|---|---|---|---|
| 1 | Direct API call (request/response) | Two systems, low volume, user waits for the answer | `requests` / `httpx`, vendor SDK |
| 2 | Scheduled synchronisation | Data may be minutes or hours old; vendor has no webhooks | Cron, scheduler, n8n schedule trigger |
| 3 | Webhook receiver | Vendor pushes events; you must react quickly | FastAPI / Express endpoint plus signature check |
| 4 | Queue between producer and consumer | Bursts, slow consumers, need retry and back-pressure | RabbitMQ, cloud queues (SQS, Service Bus, Pub/Sub) |
| 5 | Middleware / integration service | Three or more systems, shared transformations, central monitoring | Own service, n8n, Make where suitable |
| 6 | Event streaming | Very high volume, multiple independent consumers, replay needed | Kafka |
| 7 | API gateway in front of everything | Many consumers need uniform auth, rate limits, analytics | Kong, cloud-native gateways |

<div class="warn" markdown="1">
**Rule:** write the justification for moving up a rung in the architecture document (Stage 05). "It is more modern" is not a justification. A missed requirement (for example, "must survive 30 minutes of vendor downtime without losing events") is.
</div>

## 1.5 Reference architecture (what a typical VEL integration looks like)

<figure class="diagram">
<svg viewBox="0 0 900 360" role="img" aria-label="Reference integration architecture" xmlns="http://www.w3.org/2000/svg" font-family="Inter, Arial, sans-serif">
  <defs><marker id="ar" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="#1b2f6b"/></marker></defs>
  <rect x="10" y="130" width="140" height="90" rx="10" fill="#eef2fb" stroke="#1b2f6b"/>
  <text x="80" y="170" text-anchor="middle" font-size="15" font-weight="700" fill="#1b2f6b">Source system</text>
  <text x="80" y="192" text-anchor="middle" font-size="12" fill="#444">CRM / ERP / Shop</text>
  <rect x="220" y="40" width="130" height="60" rx="10" fill="#fff" stroke="#b01e5c"/>
  <text x="285" y="66" text-anchor="middle" font-size="13" font-weight="700" fill="#b01e5c">Webhook receiver</text>
  <text x="285" y="85" text-anchor="middle" font-size="11" fill="#444">verify signature</text>
  <rect x="220" y="250" width="130" height="60" rx="10" fill="#fff" stroke="#b01e5c"/>
  <text x="285" y="276" text-anchor="middle" font-size="13" font-weight="700" fill="#b01e5c">Scheduled puller</text>
  <text x="285" y="295" text-anchor="middle" font-size="11" fill="#444">cursor / pagination</text>
  <rect x="410" y="140" width="110" height="70" rx="10" fill="#fff" stroke="#1b2f6b"/>
  <text x="465" y="170" text-anchor="middle" font-size="14" font-weight="700" fill="#1b2f6b">Queue</text>
  <text x="465" y="190" text-anchor="middle" font-size="11" fill="#444">retry + DLQ</text>
  <rect x="580" y="110" width="150" height="130" rx="10" fill="#eef2fb" stroke="#1b2f6b"/>
  <text x="655" y="140" text-anchor="middle" font-size="14" font-weight="700" fill="#1b2f6b">Integration worker</text>
  <text x="655" y="162" text-anchor="middle" font-size="11" fill="#444">validate · map · transform</text>
  <text x="655" y="180" text-anchor="middle" font-size="11" fill="#444">idempotency check</text>
  <text x="655" y="198" text-anchor="middle" font-size="11" fill="#444">rate-limit aware calls</text>
  <rect x="770" y="130" width="120" height="90" rx="10" fill="#eef2fb" stroke="#1b2f6b"/>
  <text x="830" y="170" text-anchor="middle" font-size="15" font-weight="700" fill="#1b2f6b">Destination</text>
  <text x="830" y="192" text-anchor="middle" font-size="12" fill="#444">Accounting / ERP</text>
  <rect x="580" y="285" width="150" height="55" rx="10" fill="#fff" stroke="#777"/>
  <text x="655" y="310" text-anchor="middle" font-size="13" font-weight="700" fill="#333">PostgreSQL</text>
  <text x="655" y="327" text-anchor="middle" font-size="11" fill="#444">state · audit · mappings</text>
  <rect x="380" y="285" width="160" height="55" rx="10" fill="#fff" stroke="#777"/>
  <text x="460" y="310" text-anchor="middle" font-size="13" font-weight="700" fill="#333">Monitoring</text>
  <text x="460" y="327" text-anchor="middle" font-size="11" fill="#444">metrics · logs · alerts</text>
  <g stroke="#1b2f6b" stroke-width="2" fill="none" marker-end="url(#ar)">
    <path d="M150 160 L220 80"/><path d="M150 195 L220 270"/>
    <path d="M350 70 L410 160"/><path d="M350 280 L410 195"/>
    <path d="M520 175 L580 175"/><path d="M730 175 L770 175"/>
  </g>
  <g stroke="#777" stroke-width="1.5" stroke-dasharray="5 4" fill="none" marker-end="url(#ar)">
    <path d="M655 240 L655 285"/><path d="M600 240 L540 300"/>
  </g>
</svg>
<figcaption>Figure 1: Reference integration architecture. Use only the parts the requirement needs.</figcaption>
</figure>

## 1.6 Success measures for the department

| Measure | Target (to be confirmed by the Founder) |
|---|---|
| Integrations delivered with a signed test report | 100% |
| Production secrets found in Git | 0 |
| Integrations with monitoring and a runbook at go-live | 100% |
| Duplicate-record incidents caused by retries | 0 |
| Critical incident acknowledgement | Per the SLA in the client's maintenance agreement |
| Trainee reaching supervised-project status within 90 days | Tracked; reasons recorded if not met |
