# Part 7: The Nine-Level Curriculum

Each level lists what you learn, why it matters to the business, the lab you do, and how you are assessed. Every lesson is delivered in the **16-point teaching format** (section 7.10). Always read the official source named for the level.

## Level 1: Software Fundamentals

| | |
|---|---|
| **Topics** | What is software · client-server architecture · frontend and backend · database · server · application · request and response · authentication · data exchange · JSON and XML |
| **Business purpose** | You cannot integrate systems you cannot describe. Clients explain problems in business words; you translate them to systems and data. |
| **Real-world example** | A shop website (client) asks the order server for today's orders; the server reads the database and replies with JSON. |
| **Lab** | Draw the client, server and database of an online shop. Open browser dev tools and find three requests and their JSON responses. |
| **Official source** | MDN Web Docs: *How the web works* |
| **Assessment** | 20-question quiz (≥ 80%); one-page diagram explained aloud |

## Level 2: Networking and Web Fundamentals

| | |
|---|---|
| **Topics** | HTTP · HTTPS · DNS · IP addresses · ports · methods · status codes · headers · cookies · TLS · CORS · reverse proxies |
| **Business purpose** | Most production integration faults are network, certificate, header or timeout faults. |
| **Real-world example** | A webhook fails because the client's firewall blocks port 443 from our server's IP. |
| **Lab** | `curl -v https://example.com`; read the TLS handshake, headers and status; resolve a name with `nslookup`; trigger and fix a CORS error in a test page. |
| **Common errors** | Mixing up 401 and 403 · certificate name mismatch · wrong port · missing `Content-Type` |
| **Official source** | MDN HTTP guide |
| **Assessment** | Practical: diagnose three broken requests from a worksheet |

## Level 3: Programming (Python first, TypeScript later)

| | |
|---|---|
| **Topics** | Variables · functions · data structures (list, dict, set, tuple) · error handling · async programming · JSON processing · environment variables · logging (Python first; JavaScript/TypeScript introduced only when a project requires it) |
| **Business purpose** | All connectors are code that transforms data and handles failure. |
| **Lab** | Read a JSON file of 1,000 orders, validate fields, write a cleaned file and a rejects file with reasons; read configuration only from environment variables; log with levels. |
| **Common errors** | Mutable default arguments · swallowing exceptions · hard-coded secrets · printing instead of logging |
| **Official source** | docs.python.org tutorial |
| **Assessment** | 20 graded exercises plus one code review |

## Level 4: API Fundamentals

| | |
|---|---|
| **Topics** | What an API is · how APIs work · REST · GET · POST · PUT · PATCH · DELETE · request headers · request body · response body · status codes · query and path parameters · pagination · filtering · sorting · rate limits |
| **Business purpose** | These are the words clients and vendors use in every document. |
| **Lab** | Build the Postman collection for a public API; then **Assignment 01** (Part 8). |
| **Key idea: safe and idempotent methods** | `GET` is safe. `GET`, `PUT`, `DELETE` are idempotent (repeating changes nothing more). `POST` is not idempotent by default. `PATCH` depends on the design. |
| **Official source** | MDN HTTP methods; OpenAPI Initiative; FastAPI tutorial |
| **Assessment** | Assignment 01 and Gate 1 |

## Level 5: API Authentication and Security

| | |
|---|---|
| **Topics** | API keys · OAuth 2.0 · OpenID Connect · JWT · sessions · RBAC · scopes · token expiry · refresh tokens · secrets management · TLS · input validation · rate limiting |
| **Business purpose** | A leaked key or a missing authorisation check is a client-losing incident. |
| **Selection guide** | See section 4.3. Rule of thumb: API key for one trusted server; OAuth 2.0 client credentials for machine-to-machine with scopes; authorisation code + PKCE with OIDC when a human user's identity is needed. |
| **Security implications** | API keys: long-lived, easy to leak, no user identity. JWT: stateless but hard to revoke; keep lifetime short and validate signature, issuer, audience and expiry. Sessions: need CSRF protection. Refresh tokens: store encrypted, rotate on use. |
| **Lab** | Add JWT login and role checks to the Client Registry API; write tests for 401, 403 and cross-user access (API1). |
| **Official source** | OWASP API Security Top 10; OAuth 2.0 (RFC 6749); JWT (RFC 7519) |
| **Assessment** | OWASP oral check; security tests green |

## Level 6: Backend Frameworks

| | |
|---|---|
| **Topics** | **FastAPI** (primary) · Django REST Framework · Node.js · Express.js · NestJS · Spring Boot only where a client's stack requires it |
| **Business purpose** | Choose the framework the client's team can maintain, not the one you like. |
| **Selection guide** | FastAPI: new Python APIs and integration services, automatic OpenAPI. Django REST: admin UI and ORM-heavy back-office. Express/NestJS: client has a TypeScript team. Spring Boot: client mandates Java. |
| **Lab** | Rebuild the same two endpoints in FastAPI and NestJS; compare code size, validation and docs. |
| **Official source** | fastapi.tiangolo.com · django-rest-framework.org · nodejs.org/docs |

## Level 7: Databases

| | |
|---|---|
| **Topics** | PostgreSQL · MySQL · MongoDB where justified · SQL · relationships · transactions · indexes · migrations · connection pooling · consistency · backup and recovery |
| **Business purpose** | Integrations need durable state: what was synced, what failed, what was already processed. |
| **Lab** | Model clients, contacts and a sync-log table; write a migration; add a unique index for idempotency; back up and restore. |
| **Common errors** | Missing index on lookup column · long transactions · forgetting to test restore · storing secrets in tables |
| **Official source** | postgresql.org/docs |

## Level 8: Integration Architecture

| | |
|---|---|
| **Topics** | Point-to-point · middleware · API gateway · webhooks · message queues · event-driven architecture · brokers · retry · idempotency · dead-letter queues · eventual consistency · distributed transactions and their limits |
| **Business purpose** | This is what makes integrations survive real life. |
| **Key ideas** | **At-least-once delivery** means duplicates will happen, so consumers must be idempotent. **Dead-letter queue**: messages that exhaust retries are parked for human review, not dropped. **Eventual consistency**: both systems agree *after a short delay*; agree the delay with the client. **Distributed transactions** (two-phase commit across vendors) are rarely possible; use sagas with compensating actions instead. |

Tested reference code (run it with `python retry_idempotent.py`; tests in `snippets/`):

{{file:snippets/retry_idempotent.py|python}}

| | |
|---|---|
| **Lab** | Projects 5, 6 and 7 |
| **Official source** | rabbitmq.com/docs · kafka.apache.org/documentation |
| **Assessment** | Failure-scenario test report |

## Level 9: API Documentation and Testing

| | |
|---|---|
| **Topics** | OpenAPI · Swagger · Postman · automated API tests · contract tests · integration tests · load tests · mock servers · versioning |
| **Business purpose** | Documentation and tests are what the client pays to keep; they also let the next engineer work safely. |
| **Lab** | Export OpenAPI from your service; run it as a contract in CI; mock a vendor API; run a load test and record p95 latency. |
| **Official source** | openapis.org · learning.postman.com · opentelemetry.io/docs |

## 7.10 The 16-point teaching format

Every lesson and assignment in this programme contains all sixteen parts. Trainers must not drop any.

| # | Part | # | Part |
|---|---|---|---|
| 1 | Definition | 9 | Complete code examples |
| 2 | Business purpose | 10 | Expected output |
| 3 | Real-world example | 11 | Verification |
| 4 | Prerequisites | 12 | Common errors |
| 5 | Required tools | 13 | Security requirements |
| 6 | Installation | 14 | Documentation |
| 7 | Configuration | 15 | Practical assignment |
| 8 | Step-by-step implementation | 16 | Assessment |

## 7.11 Technology stack (what we standardise on)

| Category | Tools |
|---|---|
| IDE | VS Code |
| Languages | Python (primary), TypeScript |
| Frameworks | FastAPI, Django, NestJS |
| Database | PostgreSQL, MySQL |
| API testing | Postman, Insomnia |
| Documentation | OpenAPI, Swagger |
| Version control | Git, GitHub |
| Containers | Docker |
| Messaging | RabbitMQ, Kafka |
| Automation | n8n, Make where suitable |
| API gateway | Kong, cloud-native gateways |
| Monitoring | Prometheus, Grafana |
| Logging | OpenTelemetry, a suitable log platform |
| Cloud | AWS, Azure, GCP |
| Deployment | Docker, CI/CD |

Start with **one** primary backend language. Introduce others only when project requirements justify them.
