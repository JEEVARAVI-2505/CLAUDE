This handbook is the single operating manual of the **Web Development Engineering Department**. It does two jobs: **(1)** it trains a beginner who knows basic computer operations into a professional who can join supervised client projects, and **(2)** it defines the mandatory 27-stage lifecycle every client project follows - with approval gates, deliverables and acceptance criteria - from the first enquiry to maintenance.

!!! gate "GOLDEN RULES"
    1. Do not skip stages. Do not start a stage until the acceptance criteria of the previous stage are met and its gate is signed.
    2. "It runs on my laptop" is not "production-ready". Production-ready means secure, tested, monitored, backed up, restorable, documented, cost-controlled and handed over.
    3. Never commit credentials, private keys, production `.env` files or confidential customer data to Git, chat, e-mail or documents.
    4. Never run destructive commands (`rm -rf`, `DROP DATABASE`, `git push --force`) without understanding the effect and holding a verified backup.
    5. Every production change needs a second person's review.
    6. Never promise uptime, "unhackable" security, rankings or savings without evidence. We say what was tested - not that a system is "secure".
    7. Never invent prices. Quote only from official vendor pricing pages and calculators, with the date checked.

## Reading path for a new employee

{{flow: Part A · Foundations | Part B · Client delivery | Part C · Business and people | Part D · Practice and sources | Part E · Toolkit}}

| Part | What you will do | Typical duration |
|---|---|---|
| **A - Foundations** | Understand the department, complete onboarding, study Levels 1-5, choose tools | Weeks 1-12 |
| **B - Client delivery** | Learn and follow the 27-stage lifecycle, repository structure and delivery standards | Continuous, from week 4 |
| **C - Business and people** | Understand costing, pricing, roles, daily procedures and escalation | Weeks 6-12, then reference |
| **D - Practice and sources** | Build seven practical projects; use official learning sources | Weeks 3-13 |
| **E - Toolkit** | Use the checklists and templates on real work | Continuous |

## How every topic is taught (15-step methodology)

Each learning module in Section 3 follows the same fifteen steps so that nothing is skipped and every module can be assessed the same way.

| # | Step | What it gives you |
|---|---|---|
| 1 | Definition | A plain-language meaning |
| 2 | Business purpose | Why a client would pay for it |
| 3 | Real-world example | A concrete case |
| 4 | Prerequisites | What you must know first |
| 5 | Tools and installation | What to install and whether it is free or paid |
| 6 | Step-by-step procedure | Exactly what to do |
| 7 | Complete example | Working code or configuration to copy and run |
| 8 | Expected output | What you should see |
| 9 | Verification | How to prove it works |
| 10 | Common errors | What usually goes wrong |
| 11 | Security considerations | How it can be attacked and how to defend |
| 12 | Documentation | What to write down |
| 13 | Practical assignment | Hands-on work |
| 14 | Assessment | How you are tested |
| 15 | Approval gate | Who signs you off |

!!! note "How to use the HTML edition"
    Press `/` to search. Click a module title to collapse or expand it. Use **Copy** on any code block. Tick "Mark module complete" to track your own progress (saved in your browser only). Use **Print** for a clean printable version.

## Conventions used in this handbook

| Symbol / style | Meaning |
|---|---|
| `monospace` | A command, file name, code or configuration value |
| **Gate** box (amber) | A mandatory approval point |
| **Forbidden / danger** box (red) | An action that is never allowed, or a serious risk |
| **Note / tip** box (blue / green) | Helpful context or a recommended practice |
| CU | Generic currency unit used in costing examples (no real currency implied) |
| Level / Module ids | `L2.8` = Level 2, module 8 |

## Document control

| Item | Value |
|---|---|
| Document | VEL-WD-001 - Web Development Department Handbook |
| Version | 1.0 (draft for management approval) |
| Owner | Technical Lead, Web Development Department |
| Review cycle | Every 6 months, or when a major tool/standard changes |
| Applies to | All web development employees, interns and approved contractors |
| Design reference | VEL Cloud Engineering and DevOps Playbook (VEL-CD-001) - page size, palette, typography and layout |

!!! danger "VERIFY BEFORE YOU BUILD OR QUOTE"
    Tool versions, free-tier limits, licence terms and cloud prices change often. This handbook states **methods and formulas**, and it points to official sources. It deliberately does **not** state vendor prices. Always check the official documentation and pricing calculators on the day you build or quote, and record the source and date.
