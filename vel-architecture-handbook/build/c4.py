from lib import *
import diagrams as D

MERMAID_CTX = '''flowchart LR
  customer([Customer]) -->|Places orders| sys[GreenLeaf Online Ordering System]
  staff([Store Staff]) -->|Manages stock and orders| sys
  sys -->|Takes payment| pay[Payment Gateway]
  sys -->|Sends notices| msg[Email / SMS Service]
  sys -->|Exports invoices| acc[Accounting Package]'''

PUML_CTX = '''@startuml
!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Context.puml
title System Context: GreenLeaf Online Ordering (FICTIONAL EXAMPLE)
Person(customer, "Customer", "Buys organic produce online")
Person(staff, "Store Staff", "Manages stock and orders")
System(sys, "GreenLeaf Online Ordering System", "Takes and tracks orders")
System_Ext(pay, "Payment Gateway", "Card payments")
System_Ext(msg, "Email / SMS Service", "Customer notices")
System_Ext(acc, "Accounting Package", "Existing finance system")
Rel(customer, sys, "Places orders", "HTTPS")
Rel(staff, sys, "Manages stock and orders", "HTTPS")
Rel(sys, pay, "Takes payment", "HTTPS/API")
Rel(sys, msg, "Sends notices", "HTTPS/API")
Rel(sys, acc, "Exports invoices", "File/API")
@enduml'''

ADR_EX = '''# ADR-0003: Start with a modular monolith on managed PostgreSQL

Status: Proposed   | Date: 2026-11-04 | Deciders: Solution Architect (author), Principal Architect (approver)
Reviewer: <name>   | Supersedes: none

## Context
GreenLeaf has a 4-person development team (CONFIRMED, interview 2026-10-28), no on-call
experience (CONFIRMED), expects 200 concurrent users at peak (ASSUMED, owner: client CFO, confirm by 2026-11-15).

## Options considered
1. Modular monolith + managed PostgreSQL   2. Microservices on Kubernetes   3. Low-code SaaS storefront

## Decision
We will build a modular monolith with a single managed PostgreSQL database.

## Consequences
+ Lowest operational burden; single deployment; fastest delivery (est. 10 weeks).
+ Strong transactions for orders and stock.
- Whole application scales together; revisit if any module needs independent scaling.
- Module boundaries need enforcement (architecture tests).
Review trigger: peak concurrency > 1,000 or team > 15 engineers.'''

def onboarding():
    return [H1('Beginner Onboarding Guide', 'onboarding', 10),
    P('Welcome to the Software Architecture & Technical Consulting Department. This guide takes you from your first day to your first reviewed architecture diagram.'),
    H2('What this department is (and is not)'),
    UL(['**We are** advisers who help clients make sound, explainable technology decisions.','**We are not** diagram factories. A diagram without a decision, a reason and a requirement behind it has no value.','**You are** supported: seniors review everything you produce, and mistakes found in review are learning, not failure.']),
    H2('Before day 1 (manager checklist)'),
    CHECK(['Laptop, company email, GitHub organisation access (least privilege), VS Code installed','Password manager and multi-factor authentication enabled','Read confidentiality and acceptable-use policies; sign acknowledgement','Assign a mentor (Solution Architect) and a reviewer (Principal Architect)','Create the trainee\'s training repository from architecture-project-template/']),
    H2('First week plan'),
    TABLE(['Day', 'Learn', 'Do', 'Output', 'Sign-off'], [
     ['1', 'Department purpose; golden rules; how a project flows (Chapter 6)', 'Tool setup; clone the template repo; read the 15 stages', 'Setup checklist signed; 1-paragraph summary of the lifecycle in own words', 'Mentor'],
     ['2', 'Level 1: software, servers, networks, APIs', 'Call a public API in Postman; trace DNS', 'Notes + screenshots in /reviews/week1', 'Mentor'],
     ['3', 'Level 1: databases, cloud, security basics', 'Explain IaaS/PaaS/SaaS with examples', 'One-page explainer', 'Mentor'],
     ['4', 'Layered architecture; three-tier', 'Lesson L-02: draw first diagram (Chapter 11)', 'Diagram v1', 'Mentor'],
     ['5', 'Review and feedback', 'Revise diagram; write 5 lessons learned', 'Diagram v2 + review record', 'Principal / Solution Architect']], [6, 28, 28, 24, 14]),
    H2('Daily working rhythm'),
    OL(['Morning: read your mentor\'s notes and today\'s learning objective.','Study and practise (about 50% of time in first 30 days).','Produce one written artefact per day (note, diagram, ADR draft).','Commit work to Git with a clear message; open a pull request for review.','End of day: write three lines: done, blocked, next.']),
    H2('How to ask for help'),
    UL(['Try for 20-30 minutes; write down what you tried and what you expected.','Ask your mentor with that note. Questions with context get answers in minutes.','Never guess a client fact. Mark it ASSUMPTION and ask.']),
    CALL('red', 'Professional conduct', 'Client information is confidential. Never paste client data into public tools. Never store secrets in Git. Never email client documents to personal accounts. Report mistakes immediately: speed matters more than blame.'),
    H2('Your first 30-60-90 days at a glance'),
    TABLE(['Period', 'Goal', 'Evidence'], [
     ['Days 1-30', 'Understand fundamentals and business analysis; produce first diagrams', 'Levels 1-2 passed; Projects 1-2 reviewed'],
     ['Days 31-60', 'Architecture fundamentals, patterns, technical architecture', 'Levels 3-5 passed; Projects 3-6 reviewed'],
     ['Days 61-90', 'Evaluation, governance and a full simulated proposal', 'Levels 6-7 passed; Projects 7-9 reviewed; supervised shadowing of a real engagement']], [16, 44, 40])]

def exercise():
    return [H1('First Architecture Diagram Exercise', 'exercise', 11),
    P('**Objective:** draw a clear **three-tier application architecture** for a simulated client, then create the matching **C4 Level 1 system context diagram**. You will use draw.io (visual) and Mermaid or PlantUML (diagram-as-code).'),
    CALL('blue', 'Simulated client (fictional)', '**GreenLeaf Organics** sells organic produce. Today orders arrive by phone and WhatsApp and are written into a spreadsheet. The owner wants customers to order online, pay by card, and receive confirmation by SMS/email. Store staff must see and update orders and stock. Accounting uses an existing package. Expected: 200 concurrent users at peak (ASSUMED). Budget and date: unknown.'),
    H2('Step-by-step'),
    OL(['**Read the brief twice.** Underline actors (customer, staff) and external systems (payment gateway, SMS/email, accounting).',
        '**List 8 requirements** (4 functional, 4 non-functional with numbers). Mark each CONFIRMED, ASSUMED or UNKNOWN. In this brief, almost all are ASSUMED.',
        '**Open draw.io** and create a new diagram. Name the file `greenleaf-three-tier-v1.drawio`; save it in `diagrams/containers/`.',
        '**Draw three columns/boxes:** Presentation (web UI in the browser), Application (business logic and API on servers), Data (SQL database). Label each with its technology only if justified; otherwise label the role.',
        '**Draw arrows** with labels: HTTPS, JSON/REST, SQL. Arrows go only between neighbouring tiers.',
        '**Draw the network boundary** around everything except the customer. Add a legend and a title with the date and version.',
        '**Check:** can the browser reach the database directly? It must not.',
        '**Export** SVG and PNG next to the source file. Commit with the message `docs: add three-tier diagram v1`.',
        '**Draw the C4 Level 1 context diagram** (next page): one box for the system, people and external systems around it, labelled relationships.',
        '**Write the one-paragraph explanation** a non-technical owner can understand, plus 3 assumptions and 3 open questions.',
        '**Submit** a pull request to your mentor.']),
    DIAGRAM(D.three_tier(), '**Figure 11.1** Answer key: three-tier architecture for GreenLeaf Organics (training example).'),
    DIAGRAM(D.c4_context(), '**Figure 11.2** Answer key: C4 Level 1 system context (training example).', MERMAID_CTX, 'mermaid'),
    H2('PlantUML version of the same context diagram'),
    CODE(PUML_CTX, 'plantuml'),
    CALL('blue', 'Rendering', 'Paste the Mermaid source into any Mermaid renderer or a GitHub Markdown code block tagged `mermaid`. For PlantUML use the VS Code PlantUML extension. Check the official sites (Chapter 14) for current syntax and the C4-PlantUML include path.'),
    H2('Marking rubric (100 marks)'),
    TABLE(['Criterion', 'Marks', 'Full marks when'], [
     ['Correct tiers and responsibilities', '20', 'Three tiers present with correct roles'],
     ['Correct dependencies', '15', 'Only neighbour-to-neighbour; no browser-to-database arrow'],
     ['Labels and protocols', '15', 'Every arrow labelled with what flows and how'],
     ['Boundaries and legend', '10', 'Network/trust boundary, legend, title, date, version'],
     ['C4 context correctness', '20', 'Actors, system, external systems, relationships, no internal detail'],
     ['Plain-language explanation', '10', 'Owner could understand it; no unexplained jargon'],
     ['Assumptions and open questions', '10', 'Honest labelling; none presented as fact']], [40, 12, 48]),
    P('**Pass mark:** 75. Below 75: revise and resubmit once. The reviewer records feedback in `reviews/`.'),
    H2('Common mistakes in this exercise'),
    UL(['Naming products (e.g. "MongoDB") before any requirement justifies them.','Drawing every possible box. A context diagram should have fewer than 10.','Arrows with no label or no direction.','Mixing levels: a database table in a context diagram.','Treating assumptions as facts.'])]

def lessons():
    L = []
    L.append(LESSON('L-01', 'Software Architecture vs Design', {
     'Definition': 'Architecture is the set of significant decisions about a system\'s structure, qualities and constraints that are costly to change. Design is the detailed work inside those decisions.',
     'Business relevance': 'Architecture decisions decide long-term cost, speed of change, risk and scalability. Mistakes are expensive after release.',
     'Technical purpose': 'To ensure the system meets functional and quality requirements and can evolve, by choosing structure, technologies and boundaries deliberately.',
     'Real-world case': 'A retailer chose one shared database for everything. Three years later, a reporting query slowed checkout during sales. Changing it needed a rebuild: an architectural, not a coding, mistake.',
     'Prerequisites': 'Level 1 complete. Basic understanding of client/server and databases.',
     'Tools': 'Notebook, draw.io, Markdown in VS Code.',
     'Step-by-step method': '1) List the decisions in a sample system. 2) For each, ask: how costly to reverse? 3) Costly or cross-cutting = architectural. 4) Record architectural ones as ADRs; leave the rest to design.',
     'Diagram': 'Draw a "cost of change" curve: low early, high late. Mark where architectural decisions sit.',
     'Decision trade-offs': 'Too much architecture up front wastes effort and guesses; too little causes rework. Decide the minimum needed to proceed safely (the "last responsible moment").',
     'Expected output': 'A table of 10 decisions classified architectural/design with reasons.',
     'Verification': 'Mentor picks 3 items and asks "why architectural?" The trainee answers using cost-of-reversal and impact.',
     'Common mistakes': 'Calling every technology choice architecture; ignoring quality attributes; confusing diagrams with architecture.',
     'Documentation': 'Table stored in /reviews/week2/classification.md; architectural items linked to draft ADRs.',
     'Assignment': 'Classify decisions from the GreenLeaf case (database, hosting, UI library, button colour, payment provider, password rules...).',
     'Review and approval': 'Reviewer: Solution Architect. Approved when at least 8/10 are classified correctly with sound reasons.'}))
    L.append(LESSON('L-02', 'Three-Tier Architecture (first diagram)', {
     'Definition': 'A layered style splitting an application into presentation, application (business logic) and data tiers, each deployable and replaceable independently.',
     'Business relevance': 'The default shape of most business web applications; easy to staff, secure and maintain; gives a base to scale from.',
     'Technical purpose': 'Separation of concerns, security (data tier is not exposed), independent scaling per tier, testability.',
     'Real-world case': 'GreenLeaf\'s online ordering: browser UI, an API with pricing and stock rules, and a SQL database.',
     'Prerequisites': 'Level 1; client/server; HTTP; databases.',
     'Tools': 'draw.io, Mermaid, VS Code, Git.',
     'Step-by-step method': 'Follow the 11 steps in Chapter 11.',
     'Diagram': 'Figure 11.1.',
     'Decision trade-offs': 'Simple and well understood, but the business tier can become a bottleneck or "ball of mud"; for very large systems consider modular or service-based styles.',
     'Expected output': 'greenleaf-three-tier-v1.drawio plus SVG/PNG exports and a one-paragraph explanation.',
     'Verification': 'Rubric score of at least 75; reviewer can trace every arrow to a requirement.',
     'Common mistakes': 'Browser-to-database arrow; no labels; technology names without reasons; missing boundary.',
     'Documentation': 'Files in diagrams/containers/; explanation in docs/target-state/overview.md.',
     'Assignment': 'Redraw for a different client (clinic appointment booking) and list two risks of your design.',
     'Review and approval': 'Reviewer: mentor. Final: Principal Architect samples 1 in 3 diagrams.'}))
    L.append(LESSON('L-03', 'C4 Model: System Context Diagram', {
     'Definition': 'C4 describes software at four zoom levels: Context, Containers, Components, Code. Level 1 shows the system as one box with its users and neighbouring systems.',
     'Business relevance': 'Gives executives and engineers one shared picture; exposes scope and external dependencies early.',
     'Technical purpose': 'Establishes the system boundary and external interfaces before design detail.',
     'Real-world case': 'On a bank project, the context diagram revealed an unmentioned legacy mainframe feed: a hidden dependency found in week 1, not at go-live.',
     'Prerequisites': 'L-01, L-02; stakeholders and external systems concepts.',
     'Tools': 'Structurizr DSL, PlantUML (C4), Mermaid, draw.io.',
     'Step-by-step method': '1) Name the system and one-line purpose. 2) List people (roles, not names). 3) List external systems. 4) Draw relationships with verbs ("Places orders"). 5) Add legend and title. 6) Remove anything that is inside the system.',
     'Diagram': 'Figure 11.2.',
     'Decision trade-offs': 'What to show vs hide: include only what helps decide scope. More detail belongs in Level 2.',
     'Expected output': 'One-page context diagram with legend, plus an interface list (name, direction, owner).',
     'Verification': 'Client can say "yes, that is our world". Every external system has an owner and a contact.',
     'Common mistakes': 'Mixing levels; unlabeled arrows; showing technologies at Level 1; omitting existing systems.',
     'Documentation': 'diagrams/context/ source and export; interface list in specifications/integrations/.',
     'Assignment': 'Create context diagrams for as-is and to-be GreenLeaf systems.',
     'Review and approval': 'Reviewer: Solution Architect; client technical owner confirms.'}))
    L.append(LESSON('L-04', 'Non-Functional Requirements', {
     'Definition': 'Requirements describing how well the system performs: performance, availability, security, scalability, maintainability and more.',
     'Business relevance': 'Customers judge by speed and reliability; failures cost revenue and trust.',
     'Technical purpose': 'NFRs are the main drivers of architectural choices (redundancy, caching, security controls).',
     'Real-world case': 'Requirement "fast" became "95% of checkout requests complete in under 800 ms at 200 concurrent users", which justified caching and a managed database tier.',
     'Prerequisites': 'Level 2 first half.',
     'Tools': 'Requirements template (Appendix E), spreadsheet, load-test calculators.',
     'Step-by-step method': '1) For each quality attribute ask the stakeholder a concrete question. 2) Convert answers to numbers and conditions. 3) Mark CONFIRMED/ASSUMED. 4) Record the measurement method. 5) Agree priority.',
     'Diagram': 'Quality-attribute utility tree (attribute → scenario → measure).',
     'Decision trade-offs': 'Higher availability and lower latency cost more; ask the client to choose with prices shown.',
     'Expected output': 'NFR table with ID, statement, measure, target, source, status.',
     'Verification': 'Every NFR can be tested. A tester could write a pass/fail test from it.',
     'Common mistakes': 'Words like "fast", "secure", "user-friendly" with no numbers; copying 99.99% without cost discussion.',
     'Documentation': 'docs/requirements/non-functional.md.',
     'Assignment': 'Write 12 NFRs for GreenLeaf, covering at least 8 quality attributes.',
     'Review and approval': 'Reviewer: Business Analyst + Solution Architect.'}))
    L.append(LESSON('L-05', 'Architecture Decision Records', {
     'Definition': 'A short, dated document recording one significant decision: context, options, decision and consequences.',
     'Business relevance': 'Preserves why we decided, avoids repeating debates and supports audits and due diligence.',
     'Technical purpose': 'Makes trade-offs explicit and reviewable; links decisions to requirements.',
     'Real-world case': 'A team later wanted to replace the database. The ADR showed the original constraint had vanished, so the change was approved in a day.',
     'Prerequisites': 'L-01.',
     'Tools': 'Markdown, Git, ADR template (Appendix D).',
     'Step-by-step method': '1) Write the problem and constraints. 2) List 2-3 real options. 3) State the decision in one sentence. 4) Write good and bad consequences. 5) Set status Proposed. 6) Get review; set Accepted. 7) Never edit; supersede.',
     'Diagram': 'Figure 12.1 (ADR status lifecycle).',
     'Decision trade-offs': 'More ADRs = more overhead. Write one only for high-impact or hard-to-reverse decisions.',
     'Expected output': 'ADR-0001 in /adr using the template.',
     'Verification': 'A new team member can understand the decision without asking the author.',
     'Common mistakes': 'Only one option listed; hiding downsides; justifying with fashion; editing accepted ADRs.',
     'Documentation': 'adr/0001-title.md, linked from docs/architecture-decisions/.',
     'Assignment': 'Write ADR for "monolith vs microservices" for GreenLeaf (use the example below as a model, not a copy).',
     'Review and approval': 'Reviewer: Software Architect; approver for high-impact: Principal Architect.'}))
    return [H1('Teaching Lessons (15-Part Format)', 'lessons', 12),
    P('Every subject in the syllabus is taught with these fifteen parts. The first five lessons below are complete; trainers create the others with the same template (Appendix A).')] + L + [
    H2('Worked example: Architecture Decision Record'), CODE(ADR_EX, 'markdown'),
    DIAGRAM(D.adr_lifecycle(), '**Figure 12.1** ADR status lifecycle. Accepted ADRs are never edited, only superseded.')]

def projects():
    rows = [
     ['1', 'Basic three-tier application', 'Diagram + 1-paragraph explanation', 'Rubric in Chapter 11 ≥ 75'],
     ['2', 'C4 system context diagram', 'Context diagram + interface list', 'Actors/systems complete; owner confirms'],
     ['3', 'Database architecture', 'ERD, data dictionary, backup and retention plan, RPO/RTO', 'Normalised; keys defined; restore test described'],
     ['4', 'Monolith vs microservices', 'Comparison matrix + recommendation + ADR', 'Criteria weighted before scoring; trade-offs honest'],
     ['5', 'REST API architecture', 'OpenAPI skeleton, versioning, auth, error model, rate limit policy', 'Valid OpenAPI; consistent naming; secured'],
     ['6', 'Cloud-hosted application', 'Deployment + network diagram, cost range with calculator date, DR plan', 'No single point of failure unaccepted; costs sourced'],
     ['7', 'Architecture Decision Record', 'Complete ADR with 2+ options', 'Reviewer can understand it cold'],
     ['8', 'Technical feasibility assessment', 'Report (Appendix F) with conclusion and conditions', 'Evidence per dimension; assumptions labelled'],
     ['9', 'Full architecture proposal for a simulated client', 'Repository with all 20 deliverables (Chapter 8) + 20-minute presentation', 'ARB-style review passed']]
    return [H1('Practical Training Projects', 'projects', 13),
    P('Nine projects, each reviewed by a senior and signed in the trainee log. Use fictional or fully anonymised clients only.'),
    TABLE(['#', 'Project', 'Deliverable', 'Acceptance'], rows, [5, 25, 40, 30]),
    H2('Project 9 assessment: full proposal'),
    TABLE(['Area', 'Weight'], [['Requirements quality and traceability', '15%'], ['Options and decision quality', '20%'], ['Target architecture and diagrams', '20%'], ['Security and operations', '15%'], ['Cost and risk analysis', '10%'], ['Documentation and repository hygiene', '10%'], ['Presentation in business language', '10%']], [70, 30]),
    CALL('gold', 'Certification of competence', 'Completion of the nine projects plus Levels 1-7 qualifies the trainee as **Junior Technical Analyst (cleared)**. Promotion to Solution Architect additionally needs two supervised real engagements and a Principal Architect sign-off.')]

def roadmap90():
    rows = [
     ['1', 'Onboarding, tools, lifecycle overview', 'Setup done; first diagram started', 'Setup checklist'],
     ['2', 'Level 1: software, networks, cloud, security basics', 'Notes; Postman collection; DNS trace', 'Oral quiz'],
     ['3', 'Level 2: stakeholders, interviews, objectives', 'Stakeholder map; mock interview', 'Mentor review'],
     ['4', 'Level 2: requirements, NFRs, acceptance, risks', 'Requirements for GreenLeaf', 'Project 1-2 reviewed; Gate: Levels 1-2'],
     ['5', 'Level 3: architecture vs design; components; coupling', 'Classification table', 'Mentor review'],
     ['6', 'Level 3: layered, modular monolith, microservices, events', 'Project 4 draft', 'Peer review'],
     ['7', 'Level 4: patterns (Chapter 4)', 'Pattern "when NOT to use" notes', 'Quiz'],
     ['8', 'Level 4 continued; ADRs', 'Project 7 (ADR)', 'Gate: Levels 3-4'],
     ['9', 'Level 5: application, database, API', 'Projects 3 and 5', 'Review'],
     ['10', 'Level 5: cloud, network, security, DR', 'Project 6', 'Security Architect review'],
     ['11', 'Level 6: evaluation; cost; feasibility', 'Project 8', 'Gate: Levels 5-6'],
     ['12', 'Level 7: governance; shadow a real engagement', 'Governance notes; shadow log', 'Mentor'],
     ['13', 'Capstone: Project 9 proposal and presentation', 'Full proposal', 'ARB-style panel; final sign-off']]
    return [H1('90-Day Training Roadmap', 'roadmap90', None),
    P('Thirteen weeks, one gate every four weeks. Study about 50% and practise about 50% in the first month; move toward supervised real work in the last month.'),
    TABLE(['Week', 'Focus', 'Deliverable', 'Assessment / gate'], rows, [8, 38, 30, 24]),
    H2('Scorecard'),
    TABLE(['Dimension', 'Evidence', 'Rating (1-5)'], [['Fundamentals', 'Quizzes, notes', ''], ['Analysis quality', 'Requirements, interview write-ups', ''], ['Design reasoning', 'Options, ADRs', ''], ['Diagrams', 'Clarity and correctness', ''], ['Communication', 'Presentation, emails', ''], ['Professionalism', 'Honesty about assumptions, confidentiality', '']], [26, 54, 20])]

def sources():
    S = [
     ('C4 model', 'https://c4model.com/', 'Notation and guidance for software architecture diagrams (Simon Brown)', 'Free'),
     ('Martin Fowler: Architecture', 'https://martinfowler.com/architecture/', 'Essays and patterns on software architecture', 'Free'),
     ('The Open Group: TOGAF', 'https://www.opengroup.org/togaf', 'Enterprise architecture framework; certification', 'Reading free; certification paid; commercial use may need licence'),
     ('ISO', 'https://www.iso.org/', 'Catalogue of ISO standards (e.g. ISO/IEC/IEEE 42010, ISO/IEC 25010, ISO/IEC 27001)', 'Standards are paid'),
     ('Azure Architecture Center', 'https://learn.microsoft.com/azure/architecture/', 'Patterns, reference architectures, Well-Architected guidance', 'Free'),
     ('AWS Well-Architected', 'https://docs.aws.amazon.com/wellarchitected/', 'Framework pillars and review questions', 'Free'),
     ('Google Cloud Architecture Framework', 'https://cloud.google.com/architecture/framework', 'Google Cloud design principles and pillars', 'Free'),
     ('OWASP', 'https://owasp.org/', 'Application security: Top 10, ASVS, cheat sheets, Threat Dragon', 'Free, open'),
     ('NIST Cybersecurity Framework', 'https://www.nist.gov/cyberframework', 'Cybersecurity risk framework (CSF 2.0 published 2024; verify edition)', 'Free'),
     ('UML', 'https://www.uml.org/', 'OMG Unified Modeling Language information', 'Specs free from OMG'),
     ('PlantUML', 'https://plantuml.com/', 'Diagram-as-code reference', 'Open source (check edition)'),
     ('Mermaid', 'https://mermaid.js.org/', 'Diagram-as-code in Markdown', 'Open source'),
     ('draw.io (diagrams.net)', 'https://www.drawio.com/', 'Visual diagram editor', 'Free')]
    X = [
     ('ADR GitHub organisation', 'https://adr.github.io/', 'Architecture Decision Record practice and templates'),
     ('arc42', 'https://arc42.org/', 'Architecture documentation template'),
     ('Structurizr documentation', 'https://docs.structurizr.com/', 'Structurizr DSL and Lite'),
     ('Terraform documentation', 'https://developer.hashicorp.com/terraform/docs', 'Infrastructure as code'),
     ('Postman Learning Center', 'https://learning.postman.com/', 'API tooling'),
     ('OWASP Threat Dragon', 'https://owasp.org/www-project-threat-dragon/', 'Open source threat modelling'),
     ('Microsoft Threat Modeling Tool', 'https://learn.microsoft.com/azure/security/develop/threat-modeling-tool', 'STRIDE-based modelling tool'),
     ('OpenAPI Specification', 'https://spec.openapis.org/oas/latest.html', 'REST API contract standard'),
     ('Google SRE books', 'https://sre.google/books/', 'Reliability and operations practice'),
     ('The Twelve-Factor App', 'https://12factor.net/', 'Cloud-native application principles')]
    return [H1('Official Learning Sources and Standards', 'sources', 14),
    P('These are the **official sources** for this curriculum. Click each to open. Use them as the authority over this handbook when they differ.'),
    CALL('red', 'Link verification status', 'The build environment blocked outbound requests to these sites, so the links could **not** be machine-checked on the build date (4 Nov 2026). They are the official domains supplied in the brief. Run `check-links.sh` from the package (or open each link) and record the date and result in the table before relying on it.'),
    TABLE(['Source', 'Link', 'Use in this curriculum', 'Access / cost', 'Checked (date, initials)'], [[a, f'[{c}]({c})' if False else f'[{b.replace("https://","")}]({b})', c, d, '____'] for a, b, c, d in S], [18, 28, 28, 14, 12]),
    H2('Additional recommended references'),
    TABLE(['Source', 'Link', 'Use'], [[a, f'[{b.replace("https://","")}]({b})', c] for a, b, c in X], [24, 40, 36]),
    H2('Frameworks and standards: status, relevance and when a licence or certification is required'),
    TABLE(['Item', 'Status and relevance (verify current edition)', 'Licence or certification needed?'], [
     ['C4 model', 'Stable, widely used; method not a standard.', 'No.'],
     ['TOGAF Standard', 'The 10th Edition (2022) is current at last knowledge; relevant for enterprise architecture work. Certification structure changed after 9.2: check the Open Group certification portfolio.', 'Reading free. Personal certification is optional and paid via The Open Group. Using TOGAF as the basis of commercial products or training may need a licence: confirm on opengroup.org.'],
     ['ISO/IEC/IEEE 42010', 'International standard for architecture description (current edition 2022 at last knowledge).', 'Purchase required to read the full text. Claiming conformance in client contracts needs the licensed copy.'],
     ['ISO/IEC 25010', 'Product quality model (quality attributes); revised 2023.', 'Purchase required.'],
     ['ISO/IEC 27001', 'Information security management certification (2022 edition).', 'Standard is paid. Organisational certification is performed by accredited certification bodies; we offer readiness support only.'],
     ['NIST CSF', 'Version 2.0 published February 2024.', 'Free; no certification.'],
     ['OWASP Top 10 / ASVS', 'Periodically revised; always cite the edition year.', 'Free; no certification.'],
     ['UML', 'OMG standard (UML 2.5.x), also an ISO/IEC standard.', 'OMG specs free; ISO text paid.'],
     ['AWS / Azure / Google Cloud frameworks', 'Updated frequently; vendor-perspective guidance.', 'Free to read; vendor certifications optional and paid.']], [20, 46, 34]),
    CALL('gold', 'Rule', 'When a client contract names a standard, we obtain the licensed copy and cite the edition. We never quote from a paid standard beyond fair-use short references.')]

def glossary():
    g = [('ADR','Architecture Decision Record.'),('API','Application Programming Interface; a contract between programs.'),('ARB','Architecture Review Board.'),('Availability','Share of time a service works as required.'),('Bounded context','DDD term: a boundary within which a model and its language are consistent.'),('C4','Context, Containers, Components, Code: four zoom levels of architecture diagrams.'),('Coupling','Degree of dependence between parts.'),('CQRS','Command Query Responsibility Segregation.'),('Container (C4)','A separately runnable unit such as a web app, API or database. Not necessarily Docker.'),('DR','Disaster recovery.'),('Event sourcing','Persisting state as a sequence of events.'),('IaC','Infrastructure as Code.'),('NFR','Non-functional requirement.'),('RPO','Recovery point objective: maximum acceptable data loss, measured in time.'),('RTO','Recovery time objective: maximum acceptable downtime.'),('SLA / SLO / SLI','Agreement / objective / indicator of service level.'),('SPOF','Single point of failure.'),('STRIDE','Spoofing, Tampering, Repudiation, Information disclosure, Denial of service, Elevation of privilege.'),('TCO','Total cost of ownership.'),('Technical debt','Shortcuts that increase future cost.'),('Trust boundary','Line across which data moves between different levels of trust.'),('Vendor lock-in','Cost of leaving a supplier.')]
    return [H1('Glossary', 'glossary', None), TABLE(['Term', 'Meaning'], [[a, b] for a, b in g], [24, 76])]

def architecture_diagrams():
    return [H1('Reference Diagrams', 'diagrams', 15),
    P('Reusable reference diagrams for the fictional GreenLeaf Organics case. Treat as teaching examples; adapt to each client\'s evidence.'),
    DIAGRAM(D.c4_container(), '**Figure 15.1** C4 Level 2: containers (training example).'),
    DIAGRAM(D.c4_component(), '**Figure 15.2** C4 Level 3: components of the API application.'),
    DIAGRAM(D.deployment(), '**Figure 15.3** Deployment across two availability zones.'),
    DIAGRAM(D.dataflow(), '**Figure 15.4** Data-flow diagram with trust boundaries.'),
    DIAGRAM(D.network(), '**Figure 15.5** Network architecture and subnets.')]
