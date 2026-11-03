# VEL Software Solutions — Delivery Handbook

**AI Automation Projects: from Day 0 to 100% Delivery**

| Document control | |
|---|---|
| Document ID | VEL-DH-001 |
| Version | 1.0 (draft for management approval) |
| Owner | V. LOKESHWARAN, Founder |
| Audience | New engineers, analysts and support staff with basic knowledge |
| Review cycle | Every quarter, or after any failed project |
| Contact | velsoftwaresolutions.in@gmail.com |

---

## 0.1 What this handbook is

This is the single procedure every employee follows to take an automation project from **an idea** to **a working system in a customer's hands**, and then keep it working. You do not need to know everything before you start. You need to **follow the stages in order, finish every checklist, and get every sign-off**.

If you are ever unsure what to do next, open this book at the stage your project is in. The answer is there. If it is not, ask your Technical Lead and then tell the Founder so the book can be fixed.

## 0.2 How to use it

1. Find your project's current stage in the **Stage Map** (section 0.4).
2. Read that stage file once completely **before** you start doing anything.
3. Do the numbered steps in order. Tick the **Done checklist** as you go.
4. Save every deliverable in the exact folder named in the stage.
5. Ask for the **gate sign-off**. You may not start the next stage without it.
6. Update the project status sheet every Friday.

## 0.3 The ten golden rules

1. **Never skip a stage or a gate.** Skipping is how projects fail.
2. **Understand the customer's work before writing code.** Code written for a misunderstood process is waste.
3. **Choose the simplest solution that works.** Use AI only where rules cannot do the job.
4. **Never trust AI output blindly.** Every AI answer is checked by rules before it is used.
5. **Never put a password, API key or customer data into Git.** Not once, not "temporarily".
6. **Write the test before or with the code.** No test, no merge.
7. **Small steps, saved often.** Commit at least every time something works.
8. **Write it down.** If it is not in a document or a ticket, it did not happen.
9. **Ask early.** Being stuck for 30 minutes without asking is a mistake, not a virtue.
10. **Customer data belongs to the customer.** Treat it as if it were your own bank details.

## 0.4 Stage map

| Stage | Name | Main question it answers | Approx. time (small project) | Gate |
|---|---|---|---|---|
| S00 | Workstation, accounts, habits | Am I ready to work? | 1 day | — |
| S01 | Opportunity and customer discovery | Is this a real, paid problem? | 1–2 weeks | G1 |
| S02 | Understand the process (AS-IS) | How does the customer work today? | 1 week | G2 |
| S03 | Feasibility and TO-BE design | What should be automated, and how? | 3–5 days | G3 |
| S04 | Requirements and scope freeze | What exactly will we build? | 3–5 days | G4 |
| S05 | Technology and architecture | With which tools and structure? | 3–5 days | G5 |
| S06 | Project setup | Where does the code live and run? | 1–2 days | — |
| S07 | Build the core | Do the rules and the API work? | 2–3 weeks | G6, G7 |
| S08 | AI and integrations | Can it read documents and talk to other systems? | 2 weeks | — |
| S09 | Testing and AI evaluation | Is it accurate and reliable enough? | 1–2 weeks | G8 |
| S10 | Security review | Can it be abused or leak data? | 3–5 days | G8 |
| S11 | Costing and pricing | What does it cost us, what do we charge? | 3 days | G9 |
| S12 | Deployment | Is it live, safe and monitored? | 3–5 days | G10 |
| S13 | Demo, acceptance, onboarding | Does the customer accept it? | 2–4 weeks | G11 |
| S14 | Maintenance and improvement | Does it keep working and improving? | Ongoing | G12 |

## 0.5 Who is who (plain words)

| Role | What they really do | You ask them when |
|---|---|---|
| Founder | Decides what we sell, signs contracts and prices | You need a decision about money, scope or customers |
| Project Manager (PM) | Plans the work, tracks time, talks to the customer daily | You need priorities, deadlines or customer contacts |
| Technical Lead | Decides how things are built, reviews your code | You are stuck technically or need a code review |
| Business Analyst | Writes down how the customer's process works | You do not understand the customer's process |
| AI Engineer | Builds and tests the AI steps and prompts | The AI gives wrong answers |
| Developer | Writes the services, APIs, dashboards | Day-to-day coding questions |
| QA Engineer | Tries to break the software before the customer does | You want to know how to test something |
| DevOps Engineer | Puts software on servers and keeps it running | Deployment, servers, backups, alerts |
| Security Specialist | Checks for ways the system can be attacked | Passwords, permissions, customer data |
| Customer Success | Trains the customer and handles support | A customer reports a problem |

In a small team one person wears several hats. The duties still exist; one person just does more of them.

## 0.6 Words you will hear every day (glossary)

| Term | Meaning in one line |
|---|---|
| Automation | A computer doing a repeated task so a person does not have to |
| Workflow | The ordered steps from a trigger to a result |
| Trigger | The event that starts a workflow (a new email, a file, 9 a.m.) |
| API | A doorway one program offers so another program can ask it for things |
| Webhook | A message another system sends *to us* when something happens |
| JSON | A plain-text way of writing structured data, like `{"total": 11800}` |
| Schema | The agreed shape of data: which fields, which types |
| Database | Organised storage for data that survives restarts |
| LLM | Large language model; the AI that reads and writes text |
| Prompt | The instructions we give the AI |
| Token | A piece of text the AI is charged by, roughly ¾ of a word |
| OCR | Turning a picture of text into real text |
| RAG | Giving the AI our documents to look things up before answering |
| Agent | An AI that chooses its own next steps using tools; use rarely |
| Repository (repo) | The folder of project code tracked by Git |
| Git | The tool that records every change to the code and who made it |
| Commit | One saved, described change in Git |
| Branch | A separate line of work so your changes do not break others |
| Pull request (PR) | A request to merge your branch after someone reviews it |
| CI/CD | Robots that automatically test (CI) and deploy (CD) your code |
| Environment | A separate copy of the system: local, staging, production |
| Staging | The safe practice copy of the live system |
| Production | The real, live system the customer uses |
| Secret | A password, key or token that must never be shared or committed |
| Idempotency | Doing the same request twice has the same effect as once (no double posting) |
| Gate | A mandatory approval point between stages |
| Scope | Exactly what we promised to build, no more, no less |
| Change request (CR) | A written, priced request to change the agreed scope |
| SLA | The service level we promise (response times, uptime) |
| UAT | User acceptance testing: the customer tries it and signs off |
| MRR | Monthly recurring revenue: subscription income each month |
| Margin | Revenue minus cost, as a share of revenue |
| Technical debt | Shortcuts taken now that will cost time later |

## 0.7 How to read the stage files

Every stage file has the same sections, in the same order:

1. **Purpose** — why this stage exists.
2. **Who and when** — owner, approver, time.
3. **Before you start** — what you must already have.
4. **Step by step** — numbered actions.
5. **Worked example** — the same stage done for our sample project (GST purchase-bill automation).
6. **Deliverables** — files you must produce and where they go.
7. **Done checklist** — tick every box.
8. **Common mistakes** — what beginners get wrong.
9. **Gate sign-off** — who approves and what they check.

## 0.8 The running example

Throughout the book we follow one sample project so you can see every stage done once:

> **Customer:** a 6-person chartered-accountant (CA) firm with 40 small-business clients.
> **Problem:** staff type about 1,500 supplier bills a month into accounting software by hand, taking about 6 minutes each, with typing errors.
> **Solution:** a system that reads each bill, checks the GST rules, sends doubtful bills to a person, and posts good ones to the accounting software.

A small working version of the rule-checking part is in `reference-project/`. You will build and run it in Stage 7.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
