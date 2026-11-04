# Appendices: Templates and Reference

## Appendix A: Lead qualification report

| Field | Entry |
|---|---|
| Client / industry | |
| Contact and decision-maker | |
| Systems to connect | |
| Business problem and cost today | |
| Applications (count) | |
| Data volume (per day / peak) | |
| Required automation | |
| Budget range / approver | |
| Timeline and reason | |
| Score (Red / Amber / Green): problem · decision-maker · API likelihood · budget · timeline | |
| Recommendation | Go to discovery / Decline / Nurture |

## Appendix B: Discovery questionnaire

1. Which applications must exchange data? Name, edition, version, hosting.
2. Does each have a documented API? Which plan or licence includes it? Who is the vendor contact?
3. Which data entities move, and in which direction? Which system is the system of record?
4. How is data moved today? By whom, how often, with what error rate?
5. What triggers a transfer: event, schedule, manual approval?
6. How many records per day, per peak hour? Growth expected in 12 months?
7. Authentication each system supports. Who can issue credentials? Is there a sandbox?
8. Known rate limits, maintenance windows, data-size limits.
9. What must happen if the destination is down for 1 hour? 1 day?
10. Which data is personal, financial or health data? Residency and retention rules?
11. Who approves production access? Who will support after go-live?
12. What does success look like, in numbers?

## Appendix C: Integration specification template

| Section | To complete |
|---|---|
| 1. Purpose and scope | |
| 2. Systems, versions, environments | |
| 3. Entities and system of record | |
| 4. Field mapping table (source, destination, type, required, transformation, example) | |
| 5. Triggers and frequency | |
| 6. Authentication per system | |
| 7. Error handling and retry policy | |
| 8. Idempotency key | |
| 9. Logging and monitoring | |
| 10. Non-functional: volume, latency, availability | |
| 11. Acceptance criteria (measurable) | |
| 12. Assumptions, risks, out of scope | |
| 13. Change log and signatures | |

## Appendix D: Weekly report (every Friday)

| Done this week | Learned | Blocked / needs help | Next week | Hours | Risks |
|---|---|---|---|---|---|
| | | | | | |

## Appendix E: Pull request checklist

☐ Linked ticket · ☐ Tests added and passing · ☐ No secrets or client data · ☐ Docs / OpenAPI updated · ☐ Errors use the standard shape · ☐ Logging contains no sensitive data · ☐ Migration is reversible · ☐ Reviewer assigned

## Appendix F: Incident report template

| Field | Entry |
|---|---|
| Incident ID, severity (Sev-1 critical · Sev-2 major · Sev-3 minor) | |
| Detected (time, how) | |
| Impact (clients, records, duration) | |
| Timeline | |
| Root cause | |
| Fix and recovery steps | |
| Data affected and how it was reconciled | |
| Prevention actions (owner, date) | |
| Client communication sent | |

## Appendix G: Client handover checklist

☐ API documentation · ☐ Integration architecture · ☐ Data-mapping document · ☐ Authentication guide · ☐ Deployment guide · ☐ Error handling guide · ☐ Monitoring guide · ☐ Troubleshooting guide · ☐ Test report · ☐ Access register (no secrets) · ☐ Maintenance agreement · ☐ Credential rotation dates · ☐ Training session held · ☐ Signed acceptance

## Appendix H: Glossary

| Term | Meaning |
|---|---|
| API | Defined way for programs to talk to each other |
| REST | Style of API using HTTP methods on resources |
| Webhook | A push notification: the vendor calls *your* URL when an event happens |
| Idempotent | Repeating the operation has the same effect as doing it once |
| Dead-letter queue | Holding area for messages that failed all retries |
| Eventual consistency | Systems agree after a short delay, not instantly |
| Rate limit | Maximum calls allowed in a time window |
| OAuth 2.0 | Standard for delegated, scoped access with tokens |
| JWT | Signed token carrying claims, with an expiry |
| RBAC | Access by role |
| OpenAPI | Machine-readable description of an API |
| SLA | Agreed response and resolution times |
| Sandbox | Vendor test environment with no real data |

## Appendix I: Coverage map: your master prompt vs this volume

| Master-prompt section | Where covered |
|---|---|
| 1 Role | Parts 0 to 2 |
| 2 Service portfolio | 1.2 |
| 3 Training roadmap (Levels 1 to 9) | Part 7, Part 6 |
| 4 Technology stack | 7.11, Part 1.4 |
| 5 Stages 01 to 16 | Part 3 |
| 6 Governance | Part 4 |
| 7 Internal portal | 9.1 |
| 8 Commercial pricing | 9.2 to 9.4 |
| 9 Client acquisition | 9.5, 9.6 |
| 10 Ten projects | 9.7, 6.2 |
| 11 Learning sources | 9.8 |
| 12 Teaching format | 7.10, applied in Part 8 |
| 13 First action: handbook, workflow, onboarding, 90-day plan, responsibilities, first assignment | Parts 0 to 6 and Part 8 |

## Appendix J: Known limits of this volume

- Price figures are **illustrative**. Verify vendor prices from official pricing pages and record the date.
- Legal, tax and data-protection requirements differ by client and country. Confirm with qualified advisers before signing contracts.
- Vendor APIs change. Re-verify documentation at Stage 04 and during maintenance.
- Full lesson code for Levels 1 to 3 and Projects 2 to 10 is released in later modules (8.17).

*End of Module 1.*
