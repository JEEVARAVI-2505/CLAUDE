The onboarding programme turns a new joiner into a safe, productive team member in the first 13 weeks. It combines **access and policy setup (days 1-5)**, **foundation learning (weeks 1-4)** and **supervised delivery (weeks 5-13)**. The detailed tick-list is **Appendix A1**; the week-by-week plan is **Appendix A2**.

## Who you are on day one

You know basic computer operations - files, folders, browser, e-mail. You are **not** expected to know programming. The programme assumes zero professional web experience and builds from there. You will always work under a named mentor until you pass Gate 5 (week 13).

## Onboarding timeline

| When | Focus | You will finish with |
|---|---|---|
| **Before day 1** | HR paperwork, laptop prepared, accounts requested | Laptop with disk encryption, accounts ready |
| **Day 1** | Welcome, company overview, security and confidentiality briefing, policies signed | Signed NDA and acceptable-use policy; password manager and MFA active |
| **Day 2** | Tooling - Git, GitHub, VS Code, terminal; clone the handbook repo | First commit and pull request |
| **Day 3** | Department tour - lifecycle, folder structure, branching, review rules | You can explain the 27 stages in your own words |
| **Day 4** | Level 1 start; mentor pairing; first practice task | Level 1 module L1.1 complete |
| **Day 5** | Week-1 review with mentor; set 30/60/90-day goals | Signed learning plan |
| **Weeks 2-4** | Levels 1 and 2 (HTML, CSS, JavaScript) | Projects 1 and 2 started |
| **Weeks 5-8** | Levels 3 and 4 (backend, databases) | Projects 3 and 4 |
| **Weeks 9-11** | Level 5 (professional engineering) | Projects 5 and 6 |
| **Weeks 12-13** | Simulated client project; final assessment | Project 7; certification decision |

## Accounts, devices and access (day 1-2)

| Item | Rule |
|---|---|
| Laptop | Company-approved, full-disk encryption, screen lock at 5 minutes, OS and browser auto-update |
| Identity | One individual account per tool (e-mail, GitHub, project tracker, cloud console). **No shared logins** |
| MFA | Mandatory on e-mail, GitHub, cloud, password manager, registrar |
| Password manager | Company-approved; unique random password per site; no passwords in notes, chat or files |
| SSH key | `ssh-keygen -t ed25519 -C "you@company.com"` with a passphrase; add the **public** key to GitHub; never share the private key |
| Access level | Least privilege: read-only first, write after Gate 2, production access only after Gate 5 and only for assigned projects |
| Personal devices | No client data on personal devices unless the written policy permits it |

!!! danger "FORBIDDEN"
    Shared root or admin accounts. Client passwords in chat or e-mail. Client source code on personal GitHub accounts. Production credentials on laptops. Copying client data to USB drives or personal cloud storage.

## Policies every employee signs

| Policy | Summary |
|---|---|
| Confidentiality / NDA | Client information, source code and credentials are confidential during and after employment |
| Acceptable use | Company devices and accounts for company work; no pirated software |
| Information security | Report suspected incidents within one hour; follow Section 9 escalation |
| Data protection | Process personal data only as the project requires; use synthetic data for testing |
| Intellectual property | Work product belongs to the company or the client as per contract; open-source licences are checked before use |
| Code of conduct | Respectful, honest, collaborative; no plagiarism; credit sources |
| Leave and handover | Document work-in-progress before leave (Section 9.6) |

## Mentor model

| Item | Rule |
|---|---|
| Ratio | One mentor per two trainees at most |
| Meetings | 15-minute daily check-in; 45-minute weekly review |
| Stuck rule | Try for 30 minutes alone, 2 hours with documentation and notes, then ask - with a written summary of what you tried |
| Evidence | Every module ends in an artefact in the training repository (code, notes, screenshots) |
| Sign-off | The mentor signs module gates; the Technical Lead signs level gates |

## The five onboarding gates

| Gate | When | Criteria |
|---|---|---|
| **Gate 1 - Access and security** | End of week 1 | All accounts and MFA active; policies signed; passes the security basics quiz (80%) |
| **Gate 2 - Foundations** | End of week 4 | Levels 1-2 modules signed; Projects 1-2 accepted; may now commit to client repositories via pull request |
| **Gate 3 - Backend and data** | End of week 8 | Levels 3-4 modules signed; Projects 3-4 accepted |
| **Gate 4 - Professional practice** | End of week 11 | Level 5 modules signed; Projects 5-6 accepted; first code review given |
| **Gate 5 - Delivery readiness** | End of week 13 | Project 7 (simulated client project) accepted; oral assessment passed; may join supervised client work |

!!! note "A level is passed only when every exercise has been demonstrated"
    Reading is not enough. For each module you must show the working result to your mentor, answer the assessment questions, and have the gate signed in your learning record.

## First-week checklist (summary)

- [ ] Laptop encrypted, locked and updated
- [ ] Company e-mail, GitHub, project tracker, password manager - all with MFA
- [ ] NDA and policies signed
- [ ] Git, VS Code, Node.js, Python, Docker installed per the README
- [ ] SSH key created and added to GitHub
- [ ] First pull request merged into the training repository
- [ ] Mentor assigned; 30/60/90-day goals written and signed

The complete checklist with owners and dates is **Appendix A1**.
