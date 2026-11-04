# VEL API & System Integration Handbook

**API Engineering and Enterprise System Integration Department: from Beginner to Supervised Commercial Delivery**

| Document control | |
|---|---|
| Document ID | VEL-API-001 |
| Version | 1.0 (draft for management approval) |
| Owner | V. LOKESHWARAN, Founder |
| Audience | New engineers, trainees, analysts and support staff with basic computer knowledge |
| Companion document | VEL Delivery Handbook (VEL-DH-001), the company-wide project procedure |
| Review cycle | Every quarter, after any production incident, and after any failed project |
| Contact | velsoftwaresolutions.in@gmail.com |

## 0.1 What this handbook is

This is the single reference for the **API & System Integration Department** of VEL Software Solutions. It does three jobs:

1. **Defines the department**: what we sell, who does what, and the standards every API and integration must meet.
2. **Defines the delivery workflow**: sixteen stages that take a client from first enquiry to a monitored, maintained, handed-over integration.
3. **Trains the employee**: a 90-day programme that takes a beginner to a person who can join a *supervised* commercial project.

It is written in plain words. You do not need to know everything before you start. You need to **follow the stages in order, finish every checklist and collect every sign-off**.

<div class="note" markdown="1">
**This is Module 1 of a progressive series.** This volume gives you the handbook, the corporate workflow, onboarding, the 90-day roadmap, responsibilities and your first practical API assignment. Later modules (one per curriculum level) are released one at a time as the trainee completes each gate. See section 8.17 for the module release plan.
</div>

## 0.2 How to use it

1. Find your current week in the **90-day roadmap** (Part 6).
2. Read that week's lesson material and the linked official source **before** you write code.
3. Do the numbered steps in order. Tick the **Done checklist** as you go.
4. Save every deliverable in the exact folder named in the task.
5. Ask for the **gate sign-off**. You may not start the next phase without it.
6. Write a short weekly report every Friday (template in Appendix D).

## 0.3 The twelve golden rules

1. **Never skip a stage or a gate.** Skipping is how integrations fail in production.
2. **Verify, never assume.** Do not assume a vendor has an API, allows your use, or will not change it. Check documentation, permissions, licences and limits.
3. **Choose the simplest architecture that works.** A scheduled script beats a message broker if the requirement is one nightly sync.
4. **Never bypass access restrictions.** If no approved API exists, we propose approved alternatives. We do not scrape behind logins or defeat protections.
5. **Never put a secret or client data into Git.** Not once, not "temporarily". Use `.env` locally and a secrets manager in staging and production.
6. **Test the failure, not only the success.** Duplicate events, timeouts, rate limits and vendor downtime are normal events, not rare ones.
7. **Every integration is idempotent.** Running the same message twice must not create two invoices.
8. **No test, no merge.** Code without an automated test does not enter the main branch.
9. **Write it down.** If it is not in a document or a ticket, it did not happen.
10. **Least privilege.** Request the smallest access scope that does the job.
11. **Ask early.** Being stuck for 30 minutes without asking is a mistake, not a virtue.
12. **Client data belongs to the client.** Treat it as if it were your own bank details.

## 0.4 Department map at a glance

<div class="flow" markdown="1">
<span>01 Lead</span><span>02 Discovery</span><span>03 Requirements freeze</span><span>04 Feasibility</span><span>05 Architecture</span><span>06 Costing</span><span>07 Environment</span><span>08 Repository</span><span>09 API build</span><span>10 Integration</span><span>11 Security</span><span>12 Testing</span><span>13 CI/CD deploy</span><span>14 Monitoring</span><span>15 Handover</span><span>16 Maintenance</span>
</div>

Stages 01 to 06 are **pre-sales and design** (no code is written for the client). Stages 07 to 13 are **build and release**. Stages 14 to 16 are **run and support**, which is where recurring revenue and client trust are earned.

## 0.5 Who is who (plain words)

| Role | What they really do | You ask them when |
|---|---|---|
| Founder | Decides what we sell, signs contracts and prices | You need a decision about money, scope or clients |
| Project Manager (PM) | Plans the work, tracks time, speaks to the client daily | You need priorities, deadlines or client contacts |
| API Architect | Chooses the architecture and reviews designs | You are unsure which integration pattern to use |
| Senior Integration Engineer | Reviews your code, mentors you, owns hard problems | You are stuck technically or need a code review |
| Integration Engineer / Developer | Builds APIs, connectors and transformations | Day-to-day coding questions |
| Database Engineer | Designs schemas, migrations, backup and recovery | You need a table, an index or a data model |
| Security Engineer | Reviews authentication, secrets and OWASP risks | Anything touching tokens, keys, tenants or personal data |
| QA Engineer | Tries to break the software before the client does | You want to know how to test something |
| DevOps Engineer | Pipelines, containers, servers, monitoring, alerts | Deployment, environments, backups, dashboards |
| Business Analyst | Writes down how the client's process really works | You do not understand the client's workflow |
| Training Manager | Runs onboarding, assessments and gate reviews | Your learning plan, assessments, mentor allocation |

*In a small startup one person may hold several roles. The responsibilities still exist and must still be covered.*
