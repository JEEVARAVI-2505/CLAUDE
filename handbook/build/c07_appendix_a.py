from lib import Chapter


def build():
    out = []
    # ---------------- Appendix A onboarding checklist
    c = Chapter("Appendix A", "Employee Onboarding Checklist", kind="appendix", search="onboarding checklist day one accounts laptop security policy")
    c.table(["Field", "Value"], [["Employee name", ""], ["Role / track", ""], ["Start date", ""], ["Buddy (peer)", ""], ["Technical Lead (mentor)", ""], ["Department Head", ""]], widths=[30, 70])
    c.h2("Before day 1 (HR + Technical Lead)")
    c.check(["Offer letter, NDA, IP-assignment and acceptable-use policy signed.", "Laptop imaged to the standard (disk encryption on, OS updates on, screen lock ≤ 5 min).",
             "Company e-mail, chat, project tool and GitHub organisation invitations sent; **MFA mandatory** on every account.",
             "Password manager licence assigned; employee told never to store credentials in notes or chat.",
             "Sandbox cloud account/budget alert created (≤ agreed monthly cap); no access to client production.",
             "Buddy and mentor assigned; 90-day plan (Appendix B) shared; first-week calendar booked."])
    c.h2("Day 1")
    c.check(["Welcome session: company mission, brand, services (Ch 1), the Golden Rules.", "Policies read and acknowledged: information security, confidentiality, data protection, acceptable use, code of conduct.",
             "Workstation set-up verified: Git, VS Code/Visual Studio, .NET 8 SDK, Node LTS, Docker, PostgreSQL client — each with a version check.",
             "GitHub: SSH key or credential manager configured; commit signing/e-mail set; first commit to the training repo via PR.",
             "Read Chapters 1, 2 and 5 (lifecycle); complete the 10-question orientation quiz."])
    c.h2("Week 1")
    c.check(["Level 1 started; shadow one BA meeting and one stand-up.", "Complete security awareness module (phishing, passwords, data handling, clean desk).",
             "Practise the safety rule: look before delete, backup before change, ask before production.", "Set up the personal portfolio repository (training evidence).",
             "Meet Technical Lead: agree learning goals and the track (BA / Backend / Frontend / Data / QA / Support)."])
    c.h2("Access matrix (least privilege)")
    c.table(["System", "Trainee", "After L3", "After 3 supervised projects"], [
        ["Training repos", "Write (PR only)", "Write", "Write"], ["Client repos", "None", "Read", "Write via PR"],
        ["Client staging", "None", "Read", "Deploy with approval"], ["Client production", "None", "None", "Support access by ticket only"],
        ["Cloud sandbox", "Own account", "Own account", "Own account"], ["Costing/finance files", "None", "None", "As role requires"],
    ], widths=[26, 22, 22, 30])
    c.h2("Sign-off")
    c.table(["Role", "Name", "Signature", "Date"], [["Employee", "", "", ""], ["HR", "", "", ""], ["Technical Lead", "", "", ""], ["Department Head", "", "", ""]], widths=[28, 28, 28, 16])
    out.append(c)

    # ---------------- Appendix B 90-day plan
    c = Chapter("Appendix B", "90-Day Training Plan", kind="appendix", search="90 day plan weeks training schedule milestones")
    c.p("Full-time, supervised. Each week ends with a Friday review (30 min) with the mentor. Reduce scope — never skip exit criteria — if time is lost.")
    plan = [
        ("1–2", "L1 Business systems", "Ch 1–2; business fundamentals; order-to-cash map; orientation", "Quiz ≥ 80 %; whiteboard demo", "Document a small business (L1 capstone)"),
        ("3–5", "L2 Business analysis", "Discovery role-plays; process maps; stories & acceptance criteria; change request pricing", "Requirement-review defence", "BRD + FRD for 4-module mini ERP"),
        ("4–7", "L3 Software engineering (starts week 4, overlaps)", "Language fundamentals; OOP; APIs; layers; auth; errors; logging; Git basics", "Code review ≥ 80 %", "**P1 Employee management system** (weeks 6–7)"),
        ("8–10", "L4 Database engineering", "Relational design; SQL; normalisation; transactions; indexes; migrations; backup/restore", "Timed SQL practical; restore drill", "**P2 Inventory system** (weeks 9–10)"),
        ("10–11", "P3 CRM prototype + P4 start", "Stories to working software; pipeline reports", "Acceptance demo", "**P3 CRM prototype**"),
        ("11–12", "L5 Enterprise architecture", "Modular monolith; RBAC/audit; workflow; integration; data migration; multi-tenancy rules", "Architecture board (C4 + 3 ADRs)", "**P4 Billing app**; P5 started"),
        ("12", "Governance", "**Run link/price verification of all sources and rate assumptions (Ch 8, 11)**; update handbook change log", "Verification log", "Verified source list"),
        ("12–13", "L6 Professional engineering", "Pull requests; CI/CD; Docker; cloud deployment; monitoring; OWASP; documentation", "Pipeline demo + security checklist", "P4 in staging; **P5 Portal**"),
    ]
    c.table(["Weeks", "Focus", "Activities", "Assessment", "Output"], [list(p) for p in plan], widths=[8, 20, 36, 18, 18])
    c.p("Weeks 12–13 are the final fortnight of the plan; P6 (ERP prototype) and P7 (simulated client project) continue in the **next 6–9 weeks** as supervised work and complete the certification path of Chapter 10.")
    c.h2("Milestones and checkpoints")
    c.table(["Day", "Checkpoint", "Pass criterion"], [
        ["Day 5", "Orientation complete", "Policies signed; workstation verified; first PR merged"],
        ["Day 14", "Level 1 passed", "Quiz ≥ 80 %; L1 capstone approved"],
        ["Day 35", "Level 2 passed", "Requirement-review defence passed"],
        ["Day 49", "P1 delivered", "CI green; acceptance criteria met"],
        ["Day 70", "Level 4 + P2 delivered", "Restore drill witnessed"],
        ["Day 85", "Level 5 architecture board", "C4 L1–L3 + 3 ADRs accepted"],
        ["Day 90", "Readiness review", "Technical Lead + Department Head confirm assignment to a supervised client project"],
    ], widths=[12, 38, 50])
    c.h2("Weekly rhythm")
    c.ul(["**Mon:** plan week with mentor; **Tue–Thu:** learn → build → review; **Fri:** demo + retrospective + update training record.",
          "Daily: 15-minute stand-up (done / next / blocked). Write a 5-line learning log every day.",
          "Never spend more than 45 minutes stuck without asking — then bring your notes (what you tried, error text, expected vs actual)."])
    out.append(c)
    return out
