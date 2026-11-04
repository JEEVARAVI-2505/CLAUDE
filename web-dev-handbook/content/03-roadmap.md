For **every** topic the trainee receives the same 15-step pack (definition to approval gate) - see Section "How every topic is taught". Modules are expandable; each contains working code, expected output and an assessment. A level is passed **only when every module's exercise has been demonstrated** to the mentor and the Technical Lead has signed the level gate.

{{diagram: roadmap | Figure 3.1 - The five-level learning roadmap}}

| Level | Theme | Modules | Approx. time | Capstone / gate |
|---|---|---|---|---|
| **1** | Web fundamentals | 9 | 2 weeks | Quiz (80%) + whiteboard of the request path |
| **2** | Frontend development | 9 | 5 weeks | Projects 1 and 2 accepted |
| **3** | Backend development | 9 | 4 weeks | Project 3 (authentication app) accepted |
| **4** | Database engineering | 8 | 3 weeks | Project 4 (CRUD + DB) with restore evidence |
| **5** | Professional engineering | 11 | 4 weeks | Projects 5-7, security review, delivery readiness |

!!! tip "Study rhythm"
    Daily: 2 hours reading + 4 hours hands-on + 30 minutes writing notes in the training repository. Weekly: mentor review and one demonstration. Never copy-paste code you cannot explain line by line.

{{diagram: web_request | Figure 3.2 - Level 1 big picture: the path of one web request}}

## Level 1 - Web Fundamentals (about 2 weeks)

Goal: explain how the web works end to end and name the part where any given problem lives.

{{modules: level1}}

## Level 2 - Frontend Development (about 5 weeks)

Goal: build responsive, accessible, fast user interfaces with HTML, CSS, JavaScript, TypeScript, React and Next.js.

{{modules: level2}}

## Level 3 - Backend Development (about 4 weeks)

Goal: build secure, tested, documented APIs with authentication, authorisation, validation and business logic.

{{diagram: auth_flow | Figure 3.3 - Authentication sequence used throughout Level 3}}

{{modules: level3}}

## Level 4 - Database Engineering (about 3 weeks)

Goal: design, build, query, tune, migrate and back up relational databases.

{{diagram: erd | Figure 3.4 - Sample entity-relationship diagram for a booking system}}

{{modules: level4}}

## Level 5 - Professional Engineering (about 4 weeks)

Goal: work like a professional - version control, review, testing, containers, pipelines, cloud, observability and security.

{{diagram: git_flow | Figure 3.5 - Git branching strategy}}

{{diagram: cicd | Figure 3.6 - Standard CI/CD pipeline}}

{{modules: level5}}

## Level assessment summary

| Level | Written | Practical | Oral (Technical Lead) |
|---|---|---|---|
| 1 | 20-question quiz, pass 80% | Whiteboard request path; DNS plan | 10 minutes |
| 2 | 30-question quiz | Timed UI task; component review | 15 minutes |
| 3 | Security quiz | Attack-test of your API (five attacks) | 20 minutes |
| 4 | SQL test (10 queries / 60 min) | Restore drill within RTO | 15 minutes |
| 5 | OWASP and DevOps quiz | Pipeline + rollback demonstration; security review | 30 minutes |
