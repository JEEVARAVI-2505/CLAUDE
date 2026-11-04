from lib import *
import diagrams as D

STAGES = [
 ('01','Lead Qualification','Decide whether the client is a good fit before spending engineering time.',
  ['Record organisation, industry, size, systems and business objective in the lead form.','Hold a 30-minute qualification call with the decision-maker.','Check fit with our services, conflicts of interest, budget range and timeline.','Score the opportunity (fit, risk, red flags); record unknowns with owners.'],
  ['Decision-maker identified','Business problem stated in the client\'s words','Budget range and date known or marked unknown','Red flags noted (e.g. "guaranteed 100% uptime on a tiny budget")','NDA requested if confidential material is shared'],
  'Lead qualification record and opportunity assessment','Sales / Project Manager','Principal Architect','All fields filled or marked "unknown" with an owner; proceed/decline recorded.'),
 ('02','Discovery','Learn goals, pain, people and constraints by interviewing stakeholders.',
  ['Agree discovery plan, stakeholder list and interview schedule.','Send the questionnaire (Appendix C) in advance.','Run interviews and workshops; two people attend (lead + scribe).','Collect documents, diagrams, access to non-production systems.','Write up notes within 24 hours; confirm facts back to the client.'],
  ['All stakeholder groups interviewed','Facts tagged CONFIRMED / ASSUMPTION / UNKNOWN','Documents received and logged','Open questions list with owners and dates','Sensitive data handling agreed'],
  'Discovery notes, stakeholder map, open-questions log','Business Analyst','Solution Architect','Client confirms the discovery summary in writing; every assumption has an owner.'),
 ('03','Current-State Assessment','Document what exists today: applications, integrations, infrastructure, data, security and operations.',
  ['Inventory applications, owners, technologies, versions and support status.','Draw C4 L1/L2 for the as-is system; list integrations and data flows.','Document infrastructure, hosting, backup, monitoring and DR as they actually are.','Record security posture (access, secrets, patching, logging) factually, without judgement.','Capture performance, incident and cost history.'],
  ['As-is C4 L1 and L2 drawn and validated by the client','Integration list complete','Hosting and cost known','Backup tested? (yes/no/unknown)','Support and licence end dates listed'],
  'Current-state architecture document and inventories','Junior Technical Analyst (draft) / Solution Architect','Principal Architect','Client technical owner confirms diagrams match reality; gaps flagged.'),
 ('04','Problem Definition','State the technical limitations, risks, dependencies and business impact.',
  ['Group findings into problems; link each to a business objective.','Quantify impact (cost, time, risk) with evidence or marked estimate.','Identify root causes, not symptoms.','Agree a one-page problem statement with the sponsor.'],
  ['Each problem has evidence','Business impact stated','Dependencies listed','Out-of-scope items stated','Sponsor has read the statement'],
  'Problem statement and risk/impact summary','Solution Architect','Principal Architect','Sponsor signs "this is the problem we are solving".'),
 ('05','Requirements Definition','Document functional and non-functional requirements, constraints and acceptance criteria.',
  ['Derive requirements from objectives and journeys; give each a unique ID.','Quantify NFRs (e.g. availability, p95 latency, RPO/RTO, data volume).','Separate MUST / SHOULD / COULD; list constraints and assumptions.','Write acceptance criteria; build a traceability matrix to objectives.'],
  ['Every requirement is testable','NFRs have numbers','Compliance and data-protection needs stated','Traceability matrix complete','Priorities agreed'],
  'Requirements specification (Appendix E)','Business Analyst','Solution Architect + client owner','Client signs requirements baseline; changes follow change control.'),
 ('06','Feasibility Assessment','Evaluate technology, cost, skills, timeline, compliance and operational feasibility.',
  ['Define feasibility questions from the riskiest requirements.','Research options; run small proofs of concept where uncertainty is high.','Rate technical, economic, schedule, legal/compliance, operational and skills feasibility.','Conclude: feasible / feasible with conditions / not feasible, with evidence.'],
  ['Each dimension rated with evidence','Spikes timeboxed and recorded','Skills gap identified','Licence and legal checks done','Assumptions listed'],
  'Technical feasibility report (Appendix F)','Solution Architect','Principal Architect','Conclusion and conditions accepted by sponsor.'),
 ('07','Architecture Alternatives','Develop at least two viable options (plus "do nothing" baseline) and compare trade-offs.',
  ['Define decision criteria and weights with the client.','Design 2-3 viable options at C4 L1/L2.','Score options in the technology comparison matrix (Appendix G).','Run a sensitivity check: does the winner change if weights change?'],
  ['At least two viable options plus baseline','Criteria weights agreed before scoring','Cost, risk, lock-in and complexity compared','Recommendation reasoned, not just highest score','Rejected options explained'],
  'Options paper and comparison matrix','Solution Architect','Principal Architect','Sponsor understands the trade-offs and agrees the preferred option.'),
 ('08','Target Architecture','Design the recommended solution in enough detail to build, secure and operate.',
  ['Detail the chosen option: containers, components, data, integration, security, deployment, monitoring, DR.','Map each requirement to the design element that satisfies it.','Threat-model the main data flows.','Identify residual risks and open decisions.'],
  ['All MUST requirements traced','Trust boundaries shown','Failure handling and recovery designed','Observability designed','Open decisions listed with owners'],
  'Target-state architecture and threat model','Solution Architect with Cloud/Security Architects','Principal Architect','Peer review complete with no unresolved high-severity findings.'),
 ('09','Architecture Documentation','Produce diagrams, component descriptions, interfaces, data flows and decision records.',
  ['Complete C4 L1-L3, deployment, data-flow and network diagrams.','Write component and interface specifications.','Write ADRs for every high-impact decision (Appendix D).','Assemble in the repository structure (Chapter 7).'],
  ['Diagrams consistent with each other','Every diagram has title, legend, date, version','ADRs numbered and linked','Glossary included','Docs-as-code merged via reviewed pull request'],
  'Architecture documentation pack','Junior Technical Analyst (draft) / Architects','Principal Architect','Review checklist (Appendix J) passed.'),
 ('10','Cost and Risk Analysis','Estimate infrastructure, development, licences, staffing, maintenance and operational risks.',
  ['Estimate build effort by component with ranges (best/likely/worst).','Estimate run costs using official cloud calculators; record date and region.','Add licences, support, training, and contingency.','Build the risk register: likelihood x impact, mitigation, owner.'],
  ['One-time and recurring costs separated','Currency and tax basis stated','Calculator links and dates recorded','Contingency justified','Top 10 risks have owners'],
  'Cost estimate (Appendix K) and risk register','Solution Architect / Cloud Architect','Project Manager + Principal Architect','Estimate reviewed by someone who did not prepare it.'),
 ('11','Client Presentation','Explain the solution and its trade-offs in business language.',
  ['Build the deck from Appendix L: problem, options, recommendation, cost, risk, roadmap, decisions needed.','Rehearse with a senior reviewer.','Present; capture questions and decisions.','Send written follow-up within 24 hours.'],
  ['No unexplained jargon','Trade-offs shown honestly','Decisions needed from the client are explicit','Assumptions stated on a slide','Appendix of detail available'],
  'Presentation and minutes','Solution Architect','Principal Architect','Client confirms understanding; objections logged and answered.'),
 ('12','Approval','Obtain formal architecture approval from the client and our review board.',
  ['Submit the pack to the Architecture Review Board (ARB).','Resolve findings; update ADRs.','Collect signatures on approval record (client sponsor, our Principal Architect).','Baseline the approved version in Git with a tag.'],
  ['ARB decision recorded','Conditions listed with owners','Signatures obtained','Version tagged','Change process explained'],
  'Signed architecture approval record','Principal Architect','Client sponsor + ARB chair','Written approval of a specific version; no unresolved critical findings.'),
 ('13','Engineering Handover','Give development, cloud, data, API and security teams implementation specifications.',
  ['Run the handover workshop per team.','Issue specifications, acceptance criteria, environments and standards.','Hand over backlog seeds and non-functional test targets.','Confirm each team acknowledges scope and open risks.'],
  ['Handover checklist (Appendix M) complete','Specs linked to requirements','Each team has a named contact','Definition of done agreed','Escalation path defined'],
  'Handover pack and acknowledgements','Solution Architect','Engineering leads','Each receiving team signs acknowledgement.'),
 ('14','Architecture Governance','Review implementation against the approved architecture throughout delivery.',
  ['Schedule design reviews at milestones (e.g. after each epic).','Check code, infrastructure and pipelines against standards and ADRs.','Log deviations; approve, reject or raise a change request.','Maintain the technical-debt register.'],
  ['Review cadence agreed','Deviation log maintained','Changes recorded as new/superseding ADRs','Debt visible with owners','Docs updated with each change'],
  'Governance reports and updated ADRs','Software / Solution Architect','Principal Architect / ARB','No unapproved deviation remains open at go-live.'),
 ('15','Post-Implementation Review','Evaluate actual performance, cost, reliability and technical debt after go-live.',
  ['Wait for a meaningful operating period (e.g. 60-90 days).','Compare actual vs planned: cost, performance, availability, incidents.','Review debt and lessons learned; update templates and standards.','Present results; agree improvement actions or a retainer.'],
  ['Metrics collected from real monitoring','Variances explained','Lessons recorded in department knowledge base','Debt register updated','Client feedback captured'],
  'Post-implementation review report','Solution Architect','Principal Architect','Report accepted by client; improvements fed back to standards.')]

def lifecycle():
    b = [H1('Mandatory Consulting Lifecycle (15 Stages)', 'lifecycle', 6),
     P('Every engagement follows these 15 stages in order. Each stage lists its **procedure, checklist, output, owner, reviewer and approval criteria**. A stage closes only when its approval criteria are met and recorded. Do not start the next stage early; if the client insists, record the risk and get Principal Architect approval.'),
     DIAGRAM(D.lifecycle(), '**Figure 6.1** The 15-stage consulting lifecycle in three phases.'),
     TABLE(['Stage', 'Output', 'Owner', 'Reviewer'], [[f'{s[0]} {s[1]}', s[5], s[6], s[7]] for s in STAGES], [26, 34, 20, 20])]
    for s in STAGES:
        b += [H2(f'Stage {s[0]} — {s[1]}'), P(f'**Objective:** {s[2]}'), P('**Procedure**'), OL(s[3]), P('**Checklist**'), CHECK(s[4]),
              TABLE(['Output', 'Owner', 'Reviewer'], [[s[5], s[6], s[7]]], [50, 25, 25]),
              CALL('gold', f'Gate {int(s[0])}: approval criteria', s[8])]
    return b

TREE = '''architecture-project/
├── README.md
├── docs/
│   ├── executive-summary.md
│   ├── business-context.md
│   ├── requirements/
│   ├── current-state/
│   ├── target-state/
│   ├── constraints.md
│   ├── assumptions.md
│   ├── risks.md
│   ├── architecture-decisions/
│   ├── security/
│   ├── cost-analysis/
│   └── implementation-roadmap.md
├── diagrams/
│   ├── context/        containers/    components/
│   ├── deployment/     data-flow/     network/
├── specifications/
│   ├── api/    database/    integrations/    infrastructure/
├── reviews/
├── adr/
└── handover/'''

def repo():
    rows = [
     ['README.md', 'Entry point: purpose, status, version, how to navigate, owners, approval state, glossary link.', 'Project name, client (or code name), version, status, contacts, folder map, how to render diagrams.'],
     ['docs/executive-summary.md', 'One-to-two page business summary for executives.', 'Problem, recommendation, cost range, key risks, decisions needed, next steps. No jargon.'],
     ['docs/business-context.md', 'Why the project exists.', 'Organisation, objectives (measurable), stakeholders, processes, user journeys, success measures.'],
     ['docs/requirements/', 'Requirements baseline.', 'functional.md, non-functional.md, acceptance-criteria.md, traceability.csv; each with unique IDs.'],
     ['docs/current-state/', 'As-is architecture.', 'Application and integration inventory, infrastructure, data, security posture, operations, pain points.'],
     ['docs/target-state/', 'To-be architecture.', 'Options paper, chosen architecture narrative, component descriptions, migration strategy, operations model.'],
     ['docs/constraints.md', 'Fixed limits.', 'Budget, dates, skills, legal, technology; each marked CONFIRMED or ASSUMED.'],
     ['docs/assumptions.md', 'Unverified beliefs.', 'ID, statement, impact if wrong, owner, date to confirm, status.'],
     ['docs/risks.md', 'Risk register.', 'ID, description, likelihood, impact, score, mitigation, owner, status.'],
     ['docs/architecture-decisions/', 'Narrative of decisions and trade-offs for readers.', 'Decision summaries linking to ADR files in /adr; comparison matrices.'],
     ['docs/security/', 'Security architecture.', 'Trust boundaries, threat model, control mapping, data classification, access model, open security risks.'],
     ['docs/cost-analysis/', 'Money.', 'Build estimate, run-cost model with calculator references and dates, licences, TCO comparison of options.'],
     ['docs/implementation-roadmap.md', 'Delivery plan.', 'Phases, milestones, dependencies, team needs, rollout and migration plan, review gates.'],
     ['diagrams/context/', 'C4 Level 1 diagrams.', 'Source (.puml / .mmd / .dsl / .drawio) and exported SVG/PNG; one per scenario (as-is, to-be).'],
     ['diagrams/containers/', 'C4 Level 2 diagrams.', 'Deployable units, technologies and protocols.'],
     ['diagrams/components/', 'C4 Level 3 diagrams.', 'Only for containers that need it.'],
     ['diagrams/deployment/', 'Where software runs.', 'Regions, zones, nodes, environments.'],
     ['diagrams/data-flow/', 'Data movement and trust boundaries.', 'DFDs with boundaries, data classes and protocols.'],
     ['diagrams/network/', 'Network layout.', 'Subnets, routing, firewalls, connectivity, DNS.'],
     ['specifications/api/', 'API contracts.', 'OpenAPI / AsyncAPI files, versioning and error policy, auth model.'],
     ['specifications/database/', 'Data design.', 'ERD, data dictionary, migration scripts approach, backup/retention policy.'],
     ['specifications/integrations/', 'System-to-system contracts.', 'Interface catalogue, message formats, retry and error handling, SLAs.'],
     ['specifications/infrastructure/', 'Infrastructure design.', 'Resource list, IaC module structure, environment matrix, sizing assumptions.'],
     ['reviews/', 'Review evidence.', 'Peer review records, ARB minutes, checklist results, finding logs with disposition.'],
     ['adr/', 'Architecture Decision Records.', '0001-title.md ... in sequence; status, context, options, decision, consequences. Never deleted, only superseded.'],
     ['handover/', 'Engineering handover pack.', 'Handover checklist, specifications index, environment access process, acknowledgements, open-risk list.']]
    return [H1('Architecture Repository Structure', 'repo', 7),
     P('Every engagement uses this structure so any architect can find anything in minutes. Documents are written in Markdown, diagrams as code where possible, changes are reviewed by pull request, and approved baselines are tagged. A ready-made skeleton with placeholder files is supplied in **architecture-project-template/**.'),
     CODE(TREE, 'text'),
     H2('Every folder and expected content'),
     TABLE(['Path', 'Purpose', 'Expected contents'], rows, [24, 28, 48]),
     CALL('gold', 'Repository rules', 'One repository per engagement; no secrets or personal data; each document has an owner, version and review date; use pull requests with at least one reviewer; tag the version approved at Stage 12 (for example `arch-approved-v1.0`).')]

def standards():
    deliv = [
     ['Executive summary', 'docs/executive-summary.md', 'Business language; recommendation first; max 2 pages'],
     ['Business context', 'docs/business-context.md', 'Objectives measurable; stakeholders named'],
     ['Current-state architecture', 'docs/current-state/ + diagrams', 'Validated by client'],
     ['Target-state architecture', 'docs/target-state/ + diagrams', 'Every MUST requirement traced'],
     ['Functional requirements', 'docs/requirements/functional.md', 'IDs, priority, acceptance criteria'],
     ['Non-functional requirements', 'docs/requirements/non-functional.md', 'Quantified'],
     ['System context diagram', 'diagrams/context/', 'C4 L1; readable by non-technical people'],
     ['Container diagram', 'diagrams/containers/', 'C4 L2; technologies and protocols labelled'],
     ['Component diagram', 'diagrams/components/', 'C4 L3 for key containers'],
     ['Deployment diagram', 'diagrams/deployment/', 'Environments, zones, redundancy'],
     ['Data flow diagram', 'diagrams/data-flow/', 'Data classes and trust boundaries'],
     ['Integration diagram', 'specifications/integrations/', 'Every interface with owner and protocol'],
     ['Security and trust boundaries', 'docs/security/', 'Threat model and control mapping'],
     ['Technology selection matrix', 'docs/architecture-decisions/', 'Weights agreed before scoring'],
     ['Architecture Decision Records', 'adr/', 'One per high-impact decision'],
     ['Risk register', 'docs/risks.md', 'Owners and mitigations'],
     ['Cost estimate', 'docs/cost-analysis/', 'Ranges, sources, dates'],
     ['Implementation roadmap', 'docs/implementation-roadmap.md', 'Phases and gates'],
     ['Migration strategy', 'docs/target-state/migration.md', 'Cut-over, rollback, data migration, parallel run'],
     ['Operational considerations', 'docs/target-state/operations.md', 'Monitoring, backup, DR, support, runbooks']]
    return [H1('Deliverable and Review Standards', 'standards', 8),
     P('Every major consulting project must include the twenty deliverables below. Use the **C4 model** where suitable and make every diagram understandable to technical and non-technical stakeholders: title, legend, scope, date, version, and plain labels (say "Customer database", not "RDS-prod-01").'),
     TABLE(['#', 'Deliverable', 'Location', 'Quality bar'], [[str(i+1)] + r for i, r in enumerate(deliv)], [5, 25, 32, 38]),
     H2('Diagram standards'),
     UL(['One diagram, one purpose, one audience. If it needs more than ~12 boxes, split it.','Every element has a name, a type, and a short description; every arrow has a label saying what flows and how.','Always include a legend and the C4 level; show system boundary and trust boundaries.','Colour is never the only carrier of meaning (accessibility); keep a consistent palette.','Store diagram source in Git next to exported images.']),
     H2('Architecture review standards'),
     TABLE(['Review area', 'Key questions', 'Pass criteria'], [
      ['Requirement coverage', 'Is every MUST requirement traced to a design element?', 'Traceability 100% for MUST'],
      ['Architecture consistency', 'Do diagrams, ADRs and specs agree?', 'No contradictions'],
      ['Security boundaries', 'Are trust boundaries, authN/authZ and secrets handled?', 'Threat model reviewed; no open high risk'],
      ['Data protection', 'Data classified, encrypted, retained and deleted per policy and law?', 'Controls mapped; privacy owner consulted'],
      ['Scalability', 'What breaks first at 10x?', 'Bottleneck and plan documented'],
      ['Availability', 'Single points of failure? Meets target?', 'SPOFs removed or accepted in writing'],
      ['Failure handling', 'Timeouts, retries, idempotency, degradation?', 'Designed per integration'],
      ['Observability', 'Can we detect and diagnose problems?', 'Logs, metrics, traces, alerts defined'],
      ['Backup and recovery', 'RPO/RTO defined and restore tested?', 'Test plan exists'],
      ['Performance', 'Targets and test plan?', 'Numeric targets'],
      ['Operational complexity', 'Can the client run it?', 'Skills gap closed or planned'],
      ['Cost', 'Estimate reproducible with sources?', 'Independent review done'],
      ['Maintainability', 'Is change cheap?', 'Boundaries and tests in place'],
      ['Vendor dependency', 'Lock-in understood with an exit plan?', 'Documented'],
      ['Implementation feasibility', 'Can the team build it in time?', 'Plan and skills validated']], [20, 46, 34]),
     CALL('red', 'Assumptions and unresolved decisions', 'Every deliverable has an **Assumptions** and an **Unresolved decisions** section. Mark each fact as CONFIRMED (with source), ASSUMED (with owner and confirm-by date) or UNKNOWN. **Never present an unverified assumption as a confirmed client fact.**')]
