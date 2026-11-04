from lib import *
import diagrams as D

def howto():
    return [H1('How to Use This Handbook', 'how-to'),
    P('This handbook is the single operating manual for the **Software Architecture & Technical Consulting Department**. It does two jobs: **(1)** it trains a beginner into a professional who can support client projects under senior architectural supervision, and **(2)** it defines the mandatory 15-stage consulting lifecycle, with owners, reviewers, approval gates and deliverable standards.'),
    CALL('gold', 'Golden rules of the department', ['Architecture is a set of **decisions with reasons**, not a set of diagrams. Every diagram must trace to a requirement and every significant choice to an ADR.',
        'Never present an unverified assumption as a confirmed client fact. Label it **ASSUMPTION** with an owner and a date to confirm.',
        'Do not recommend a pattern or technology because it is popular. Compare at least two viable options and write down the trade-offs.',
        'Juniors prepare research, documentation and diagrams. Juniors **never** approve high-impact decisions.',
        'We never certify a client as "secure" or "compliant" and never guarantee uptime, savings or performance without evidence. We offer readiness support and *reasoned recommendations*.',
        'Prices, tool versions, standards editions and cloud limits change. Verify against the official source before you build, quote or cite.',
        'Every approved architecture decision needs a second person\'s review. No one reviews their own work.']),
    H2('Reading path for a beginner'),
    PILLS(['Part A: Onboard (Ch. 10-11)', 'Part B: Learn Levels 1-7 (Ch. 3-5)', 'Part C: Lifecycle (Ch. 6-8)', 'Part D: Business (Ch. 2, 9)', 'Part E: Projects and sources (Ch. 13-14)']),
    CALL('blue', 'About the examples', 'Client names such as **GreenLeaf Organics** are fictional training examples. Prices are illustrative defaults for training, not quotes. Replace them with approved company rates before use.'),
    CALL('red', 'Document status', 'Document VEL-SA-001 · Version 1.0 · prepared 4 November 2026 · draft for management approval. Standards and tool facts in this handbook were written from the author\'s knowledge; the official links in Chapter 14 could not be machine-checked in the build environment, so open each one and confirm the current edition before relying on it.')]

def portfolio():
    rows = [
     ['Software architecture consulting', 'Independent advice on structure, patterns and risks for a new or existing system', '"We are about to build and want it right."'],
     ['Solution architecture', 'End-to-end design of one solution across apps, data, integration and infrastructure', '"We need a design before we hire developers."'],
     ['Enterprise architecture advisory', 'Capability maps, target state, standards and roadmaps across many systems', '"We have 40 systems and no shared direction."'],
     ['Technical feasibility studies', 'Evidence-based yes/no/with-conditions on technology, cost, skills, time and compliance', '"Can this even be done within budget?"'],
     ['Technology selection', 'Weighted comparison of languages, frameworks, databases, platforms', '"Which stack should we choose?"'],
     ['Application architecture reviews', 'Structured review against quality attributes; findings and fixes ranked by risk', '"It works but nobody dares change it."'],
     ['Cloud architecture design', 'Landing zone, network, compute, data, DR and cost design on AWS / Azure / GCP', '"Move us to the cloud without surprises."'],
     ['Database architecture', 'Logical and physical models, engine choice, scaling, backup and migration', '"Reports take minutes; data is inconsistent."'],
     ['API architecture', 'REST/event API style, contracts (OpenAPI), versioning, security, governance', '"Partners integrate in ten different ways."'],
     ['Microservices assessment', 'Is a split justified? Boundaries, data ownership, operational readiness', '"Everyone says we need microservices."'],
     ['Monolith modernisation', 'Incremental path (modular monolith, strangler pattern) with risk-ranked steps', '"Release takes 3 weeks and breaks things."'],
     ['Legacy system assessment', 'Inventory, risk, cost-of-ownership, retire / retain / rehost / refactor / replace', '"The only developer who knows it is leaving."'],
     ['System integration architecture', 'Integration patterns, canonical data, error handling, monitoring', '"Systems disagree and re-key data by hand."'],
     ['Security architecture coordination', 'Trust boundaries, threat model, control mapping; coordinates with security specialists', '"A customer asked about our security."'],
     ['Scalability and performance reviews', 'Load model, bottleneck analysis, capacity plan, test strategy', '"Black Friday took us down."'],
     ['Technical due diligence', 'Independent assessment of a target\'s technology, debt, team and risk for investors', '"We are acquiring / investing; what are we buying?"'],
     ['Architecture documentation', 'C4 diagrams, ADRs, interface specs, runbooks and handover packs', '"No one knows how it fits together."'],
     ['Digital transformation roadmaps', 'Phased, costed roadmap tying business goals to technology change', '"Leadership wants a 3-year plan."']]
    return [H1('Commercial Service Portfolio', 'portfolio', 1),
    P('Eighteen commercially deliverable services. Each can be sold alone or combined. Every service ends in a **reviewed written deliverable**, never only a conversation.'),
    TABLE(['Service', 'What we deliver', 'Typical client trigger'], rows, [24, 46, 30]),
    CALL('red', 'Compliance and assurance wording', 'We say "security architecture coordination" and "compliance readiness support." We do not certify, audit-opine or guarantee. Formal certification (for example ISO/IEC 27001) is performed by accredited bodies. Technical due diligence reports state their scope and evidence limits.')]

def roles():
    rows = [
     ['Junior Technical Analyst', 'Research, notes, diagrams, documentation, draft ADRs, comparison tables', 'Prepares only. Cannot approve architecture decisions or send client advice unreviewed.', 'Months 0-12'],
     ['Business Analyst', 'Stakeholder interviews, process maps, requirements, acceptance criteria, traceability', 'Owns the requirements baseline; signs requirement completeness.', 'Level 2 mastered'],
     ['Solution Architect', 'End-to-end design for one engagement; alternatives, target architecture, handover', 'Approves medium-impact design; proposes high-impact decisions.', 'Level 5 mastered'],
     ['Software Architect', 'Application structure, patterns, code-level standards, API and data design', 'Approves application-level design; reviews implementation compliance.', 'Level 4-5'],
     ['Cloud Architect', 'Cloud landing zone, networking, DR, cost, IaC standards', 'Approves cloud design and cost model.', 'Level 5 + cloud certs advised'],
     ['Security Architect', 'Threat models, trust boundaries, control mapping, security review', 'Veto on unresolved high security risks; signs security section.', 'Level 5-6'],
     ['Enterprise Architect', 'Cross-system capability maps, standards, roadmaps, governance', 'Approves cross-programme direction and standards.', 'Level 7'],
     ['Principal Architect', 'Department technical authority, review board chair, due-diligence sign-off', 'Final approval of high-impact decisions and client-facing architecture.', 'Senior'],
     ['Project Manager', 'Scope, schedule, budget, risks, change control, client communication', 'Approves scope change and commercial terms with the sponsor.', 'Any']]
    return [H1('Department Structure and Roles', 'roles', 2),
    P('A junior employee may prepare research, documentation and diagrams but **must not independently approve high-impact architecture decisions**. High-impact means any decision that is costly to reverse, affects security or data protection, changes cost by more than 10%, creates vendor lock-in, or touches more than one team.'),
    TABLE(['Role', 'Core responsibilities', 'Decision authority', 'Typical entry'], rows, [18, 34, 32, 16]),
    DIAGRAM(D.roles_ladder(), '**Figure 2.1** Approval authority grows with seniority.'),
    H2('Decision-impact matrix'),
    TABLE(['Impact', 'Examples', 'Who may approve'], [
      ['Low', 'Diagram styling, naming, tool template choice', 'Any architect or the author\'s reviewer'],
      ['Medium', 'Library choice, module boundaries, API style within an approved architecture', 'Solution or Software Architect'],
      ['High', 'Database engine, cloud provider, monolith vs microservices, security model, DR strategy, any commitment above 10% cost change', 'Principal Architect plus client sponsor; recorded as an ADR']], [14, 52, 34]),
    H2('RACI for a standard engagement'),
    P('R = does the work · A = accountable and approves · C = consulted · S = supports · I = informed.'),
    P('R = does the work · A = accountable and approves · C = consulted · S = supports · I = informed.')]
    TABLE(['Activity', 'Junior', 'BA', 'Solution Arch.', 'Security Arch.', 'Principal', 'PM'], [
      ['Discovery interviews', 'S', 'R', 'A', 'C', 'I', 'C'],
      ['Current-state documentation', 'R', 'S', 'A', 'C', 'I', 'I'],
      ['Requirements baseline', 'S', 'R', 'A', 'C', 'I', 'C'],
      ['Options and target architecture', 'S', 'C', 'R', 'C', 'A', 'I'],
      ['Security architecture section', 'S', 'I', 'C', 'R', 'A', 'I'],
      ['Cost and risk analysis', 'S', 'C', 'R', 'C', 'A', 'C'],
      ['Client presentation', 'S', 'C', 'R', 'C', 'A', 'C'],
      ['Scope change', 'I', 'C', 'C', 'I', 'C', 'A/R']], [26, 10, 10, 15, 15, 12, 12]),

def levels():
    def L(title, dur, rows): return [H2(title + f' ({dur})'), TABLE(['Topic', 'Simple definition', 'Why it matters', 'Exercise and check'], rows, [17, 28, 27, 28])]
    b = [H1('Learning Roadmap: Levels 1-7', 'roadmap', 3),
     P('For **every** subject the trainee receives the 15-part teaching pack (Chapter 12). The tables below are the syllabus. A level is passed only when all exercises are demonstrated and reviewed by a Principal or Solution Architect.')]
    b += L('Level 1 — Software Fundamentals', 'weeks 1-2', [
     ['Software lifecycle', 'The stages software passes through: idea, requirements, design, build, test, release, operate, retire.', 'Architecture decisions are cheapest at the start and costliest after release.', 'Map a recent app you use to the lifecycle stages.'],
     ['Requirements', 'A statement of what the system must do or be.', 'Wrong requirements produce the right system for the wrong problem.', 'Rewrite 5 vague wishes as testable requirements.'],
     ['Frontend and backend', 'Frontend runs on the user\'s device; backend runs on servers and holds rules and data.', 'Security checks must live in the backend; the browser can be tampered with.', 'Open browser dev tools; identify requests the page makes.'],
     ['Databases', 'Organised, durable storage with rules for access.', 'Data outlives code; bad data design is the hardest to fix.', 'Design two related tables (customer, order); explain the key.'],
     ['APIs', 'A contract by which one program asks another to do something.', 'APIs are how systems integrate; changing them breaks others.', 'Call a public API with Postman; read the status code and JSON.'],
     ['Servers', 'Computers (physical or virtual) that run software 24x7.', 'Capacity, location and failure of servers drive availability.', 'List what happens if the only server loses power.'],
     ['Networks', 'How devices exchange data: IP, DNS, ports, HTTP/S, firewalls.', 'Most "the site is down" faults are network faults.', 'Trace DNS for a domain with nslookup/dig; explain each answer.'],
     ['Cloud fundamentals', 'Renting compute, storage and services on demand (IaaS, PaaS, SaaS).', 'Cloud shifts cost from capital to operating and changes failure modes.', 'Compare IaaS/PaaS/SaaS for hosting a website; give one example each.'],
     ['Security fundamentals', 'Confidentiality, integrity, availability; authentication vs authorisation.', 'Security is a design property, not a patch.', 'Explain CIA triad using an online bank example.']])
    b += L('Level 2 — Business Analysis', 'weeks 3-4', [
     ['Stakeholder identification', 'People who affect or are affected by the system.', 'Missing one stakeholder is the commonest cause of late rework.', 'Build a power/interest grid for the GreenLeaf case.'],
     ['Discovery interviews', 'Structured conversations to learn goals, pain and constraints.', 'Clients describe solutions; we must uncover problems.', 'Run a 30-minute mock interview with the questionnaire (Appendix C).'],
     ['Business objectives', 'Measurable outcomes the organisation wants.', 'Architecture is justified by objectives, not technology taste.', 'Turn "grow online" into 3 measurable objectives.'],
     ['Functional requirements', 'What the system does (features, rules, data).', 'Basis for scope, estimates and tests.', 'Write 10 requirements in "The system shall..." form with IDs.'],
     ['Non-functional requirements', 'How well it does it: performance, availability, security, etc.', 'NFRs drive architecture more than features do.', 'Give each NFR a number: "p95 < 500 ms at 200 users".'],
     ['Business process mapping', 'Diagram of how work flows today and tomorrow.', 'Reveals manual steps and hand-offs that systems must support.', 'Draw order-to-delivery as a swimlane.'],
     ['User journeys', 'A user\'s steps, thoughts and pain points to reach a goal.', 'Surfaces missing features and failure paths.', 'Map the checkout journey including a failed payment.'],
     ['Constraints', 'Fixed limits: budget, date, skills, law, existing systems.', 'Constraints rule options out early.', 'List 8 constraints; mark each confirmed or assumed.'],
     ['Risks', 'Uncertain events that could harm objectives.', 'Unmanaged risk becomes schedule and cost overrun.', 'Score 6 risks on likelihood x impact with owners.'],
     ['Acceptance criteria', 'Testable conditions that define "done".', 'Prevents disputes at handover.', 'Write Given/When/Then for 3 requirements.']])
    b += L('Level 3 — Software Architecture Fundamentals', 'weeks 5-6', [
     ['Architecture vs design', 'Architecture = significant, hard-to-change decisions; design = detail inside them.', 'We spend review time where change is costly.', 'Classify 10 decisions as architectural or design, with reasons.'],
     ['Components and modules', 'Replaceable parts with a clear responsibility.', 'Enable parallel work and change isolation.', 'Split a food-ordering app into modules.'],
     ['Interfaces and dependencies', 'The agreed surface between parts; who needs whom.', 'Most failures happen at boundaries.', 'Draw a dependency graph; find a cycle and fix it.'],
     ['Separation of concerns', 'Each part handles one kind of thing.', 'Makes change local and testing simple.', 'Refactor a "god class" description into 4 concerns.'],
     ['Coupling and cohesion', 'Coupling: how much parts depend on each other. Cohesion: how related a part\'s content is.', 'Aim for low coupling, high cohesion.', 'Score two designs for coupling; justify.'],
     ['Layered architecture', 'Stack of layers; each uses the one below.', 'Simple, well understood; default for many business apps.', 'Draw the three-tier diagram (Chapter 11).'],
     ['Modular monolith', 'One deployable unit with strict internal module boundaries.', 'Often the best starting point; cheap to run.', 'Define module boundaries and allowed dependencies.'],
     ['Microservices', 'Independently deployable services owning their data.', 'Gives team autonomy; costs distribution complexity.', 'Complete Training Project 4 (comparison).'],
     ['Event-driven systems', 'Parts communicate by publishing and reacting to events.', 'Decouples producers from consumers.', 'Model "order placed" with 3 consumers; list failure cases.'],
     ['Distributed systems', 'Systems spread over networked machines that can fail independently.', 'Network is unreliable; consistency, latency and failure must be designed.', 'Explain CAP trade-off with a shopping-cart example.']])
    b += L('Level 4 — Architecture Patterns', 'weeks 7-8', [
     ['See Chapter 4', 'Ten patterns with benefits, costs, complexity and trade-offs.', 'Patterns are tools; wrong tool = wasted money.', 'For each pattern, write one situation where you would NOT use it.']])
    b += L('Level 5 — Technical Architecture', 'weeks 9-10', [
     ['Application architecture', 'Structure of the software: layers, modules, runtime.', 'Determines change cost.', 'Produce C4 L2-L3 for the case study.'],
     ['Database architecture', 'Data model, engine, replication, backup, scaling.', 'Data is the longest-lived asset.', 'Design ERD + backup plan (Project 3).'],
     ['API architecture', 'Style (REST, events), contracts, versioning, auth.', 'Governs integration cost.', 'Write an OpenAPI skeleton (Project 5).'],
     ['Integration architecture', 'How systems exchange data reliably.', 'Prevents re-keying and silent data loss.', 'Design retry + dead-letter handling.'],
     ['Cloud architecture', 'Regions, zones, compute, storage, managed services.', 'Drives availability and cost.', 'Complete Project 6.'],
     ['Network architecture', 'Subnets, routing, gateways, firewalls, DNS.', 'Limits blast radius of attack.', 'Draw the network diagram with trust zones.'],
     ['Security architecture', 'Identity, encryption, trust boundaries, logging.', 'Required for client trust.', 'STRIDE-analyse one data flow.'],
     ['Deployment architecture', 'How software reaches and runs in environments.', 'Determines release risk.', 'Describe dev/test/prod and promotion steps.'],
     ['Monitoring architecture', 'Logs, metrics, traces, alerts, dashboards.', 'You cannot operate what you cannot see.', 'Choose 5 golden signals and alert thresholds.'],
     ['Disaster recovery', 'Plan to restore service after major failure (RTO/RPO).', 'Backups untested are hopes.', 'Define RTO/RPO for 3 tiers of system.']])
    b += L('Level 6 — Architecture Evaluation', 'weeks 11-12', [
     ['Quality attributes', 'Scalability, availability, reliability, performance, security, maintainability, interoperability, portability.', 'These are the criteria for judging alternatives.', 'See evaluation table in Chapter 4.'],
     ['Cost and complexity', 'Total cost of ownership, plus cognitive and operational complexity.', 'Cheap to build can be costly to run.', 'Compute 3-year TCO for two options.'],
     ['Vendor lock-in', 'Cost of leaving a provider or product.', 'Affects negotiating power and risk.', 'Rate lock-in 1-5 and write an exit plan.'],
     ['Operational capability', 'Whether the client can actually run the design.', 'A brilliant design nobody can operate fails.', 'Skills-gap table for the client team.']])
    b += L('Level 7 — Architecture Governance', 'weeks 13+', [
     ['Architecture Decision Records', 'Short documents capturing one decision, context, options and consequences.', 'Preserve the "why".', 'Write ADR-001 (Project 7).'],
     ['Technical standards', 'Approved defaults (languages, patterns, naming, security baselines).', 'Reduce variation and review cost.', 'Draft a 1-page API standard.'],
     ['Review boards and design reviews', 'Scheduled expert review of designs.', 'Catches high-impact flaws early.', 'Run a mock design review with a checklist.'],
     ['Change management', 'Controlled handling of changes to scope and architecture.', 'Stops silent drift.', 'Process one change request end to end.'],
     ['Technical debt', 'Accumulated shortcuts that raise future cost.', 'Must be visible and managed.', 'Create a debt register with interest and priority.'],
     ['Architecture compliance', 'Checking implementation matches approved design.', 'Closes the loop from design to delivery.', 'Perform a compliance review on a sample repo.'],
     ['Documentation lifecycle', 'Create, review, publish, update, retire documents.', 'Stale documents mislead.', 'Define owners and review cadence for each artefact.']])
    return b

def patterns():
    rows = [
     ['MVC / MVP / MVVM', 'Separates data (model), screen (view) and input handling (controller).', 'Clear UI structure; testable logic; widely known.', 'Controllers can bloat; not a whole-system architecture.', 'Low', 'Server-rendered web apps, desktop apps.', 'Systems whose complexity is in the domain rules, not the UI.'],
     ['Layered / N-tier', 'Presentation, business, data layers each using the one below.', 'Simple; easy onboarding; fits CRUD.', 'Can become a "big ball" with leaky layers; changes ripple vertically.', 'Low', 'Typical business apps, first version.', 'Highly independent scaling needs per feature.'],
     ['Clean Architecture', 'Concentric rings; dependencies point inward to business rules.', 'Framework-independent core; testable; long-lived.', 'More files and mapping code; learning curve.', 'Medium', 'Long-lived, rule-heavy systems.', 'Short-lived prototypes or simple CRUD.'],
     ['Hexagonal (ports and adapters)', 'Core exposes ports; adapters connect UI, DB, services.', 'Swap infrastructure easily; test core in isolation.', 'Indirection; discipline needed.', 'Medium', 'Many integrations; need testability.', 'Tiny apps where indirection exceeds value.'],
     ['Domain-Driven Design', 'Model software around the business domain with bounded contexts.', 'Shared language with business; sound boundaries.', 'Needs domain experts\' time; heavy for simple domains.', 'High', 'Complex domains; many teams.', 'Simple data entry systems.'],
     ['Repository pattern', 'Abstraction over data access as a collection.', 'Decouples logic from storage; testable.', 'Can be redundant over modern ORMs; leaky abstractions.', 'Low', 'When storage may change or tests need fakes.', 'Where the ORM already gives the same abstraction.'],
     ['CQRS', 'Separate models for commands (writes) and queries (reads).', 'Optimise reads and writes independently; clearer intent.', 'Two models to maintain; eventual consistency if stores split.', 'High', 'Read-heavy vs write-heavy asymmetry; reporting needs.', 'Plain CRUD with similar read/write shapes.'],
     ['Event sourcing', 'Store every state change as an event; derive state by replay.', 'Full audit trail; time travel; rebuild projections.', 'Steep learning; event versioning; query complexity.', 'Very high', 'Audit-critical domains (finance, ledgers).', 'Most business apps; a normal audit table suffices.'],
     ['Message-driven architecture', 'Components communicate via asynchronous messages through a broker.', 'Loose coupling; load levelling; resilience to downtime.', 'Debugging; ordering and duplicates; broker operations.', 'Medium-High', 'Spiky load; long tasks; integration.', 'Simple request/response needing immediate answers.'],
     ['API gateway', 'Single entry point handling routing, auth, rate limits.', 'Central security and policy; hides internals.', 'Single point of failure and bottleneck if poor; extra hop.', 'Medium', 'Many services/clients; public APIs.', 'One small app with one client.'],
     ['Backend-for-frontend (BFF)', 'A backend tailored to each client type (web, mobile).', 'Client-specific payloads; independent evolution.', 'More services; duplicated logic.', 'Medium', 'Distinct client needs.', 'One client type.']]
    return [H1('Architecture Patterns and Evaluation', 'patterns', 4),
    P('Explain **benefits, costs, complexity and trade-offs** of every pattern. **Do not recommend a pattern because it is popular.** A pattern is justified only by a requirement it satisfies better than the simpler alternative.'),
    CALL('gold', 'The simplicity test', 'Before proposing any pattern, ask: (1) which requirement does it serve? (2) what is the simplest option that could meet it? (3) what new failure modes and skills does it add? (4) can the client team operate it? If you cannot answer all four, do not propose it.'),
    TABLE(['Pattern', 'What it is', 'Benefits', 'Costs / trade-offs', 'Complexity', 'Use when', 'Avoid when'], rows, [12, 17, 15, 17, 8, 16, 15], 'dense'),
    DIAGRAM(D.hexagonal(), '**Figure 4.1** Hexagonal architecture (ports and adapters).'),
    DIAGRAM(D.mono_vs_micro(), '**Figure 4.2** Modular monolith versus microservices.'),
    DIAGRAM(D.eventdriven(), '**Figure 4.3** Event-driven architecture.'),
    H2('Monolith or microservices? Decision guide'),
    TABLE(['Question', 'Points to modular monolith', 'Points to microservices'], [
      ['Team size', 'Under ~15 engineers or one team', 'Many autonomous teams'],
      ['Release independence', 'Releases can be coordinated', 'Teams must deploy independently'],
      ['Scaling', 'Load is uniform', 'Specific parts need very different scaling'],
      ['Domain knowledge', 'Boundaries still unclear', 'Boundaries are stable and well known'],
      ['Operations maturity', 'Little DevOps experience', 'CI/CD, observability and on-call already mature'],
      ['Consistency', 'Needs strong transactions across features', 'Eventual consistency acceptable']], [24, 38, 38]),
    CALL('blue', 'Default advice', 'For most clients, start with a **modular monolith** and extract services only where a measured need exists. State this reasoning in the ADR so the decision survives later fashion changes.'),
    H2('Architecture evaluation criteria (Level 6)'),
    TABLE(['Attribute', 'Ask', 'Evidence / measure'], [
      ['Scalability', 'Can it handle 10x load by adding resources?', 'Load test; bottleneck analysis; capacity model'],
      ['Availability', 'What % uptime is needed and what is the failure domain?', 'Target SLO (e.g. 99.9% = about 8.8 h downtime/year); redundancy design'],
      ['Reliability', 'Does it behave correctly under faults?', 'Failure-mode analysis; retry/timeout design; chaos tests'],
      ['Performance', 'Response time and throughput at expected load?', 'p95/p99 latency targets; profiling'],
      ['Security', 'Which threats are mitigated and which accepted?', 'Threat model; control mapping; test reports'],
      ['Maintainability', 'How costly is a typical change?', 'Module coupling; test coverage; change lead time'],
      ['Interoperability', 'Can it exchange data with required systems?', 'Open standards; contract tests'],
      ['Portability', 'Can it move between hosts/clouds?', 'Containers; standard interfaces; exit cost'],
      ['Cost', 'Build + run + change cost over 3 years?', 'TCO model; cloud calculator'],
      ['Complexity', 'How many moving parts and skills required?', 'Component count; skills matrix'],
      ['Vendor lock-in', 'Cost and time to leave?', 'Proprietary service inventory; exit plan'],
      ['Operational capability', 'Can the client run and support it?', 'Skills gap; runbooks; on-call model']], [18, 38, 44]),
    CALL('red', 'Availability numbers', '99.9% availability allows about 8.76 hours of downtime per year (0.001 x 8,760 h). 99.99% allows about 52.6 minutes. Calculate, show the formula, and never promise a number the design and budget cannot support.')]

def tools():
    return [H1('Tools and When to Use Each', 'tools', 5),
    P('Choose the tool for the **audience and the artefact**. Diagram-as-code is best for version control and review; visual editors are best for workshops and stakeholder polish.'),
    TABLE(['Tool', 'Best for', 'Strengths', 'Watch out for', 'Licence / cost*'], [
     ['draw.io (diagrams.net)', 'Network, deployment, cloud and free-form diagrams', 'Free; large icon libraries; stores .drawio/.svg in Git', 'Layout drift; no semantic model', 'Open source, free'],
     ['PlantUML', 'Sequence, class, component, C4 (via C4-PlantUML)', 'Text-based, diff-friendly; powerful', 'Syntax learning; automatic layout limits', 'Open source (verify edition)'],
     ['Mermaid', 'Flowcharts, sequence, ER, simple C4 in Markdown', 'Renders natively in GitHub/GitLab Markdown', 'Limited layout control; C4 is experimental', 'Open source, MIT'],
     ['C4 model', 'Notation to describe software at 4 zoom levels', 'Shared vocabulary for technical and business readers', 'Is a method, not a tool; keep levels consistent', 'Free to use (c4model.com)'],
     ['Structurizr (DSL / Lite)', 'One model, many C4 views', 'Model-based consistency; DSL in Git', 'Hosted-service availability and pricing change: verify', 'DSL/Lite open source; check hosted plans'],
     ['Figma / FigJam', 'Client presentations, workshops, visual polish', 'Collaboration; beautiful slides', 'Not source-controlled; not a model', 'Freemium; paid plans'],
     ['GitHub', 'Version control, reviews, ADRs, docs-as-code, issues', 'Pull-request review; Actions; Markdown rendering', 'Do not store secrets or client confidential data in public repos', 'Free and paid plans'],
     ['VS Code', 'Editing Markdown, PlantUML, Mermaid, IaC, API specs', 'Extensions; Git integrated; live preview', 'Extension trust and licences', 'Editor free'],
     ['Cloud architecture tools', 'Vendor reference diagrams, cost calculators, well-architected tools', 'Official icons and patterns', 'Vendor bias; prices change', 'Calculators free'],
     ['Threat modelling tools', 'STRIDE analysis, data-flow threat lists (OWASP Threat Dragon, Microsoft Threat Modeling Tool)', 'Structured threat discovery', 'Output needs expert review', 'Check each tool\'s licence'],
     ['Database modelling tools', 'ERDs and schema design (dbdiagram, pgModeler, MySQL Workbench, draw.io)', 'Generates SQL; visual relations', 'Model drift from live schema', 'Varies; verify'],
     ['Postman', 'Exploring and documenting APIs; collections; mock servers', 'Quick testing; sharing collections', 'Account/team limits; secrets in collections', 'Freemium'],
     ['Terraform (documentation)', 'Reading and writing IaC, reviewing plans', 'Declarative infrastructure; large provider docs', 'Licence changes: evaluate OpenTofu; verify current terms', 'Check licence'],
     ['Architecture documentation platforms', 'arc42 templates, MkDocs, Docusaurus, Confluence, Notion', 'Search, structure, publishing', 'Documents rot without owners', 'Varies']], [15, 22, 22, 22, 19], 'dense'),
    P('*Licences and prices change. Verify on the vendor\'s official site and record the check date in the project\'s tool register.'),
    H2('Which diagram for which question?'),
    TABLE(['Question to answer', 'Diagram', 'Audience', 'Preferred tool'], [
     ['What is the system and who/what uses it?', 'C4 Level 1 System Context', 'Everyone, including executives', 'Structurizr, PlantUML, draw.io'],
     ['What deployable parts exist?', 'C4 Level 2 Container', 'Technical leads, ops, security', 'Structurizr, PlantUML'],
     ['How is one container structured?', 'C4 Level 3 Component', 'Developers', 'PlantUML, Mermaid'],
     ['Where does it run?', 'Deployment diagram', 'Cloud, ops, security', 'draw.io'],
     ['How does data move and where are the trust boundaries?', 'Data-flow diagram', 'Security, data protection', 'draw.io, Threat Dragon'],
     ['How do these steps interact over time?', 'Sequence diagram', 'Developers, integration partners', 'Mermaid, PlantUML'],
     ['How is data structured?', 'Entity-relationship diagram', 'Developers, DBAs', 'dbdiagram, draw.io, Mermaid'],
     ['How is the network segmented?', 'Network diagram', 'Network, security, ops', 'draw.io'],
     ['What does the client approve?', 'Presentation deck', 'Executives', 'Figma, slides']], [32, 24, 22, 22])]
