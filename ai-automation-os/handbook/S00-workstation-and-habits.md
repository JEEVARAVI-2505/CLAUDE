# Stage S00 — Workstation, Accounts and Habits

## Purpose
Get your computer and accounts ready, and learn the daily habits, so that from Stage S01 onward you can concentrate on the project.

## Who and when
- **Owner:** You (new joiner). **Helper:** Technical Lead. **Approver:** Technical Lead.
- **Time:** 1 day.

## Before you start
A laptop with admin rights, internet, a company email address, and this handbook.

## Step by step

### A. Accounts (ask the Technical Lead to create or invite you)
1. Company email and calendar.
2. GitHub account added to the company organisation (turn on two-factor authentication immediately).
3. Task board (ticket system) access.
4. Company drive access to the folders for your project only.
5. Team chat.
6. A password manager. Store every password there; never in a notebook or chat.

### B. Install the tools
Install in this order. After each, run the **check command**; it must print a version.

| # | Tool | Why | Check command |
|---|---|---|---|
| 1 | Git | Records changes | `git --version` |
| 2 | Python 3.12 or newer | Our main language | `python --version` |
| 3 | VS Code | Code editor | open it |
| 4 | VS Code extensions: Python, Pylance, GitLens, YAML | Editing help | check Extensions tab |
| 5 | Node.js (LTS) | Dashboards and some tools | `node --version` |
| 6 | Docker Desktop | Runs identical copies of software | `docker --version` |
| 7 | Postman or Bruno | Try out APIs by hand | open it |
| 8 | A database viewer (e.g. DBeaver) | Look inside databases | open it |

If a check command fails, write down the exact error text and ask. Do not guess.

### C. Configure Git (once)
```bash
git config --global user.name "Your Full Name"
git config --global user.email "your.company.email@example.com"
git config --global init.defaultBranch main
```

### D. Get the code and run the example
```bash
git clone <repository-url>
cd <repository-folder>/ai-automation-os/reference-project
python -m venv .venv
source .venv/bin/activate        # Windows: .venv\Scripts\activate
pip install -r requirements.txt
python -m pytest -q
```
Expected result: `23 passed`. If you see that, your workstation works.

### E. Learn the five Git commands you will use daily
| Command | What it does |
|---|---|
| `git status` | Shows what changed |
| `git switch -c feature/GBA-12-short-name` | Starts a new branch for your ticket |
| `git add <file>` | Chooses what goes into the next save |
| `git commit -m "GBA-12: what you did"` | Saves a described change |
| `git push -u origin feature/GBA-12-short-name` | Sends your branch to GitHub |

Never use `git push --force` or `git reset --hard` unless the Technical Lead tells you to.

### F. Daily routine
1. Morning: read the board and chat; stand-up.
2. Pick **one** ticket from *Ready*; move it to *In Progress*.
3. Work in small steps; run tests often.
4. Commit when something works.
5. Open a pull request; move the ticket to *In Review*.
6. End of day: update the ticket with what is done and what is next.

### G. Confidentiality and security habits
- Lock your screen when you leave.
- Never paste customer data or secrets into public tools or AI chat services unless the Founder has approved that tool for that data.
- Report a lost laptop or suspected leak **immediately**.

## Deliverables
- Tools installed and verified (screenshot of the version checks in your onboarding ticket).
- Reference project tests passing on your laptop.
- Signed confidentiality agreement filed in the HR folder.

## Done checklist
- [ ] All accounts work; two-factor authentication is on.
- [ ] All eight tools pass their check commands.
- [ ] `git config` shows my name and email.
- [ ] I cloned the repository and `pytest` shows 23 passed.
- [ ] I read Parts 0, 1 and 2 of this handbook.
- [ ] I know who my Technical Lead and PM are.

## Common mistakes
- Installing a different Python version than the project uses.
- Working directly on `main`.
- Saving passwords in a text file.
- Leaving two-factor authentication for "later".

## Gate sign-off
Technical Lead confirms the checklist. No gate number; this is a readiness check.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
