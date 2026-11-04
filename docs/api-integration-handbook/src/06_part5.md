# Part 5: Beginner Onboarding Programme

## 5.1 Day-0 checklist (before the first morning)

| Item | Owner | Done |
|---|---|---|
| Company email account created | Admin | ☐ |
| GitHub account invited to the VEL organisation (two-factor authentication **on**) | Admin | ☐ |
| Password manager account set up | Security | ☐ |
| Laptop with disk encryption, OS updates, screen lock | DevOps | ☐ |
| Mentor assigned and introduced | Training Manager | ☐ |
| Signed confidentiality agreement and acceptable-use policy | Founder | ☐ |
| Access register entry created (least privilege: training sandbox only) | Security | ☐ |

## 5.2 Week-1 schedule

| Day | Morning | Afternoon | Output |
|---|---|---|---|
| 1 | Welcome, company tour, read Part 0 and Part 1 | Security briefing; set up password manager and 2FA | Signed policies |
| 2 | Read Part 3 (workflow overview) | Install tools (5.3) | Screenshot of every tool's version |
| 3 | Level 1 lesson: software fundamentals | Level 1 quiz; first Git commit to the training repo | Quiz ≥ 80% |
| 4 | Level 2 lesson: HTTP and the web | Hands-on: inspect requests in browser dev tools and Postman | Notes file |
| 5 | Python starter exercises | Weekly review with mentor; Friday report | First weekly report |

## 5.3 Install the toolchain

Run each verification command and keep the output in your notes.

| Tool | Install | Verify |
|---|---|---|
| **VS Code** | Official installer from code.visualstudio.com; add extensions *Python*, *Pylance*, *REST Client* (optional), *Docker*, *YAML*, *GitLens* | Open a folder; run `code --version` |
| **Python 3.12+** | python.org installer or OS package manager | `python --version` |
| **Node.js LTS** (needed from Level 6) | nodejs.org installer | `node --version` and `npm --version` |
| **Git** | git-scm.com | `git --version` |
| **GitHub** | Create/sign in with the company account; enable 2FA; add an SSH key | `ssh -T git@github.com` |
| **Postman** and/or **Insomnia** | Official downloads | Send a `GET` to a public test API |
| **PostgreSQL 16** (use Docker if easier) | Official installer or `docker run` (see Assignment 02) | `psql --version` |
| **Docker** | Docker Desktop or Docker Engine | `docker run hello-world` |

**Configure Git once:**

```bash
git config --global user.name  "Your Name"
git config --global user.email "your.name@company-domain"
git config --global init.defaultBranch main
git config --global pull.rebase true
```

**Create a virtual environment for every project** (never install packages globally):

```bash
python -m venv .venv
source .venv/bin/activate        # Windows PowerShell: .venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
```

## 5.4 Security rules for every day

1. Two-factor authentication on every account (email, GitHub, cloud, password manager).
2. Credentials live **only** in the password manager or the secrets store.
3. Never paste a client token, key or record into a public AI tool, forum, chat or ticket.
4. Lock your screen when you leave your desk. Disk encryption stays on.
5. Report a lost device, a leaked key or a suspicious email **immediately**.
6. Use test data. If you must see real client data, a Senior Engineer approves it in writing first.

## 5.5 Working agreement

| Topic | Rule |
|---|---|
| Stand-up | 10 minutes, daily: yesterday / today / blocked |
| Tickets | Every task is a ticket; move it as you work |
| Commits | Small, frequent, clear message: `type(scope): what and why` |
| Pull requests | Template completed; tests green; reviewer assigned |
| Help | Try for 30 minutes, then ask. Include what you tried and the exact error |
| Friday report | Done, learning, blocked, next week (Appendix D) |

## 5.6 First-week success criteria

By the end of week 1 you can: explain client-server, request and response in your own words · send a `GET` and a `POST` in Postman · read a JSON document · create a Git branch, commit and open a pull request · state the twelve golden rules.
