# Part 2: Roles, Responsibilities and Career Path

## 2.1 Employee responsibilities (all grades)

Every employee in the department must:

- Follow this handbook and the company standards in Part 4.
- Keep **all client information confidential**, including after leaving the company.
- Use only company-approved tools and accounts. Never use personal accounts for client work.
- Never store credentials, tokens or personal data in Git, chat, email, tickets, screenshots or the portal.
- Record time and update ticket status daily.
- Write tests and documentation as part of the work, not after it.
- Report incidents, mistakes and near-misses **immediately**. Reporting early is rewarded; hiding is a disciplinary matter.
- Request a code review before every merge.
- Raise a risk in writing when a requirement, vendor or timeline looks unsafe.

## 2.2 Responsibility matrix (RACI)

**R** = Responsible (does it) · **A** = Accountable (signs off) · **C** = Consulted · **I** = Informed

| Activity | Founder | PM | Architect | Sr. Engineer | Engineer | Security | QA | DevOps |
|---|---|---|---|---|---|---|---|---|
| 01 Lead qualification | A | R | C | I | I | | | |
| 02 Client discovery | I | A | R | C | R | C | | |
| 03 Requirements freeze | A | R | C | C | C | C | C | |
| 04 Feasibility assessment | I | A | C | R | R | C | | |
| 05 Architecture design | I | C | A/R | C | C | C | | C |
| 06 Cost estimation | A | R | C | C | | | | C |
| 07 Environment setup | | I | | A | R | C | | R |
| 08 Repository structure | | I | C | A | R | | | C |
| 09 API development | | I | C | A | R | C | C | |
| 10 System integration | | I | C | A | R | C | C | |
| 11 API security | I | I | C | C | R | A | C | C |
| 12 Testing and QA | I | I | | C | R | C | A | |
| 13 CI/CD and deployment | I | A | C | C | C | C | C | R |
| 14 Production monitoring | I | I | C | C | C | | | A/R |
| 15 Client handover | I | A | C | R | R | C | C | C |
| 16 Maintenance | I | A | C | R | R | C | | R |

## 2.3 Role cards

### Trainee API Engineer (days 1 to 90)
**Does:** completes the 90-day roadmap, builds the ten practical projects, shadows live projects.
**May not:** deploy to production, access client production systems, or hold client credentials.
**Reports to:** assigned mentor (Senior Integration Engineer).

### Junior Integration Engineer
**Does:** implements well-specified endpoints and connectors under review; writes tests and docs.
**May:** work in staging with client sandbox credentials issued through the access register.
**Promotion test:** two supervised projects delivered with no critical defect found by the client.

### Integration Engineer
**Does:** owns a connector end-to-end, writes the data-mapping document, handles vendor liaison.
**Promotion test:** designs and delivers a multi-system integration with architect review.

### Senior Integration Engineer
**Does:** reviews all code, mentors trainees, leads feasibility (Stage 04), owns hard incidents.

### API Architect
**Does:** owns architecture (Stage 05), the API governance standards, the API inventory and the technology radar.

## 2.4 Mentor responsibilities

Each trainee has one named mentor who must: hold a 30-minute weekly one-to-one, review every pull request within one working day, mark the weekly assessment, and recommend pass / repeat at each gate. A mentor supervises at most three trainees at once.

## 2.5 Separation of duties (important for client trust)

| Rule | Why |
|---|---|
| The author of code never approves their own pull request | Independent review catches mistakes |
| The person who deploys to production is not the only person who approved it | Prevents unreviewed changes reaching clients |
| Production credentials are held by the DevOps/Security owner, never by trainees | Limits blast radius |
| Billing changes need PM and Founder approval | Prevents revenue leakage and disputes |
