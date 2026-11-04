from lib import *
BL = lambda n, c: [[''] * c for _ in range(n)]
META = [('Project / client', ''), ('Document ID and version', ''), ('Author / date', ''), ('Reviewer / date', ''), ('Approver / date', ''), ('Status (Draft / In review / Approved)', '')]

def tA():
    return [H1('Appendix A: 15-Part Lesson Template', 'app-a', 'A'), P('Use for every syllabus subject. One row per part; keep each answer short and testable.'),
      FORM([(f'{i+1}. {k}', '') for i, k in enumerate(LESSON_KEYS)])]

def tB():
    return [H1('Appendix B: Architecture Assessment Template', 'app-b', 'B'),
     P('For Stage 03-04 assessments. Facts must be tagged **C** (confirmed, with source), **A** (assumed, with owner and date) or **U** (unknown).'), FORM(META),
     H2('1. Executive summary'), P('Three paragraphs: situation, key findings, recommendation. No jargon. Max one page.'),
     H2('2. Scope and method'), UL(['In scope / out of scope','Interviews held (who, when) and documents reviewed','Evidence limits: what we could not see']),
     H2('3. Business context'), UL(['Objectives (measurable)','Stakeholders and decision-makers','Constraints (budget, date, skills, legal)']),
     H2('4. Current-state architecture'), TABLE(['Area', 'Findings', 'Evidence / source', 'C/A/U'], BL(8, 4), [16, 44, 28, 12]),
     UL(['Attach C4 L1 and L2 diagrams (as-is)','Application and integration inventory','Infrastructure, data, security, operations']),
     H2('5. Findings and risks'), TABLE(['ID', 'Finding', 'Impact', 'Likelihood', 'Severity H/M/L', 'Recommendation'], BL(8, 6), [6, 30, 18, 12, 12, 22]),
     H2('6. Quality attribute assessment'), TABLE(['Attribute', 'Rating 1-5', 'Evidence', 'Key gap'], [[a, '', '', ''] for a in ['Scalability', 'Availability', 'Reliability', 'Performance', 'Security', 'Maintainability', 'Interoperability', 'Portability', 'Cost', 'Operability']], [20, 12, 40, 28]),
     H2('7. Options and recommendation'), P('Summarise at least two options plus "do nothing"; link to the comparison matrix (Appendix G).'),
     H2('8. Roadmap and next steps'), TABLE(['Phase', 'Actions', 'Owner', 'Timing', 'Cost range'], BL(4, 5), [14, 40, 16, 14, 16]),
     H2('9. Assumptions and unresolved decisions'), TABLE(['ID', 'Statement', 'Owner', 'Confirm by', 'Status'], BL(5, 5), [8, 46, 16, 16, 14])]

def tC():
    Q = {
     '1. Organisation and objectives': ['What does the organisation do, and how does it make money or deliver value?','What are the top three business objectives for the next 12-24 months?','How will success of this project be measured (numbers)?','Who is the executive sponsor and who makes the final decision?','What happens if nothing changes?'],
     '2. Stakeholders and process': ['Who uses the system today (roles, count)? Who will be affected by change?','Which business processes does it support? Where are manual steps or workarounds?','What are the busiest periods (daily, monthly, seasonal)?'],
     '3. Current systems': ['List applications, owners, technologies and versions. Which are vendor-supported?','Which systems exchange data? How (files, API, manual re-keying)?','Where is it hosted (own premises, data centre, cloud)? Who administers it?','Which databases and data volumes exist? How is data backed up and has restore been tested?','What documentation exists and how current is it?'],
     '4. Pain points and incidents': ['What are the top five technical problems? Which cost money or customers?','Recent outages or security incidents: dates, causes, impact?','How long does a release take? How often do releases fail?'],
     '5. Requirements and quality': ['What must the new/changed system do that it cannot now?','Expected users and transactions (today, in 3 years)?','Acceptable downtime and data loss? Response-time expectations?','Which data is personal, financial or confidential?'],
     '6. Security and compliance': ['Which laws, standards or customer audits apply (e.g. data-protection law, ISO/IEC 27001, PCI DSS)?','How are users authenticated and authorised? Is multi-factor authentication used?','Any previous security assessments or penetration tests?'],
     '7. Team, skills and operations': ['Size and skills of in-house IT/development team? Who is on call?','Preferred technologies and any prohibited ones?','Who will operate the solution after delivery?'],
     '8. Budget, timeline and constraints': ['Budget range and funding approval process?','Hard dates (events, contracts, regulations)?','Procurement or vendor constraints (existing licences, cloud agreements)?'],
     '9. Risks and expectations': ['What worries you most about this project?','What has been tried before and why did it fail?','What would make you call this engagement a success?']}
    b = [H1('Appendix C: Client Discovery Questionnaire', 'app-c', 'C'), P('Send before the first interview; complete together. Record who answered, the date, and tag each answer **C / A / U**. Ask for documents where answers exist.'), FORM([('Client / respondent', ''), ('Role', ''), ('Interviewer / scribe', ''), ('Date', '')])]
    for h, qs in Q.items():
        b += [H2(h), TABLE(['#', 'Question', 'Answer', 'C/A/U'], [[str(i + 1), q, '', ''] for i, q in enumerate(qs)], [5, 45, 40, 10])]
    b += [CALL('red', 'Confidentiality', 'Information in this questionnaire is confidential and used only for the engagement under the signed NDA. Do not paste client answers into public tools.')]
    return b

def tD():
    return [H1('Appendix D: Architecture Decision Record Template', 'app-d', 'D'), P('Copy to `adr/NNNN-short-title.md`. One decision per record. Accepted ADRs are never edited; supersede them.'),
     CODE('''# ADR-NNNN: <short noun phrase describing the decision>

Status: Proposed | Accepted | Deprecated | Superseded by ADR-XXXX
Date: YYYY-MM-DD
Deciders: <names / roles>     Reviewer: <name>     Impact: Low | Medium | High

## Context
<The problem, forces and constraints. Link to requirement IDs. Mark facts CONFIRMED / ASSUMED / UNKNOWN.>

## Options considered
1. <Option A>: summary, pros, cons, cost, risk
2. <Option B>: summary, pros, cons, cost, risk
3. <Do nothing / baseline>

## Decision
We will <choice>, because <reason tied to requirements and trade-offs>.

## Consequences
Positive: ...
Negative / risks: ...
Follow-up actions and owners: ...
Review trigger: <measurable condition that means we should revisit>

## Compliance and links
Related requirements: REQ-..., NFR-...   Related ADRs: ...   Evidence: <links>''', 'markdown'),
     H2('Quality checklist'), CHECK(['Two or more real options plus baseline', 'Trade-offs include downsides', 'Linked to requirements', 'Assumptions labelled', 'Review trigger defined', 'Reviewed by a second person; approved by authority matching the impact level'])]

def tE():
    return [H1('Appendix E: Requirements Specification Template', 'app-e', 'E'), FORM(META),
     H2('1. Purpose and scope'), P('What the system is for; boundaries; link to context diagram. Out of scope list.'),
     H2('2. Stakeholders'), TABLE(['Name / role', 'Interest', 'Influence', 'Contact'], BL(5, 4), [28, 34, 18, 20]),
     H2('3. Business objectives'), TABLE(['ID', 'Objective', 'Measure and target', 'Source'], BL(4, 4), [8, 40, 32, 20]),
     H2('4. Functional requirements'), TABLE(['ID', 'The system shall...', 'Priority (M/S/C)', 'Acceptance criteria (Given/When/Then)', 'Source', 'Status C/A/U'], [['FR-001', '', '', '', '', ''], ['FR-002', '', '', '', '', ''], ['FR-003', '', '', '', '', ''], ['FR-004', '', '', '', '', '']], [8, 28, 10, 30, 12, 12]),
     H2('5. Non-functional requirements'), TABLE(['ID', 'Attribute', 'Requirement with number', 'Measure / test', 'Priority', 'Status'], [['NFR-001', 'Performance', '', '', '', ''], ['NFR-002', 'Availability', '', '', '', ''], ['NFR-003', 'Security', '', '', '', ''], ['NFR-004', 'Scalability', '', '', '', ''], ['NFR-005', 'Recovery (RPO/RTO)', '', '', '', ''], ['NFR-006', 'Maintainability', '', '', '', '']], [10, 16, 32, 22, 10, 10]),
     H2('6. Constraints'), TABLE(['ID', 'Constraint', 'Type (budget, date, skill, legal, tech)', 'Status C/A'], BL(4, 4), [8, 44, 30, 18]),
     H2('7. Assumptions and dependencies'), TABLE(['ID', 'Statement', 'Impact if wrong', 'Owner', 'Confirm by'], BL(4, 5), [8, 38, 24, 16, 14]),
     H2('8. Traceability matrix'), TABLE(['Objective', 'Requirement IDs', 'Design element', 'Test / acceptance'], BL(4, 4), [24, 26, 26, 24]),
     H2('9. Sign-off'), TABLE(['Name', 'Role', 'Signature', 'Date'], BL(3, 4), [30, 28, 28, 14])]

def tF():
    return [H1('Appendix F: Technical Feasibility Report Template', 'app-f', 'F'), FORM(META),
     H2('1. Question being assessed'), P('One sentence: "Can we ... within ... by ... for ...?"'),
     H2('2. Summary conclusion'), TABLE(['Result', 'Select one'], [['Feasible', '[ ]'], ['Feasible with conditions (list)', '[ ]'], ['Not feasible (reasons)', '[ ]'], ['Cannot conclude: evidence needed', '[ ]']], [70, 30]),
     H2('3. Feasibility dimensions'), TABLE(['Dimension', 'Rating (High/Med/Low)', 'Evidence', 'Risks / conditions'], [[d, '', '', ''] for d in ['Technical', 'Economic (cost-benefit)', 'Schedule', 'Legal and compliance', 'Operational', 'Skills and staffing', 'Security and data protection', 'Vendor / licence']], [22, 16, 34, 28]),
     H2('4. Proofs of concept and spikes'), TABLE(['Spike', 'Question', 'Time-box', 'Result', 'Decision'], BL(3, 5), [18, 30, 12, 24, 16]),
     H2('5. Cost range'), TABLE(['Item', 'One-time', 'Recurring / year', 'Source and date'], BL(5, 4), [28, 18, 20, 34]),
     H2('6. Risks'), TABLE(['ID', 'Risk', 'L', 'I', 'Mitigation', 'Owner'], BL(4, 6), [6, 34, 6, 6, 32, 16]),
     H2('7. Assumptions and unresolved decisions'), TABLE(['ID', 'Statement', 'Owner', 'Confirm by'], BL(4, 4), [8, 56, 18, 18]),
     H2('8. Recommendation and next step'), P('What to do, by whom, by when, and what decision is needed from the sponsor.')]

EX = [('Requirement fit', 20, (4, 5, 3)), ('Total cost (3 years)', 20, (4, 2, 4)), ('Operational complexity', 15, (5, 1, 5)), ('Delivery time / risk', 15, (4, 2, 5)),
      ('Scalability', 10, (3, 5, 3)), ('Security', 10, (4, 4, 3)), ('Vendor lock-in', 10, (4, 3, 2))]
EX_ROWS = [[n, str(w)] + [str(x) for x in sc] for n, w, sc in EX]
EX_TOT = [sum(w * sc[i] for _, w, sc in EX) for i in range(3)]

def tG():
    crit = ['Requirement fit', 'Security', 'Scalability', 'Availability / reliability', 'Maintainability and skills', 'Total cost (3 years)', 'Vendor lock-in (5 = low)', 'Operational complexity (5 = low)', 'Delivery time / risk', 'Ecosystem and support']
    return [H1('Appendix G: Technology Comparison Matrix', 'app-g', 'G'),
     P('**Method:** (1) agree criteria and weights with the client **before** scoring; (2) score each option 1-5 with evidence; (3) weighted score = Σ(weight x score); (4) run sensitivity: change weights ±20% and see if the winner changes; (5) the highest score is an input to the recommendation, not the recommendation itself.'),
     FORM([('Decision to be made', ''), ('Options (min. two + baseline)', ''), ('Date, authors, reviewer', '')]),
     TABLE(['Criterion', 'Weight (sum = 100)', 'Option A score', 'Option B score', 'Option C / baseline', 'Evidence / notes'], [[c, '', '', '', '', ''] for c in crit] + [['**Weighted total**', '**100**', '', '', '', '']], [24, 12, 12, 12, 14, 26]),
     H2('Example scoring (training)'), TABLE(['Criterion', 'Weight', 'Modular monolith', 'Microservices', 'Low-code SaaS'], EX_ROWS + [['**Weighted total (max 500)**', '100'] + [f'**{t}**' for t in EX_TOT]], [30, 10, 20, 20, 20]),
     P('Calculation, modular monolith: 20x4 + 20x4 + 15x5 + 15x4 + 10x3 + 10x4 + 10x4 = 80+80+75+60+30+40+40 = **405**. Microservices = **305**; low-code = **370**.'),
     CALL('gold', 'Reminder', 'Low-code scores close to the monolith here. If the client values speed over customisation, or lock-in weight drops, it could win. State this in the recommendation.')]

def tH():
    return [H1('Appendix H: Risk and Assumption Register Template', 'app-h', 'H'),
     TABLE(['ID', 'Risk', 'Cause', 'Likelihood (1-5)', 'Impact (1-5)', 'Score', 'Mitigation', 'Owner', 'Status'], BL(8, 9), [5, 17, 12, 9, 9, 7, 21, 10, 10]),
     P('**Score = likelihood x impact.** 15-25 high (act now), 8-14 medium (plan), 1-7 low (watch). Review weekly.'),
     TABLE(['ID', 'Assumption', 'Why we believe it', 'Impact if wrong', 'Owner', 'Confirm by', 'Status'], BL(6, 7), [6, 24, 18, 18, 12, 11, 11])]

def tI():
    return [H1('Appendix I: Stakeholder Map Template', 'app-i', 'I'),
     TABLE(['Stakeholder', 'Role', 'Interest', 'Influence (H/M/L)', 'Attitude', 'Engagement plan', 'Owner'], BL(8, 7), [16, 12, 20, 12, 10, 20, 10]),
     P('Grid: **High influence + high interest** = manage closely · high influence + low interest = keep satisfied · low influence + high interest = keep informed · low/low = monitor.')]

def tJ():
    areas = [('Requirement coverage', ['Every MUST requirement traced to design', 'NFRs quantified', 'Acceptance criteria exist']),
     ('Architecture consistency', ['Diagrams, ADRs and specs agree', 'Terminology consistent', 'C4 levels not mixed']),
     ('Security boundaries', ['Trust boundaries drawn', 'Authentication and authorisation designed', 'Secrets management defined', 'Threat model reviewed']),
     ('Data protection', ['Data classified', 'Encryption in transit and at rest decided', 'Retention and deletion defined', 'Privacy owner consulted']),
     ('Scalability and performance', ['Load model and targets', 'Bottleneck identified', 'Test plan']),
     ('Availability and failure handling', ['Single points of failure removed or accepted', 'Timeouts, retries, idempotency', 'Graceful degradation']),
     ('Observability', ['Logs, metrics, traces', 'Alerts with owners', 'Dashboards']),
     ('Backup and recovery', ['RPO/RTO defined', 'Backups separate from primary', 'Restore tested or test planned']),
     ('Operational complexity', ['Client can operate it', 'Runbooks planned', 'Support model defined']),
     ('Cost', ['Estimate has sources and dates', 'Independent review', 'Cost-control measures']),
     ('Maintainability and vendor dependency', ['Module boundaries clear', 'Lock-in assessed with exit plan', 'Technical debt visible']),
     ('Implementation feasibility', ['Skills available', 'Timeline realistic', 'Dependencies identified'])]
    b = [H1('Appendix J: Architecture Review Checklist', 'app-j', 'J'), FORM([('Project / document under review', ''), ('Reviewer (not the author)', ''), ('Date', ''), ('Outcome: Approved / Approved with conditions / Rejected', '')])]
    for h, items in areas: b += [H2(h), CHECK(items)]
    b += [H2('Findings log'), TABLE(['#', 'Finding', 'Severity (Critical/High/Med/Low)', 'Owner', 'Due', 'Resolution'], BL(6, 6), [5, 38, 17, 12, 10, 18]),
          CALL('gold', 'Rule', 'No approval while a Critical or unaccepted High finding is open. Document accepted risks with the sponsor\'s name and date.')]
    return b

def tK():
    return [H1('Appendix K: Cost Estimation Template', 'app-k', 'K'),
     P('Companion spreadsheet with live formulas: **Cost-Estimation-Template.xlsx**. Use the same structure on paper. Currency: ______ · Tax basis: ______ · Estimate date: ______.'),
     H2('1. Effort (delivery cost)'), TABLE(['Activity', 'Role', 'Hours', 'Cost/h', 'Cost'], BL(8, 5), [34, 22, 12, 14, 18]),
     H2('2. Direct expenses'), TABLE(['Item', 'Amount', 'Basis'], BL(4, 3), [40, 20, 40]),
     H2('3. Pricing calculation'), TABLE(['Line', 'Formula', 'Value'], [['Delivery cost (DC)', 'Σ effort + expenses', ''], ['Risk reserve', 'risk % x DC', ''], ['Selling price', '(DC + reserve) / (1 − target GM)', ''], ['Gross margin %', '(Price − DC) / Price', ''], ['Contribution margin', 'Price − DC − variable costs − reserve', ''], ['Stress GM at +25% effort', '(Price − 1.25 x DC) / Price', '']], [28, 44, 28]),
     H2('4. Client run-cost estimate (cloud)'), TABLE(['Service / component', 'Quantity and size', 'Region', 'Monthly low', 'Monthly high', 'Calculator link and date'], BL(8, 6), [20, 20, 10, 12, 12, 26]),
     P('Include compute, storage, database, network/data transfer, backup, monitoring, support plan, licences and tax. Present as a **range** and state it is an estimate; the client\'s bill depends on usage.'),
     H2('5. Three-year TCO of options'), TABLE(['Cost', 'Option A', 'Option B', 'Baseline'], [[c, '', '', ''] for c in ['Build (one-time)', 'Infrastructure (3 yrs)', 'Licences (3 yrs)', 'Support and operations (3 yrs)', 'Training', 'Risk contingency', 'Total']], [34, 22, 22, 22])]

def tL():
    slides = [('1. Title and purpose', 'Client name, engagement, date, presenters, what decision we need today.'), ('2. Executive summary', 'Problem, recommendation, cost range, timeline, risk, decisions needed: on one slide.'), ('3. Business context', 'Objectives, constraints and what success looks like in the client\'s words.'),
      ('4. Where you are today', 'C4 Level 1 as-is; 3-5 key findings with evidence.'), ('5. Requirements that matter most', 'Top 5-8 requirements with numbers; assumptions flagged.'), ('6. Options considered', 'Two or three options plus "do nothing", each in business terms.'),
      ('7. Comparison and trade-offs', 'Weighted matrix; honest strengths and weaknesses.'), ('8. Recommended architecture', 'C4 L1 and L2 to-be diagrams; one-sentence explanation of each part.'), ('9. Security and data protection', 'Trust boundaries and controls in plain language; what remains the client\'s responsibility.'),
      ('10. Cost', 'One-time and recurring ranges; assumptions; how we estimated.'), ('11. Risks and mitigations', 'Top 5 with owners.'), ('12. Roadmap', 'Phases, milestones, team needs, decision gates.'), ('13. Decisions we need from you', 'Numbered list with owners and dates.'), ('14. Next steps', 'First 30 days; contacts.'), ('Appendix', 'Detail diagrams, ADR list, assumptions register, glossary.')]
    return [H1('Appendix L: Client Presentation Template', 'app-l', 'L'), P('Present in business language. One message per slide. Rehearse with a senior reviewer. Send a written follow-up within 24 hours.'),
     TABLE(['Slide', 'Content guidance'], [[a, b] for a, b in slides], [30, 70]),
     CALL('blue', 'Language rules', 'Say "customer database" not "RDS cluster"; "can fail over automatically" not "multi-AZ"; explain any acronym once. Show trade-offs honestly: a recommendation that hides its downside is not trusted.'),
     H2('Presentation checklist'), CHECK(['Every number has a source and date', 'Assumptions slide included', 'No guarantees on uptime, savings or security', 'Decision requests are explicit', 'Rehearsed; questions anticipated', 'Fictional/anonymised data only in demos'])]

def tM():
    return [H1('Appendix M: Engineering Handover Checklist', 'app-m', 'M'), FORM([('Project', ''), ('Approved architecture version / Git tag', ''), ('Handover date', ''), ('Handed over by / received by', '')]),
     H2('Common'), CHECK(['Approved architecture pack (diagrams, ADRs, requirements) shared with links', 'Open risks, assumptions and unresolved decisions listed with owners', 'Definition of done and acceptance criteria agreed', 'Governance cadence and escalation path agreed', 'Change process explained (new ADR to deviate)']),
     H2('Development team'), CHECK(['Component and interface specifications', 'Coding and API standards', 'Module boundaries and allowed dependencies (architecture tests)', 'Backlog seeds traced to requirements', 'Non-functional test targets']),
     H2('Cloud / infrastructure team'), CHECK(['Deployment and network diagrams', 'Environment matrix (dev/test/prod) and sizing assumptions', 'IaC module structure and state management', 'Backup, DR, RPO/RTO and restore-test plan', 'Cost budget and alerts']),
     H2('Data team'), CHECK(['ERD and data dictionary', 'Data classification, retention and deletion rules', 'Migration approach and reconciliation checks', 'Reporting and analytics requirements']),
     H2('API / integration team'), CHECK(['OpenAPI / AsyncAPI contracts', 'Versioning, error and rate-limit policy', 'Retry, idempotency and dead-letter handling', 'Interface catalogue with owners and SLAs']),
     H2('Security team'), CHECK(['Trust boundaries and threat model', 'Authentication and authorisation design', 'Secrets and key management', 'Logging and monitoring requirements', 'Security testing plan']),
     H2('Operations and support'), CHECK(['Monitoring, alerting and dashboards', 'Runbooks and on-call model', 'Incident and change process', 'Training for client operators']),
     H2('Acknowledgement'), TABLE(['Team', 'Name', 'Questions raised', 'Signature', 'Date'], [[t, '', '', '', ''] for t in ['Development', 'Cloud', 'Data', 'API / integration', 'Security', 'Operations']], [18, 22, 28, 20, 12])]

def all_templates():
    return [tA, tB, tC, tD, tE, tF, tG, tH, tI, tJ, tK, tL, tM]
