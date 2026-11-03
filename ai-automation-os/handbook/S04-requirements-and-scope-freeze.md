# Stage S04 — Requirements and Scope Freeze (Gate G4)

## Purpose
Write down exactly what we will build, what we will not build, how we will prove it works, and what it costs in time. After this gate, changes are only allowed through a priced change request.

## Who and when
- **Owner:** Business Analyst and PM. **Approver (G4):** customer owner and Founder (both sign).
- **Time:** 3–5 days.

## Before you start
G3 passed; TO-BE workflow; business rules; sample data.

## Step by step

1. **List the requirements** as short, testable statements, numbered `REQ-001…`. Format: *"The system shall [do something] when [condition], so that [benefit]."* Group them: functional (what it does), data, integration, security, performance, reporting.
2. **Write acceptance criteria** for each requirement: a check a stranger can run and answer yes/no. Example: *"Given a bill with a wrong GSTIN check character, the system routes it to human review and records reason GSTIN_INVALID."*
3. **Freeze the Input/Output Specification (T5):**
   - *Inputs:* source, file types, size limit, required and optional fields, validation rules, who may send.
   - *Processing:* trigger, business rules (numbered), AI model needs, thresholds, retries, escalation.
   - *Outputs:* format and JSON schema (name and version it, e.g. `bill.v1`), destination, accuracy target per field, notifications, audit log, error responses, approval needs.
4. **State the accuracy targets** with how they will be measured (e.g. "field exact-match ≥ 97% on a labelled set of 300 real bills; zero unflagged tax-maths errors reach the ledger").
5. **List integrations:** each system, what we read or write, authentication method, who provides access, test account.
6. **Write out of scope** explicitly: things the customer might assume but we will not do.
7. **List assumptions and customer responsibilities** (e.g. provide test data within 5 days; named reviewer available daily).
8. **Estimate effort** with three numbers per task: optimistic, expected, pessimistic hours. Add testing, documentation, meetings. Do not forget deployment and training.
9. **Set the timeline** with milestones and the gate dates.
10. **Set the budget** using the cost sheet from Stage S11 early estimate (a draft is fine now; final at G9).
11. **Assemble the Scope Freeze Document** and the **Statement of Work (SOW)** inputs. Review with the customer line by line.
12. **Sign.** Customer owner, Founder and PM sign. File in `03-requirements/` as read-only.
13. **Set up change control:** from now on, any change uses the Change Request form (T5 bottom): description, reason, extra hours, cost, timeline impact, risk, approval.

## Worked example (GST bill project)
- REQ-004: *The system shall reject any bill whose CGST and IGST are both non-zero and route it to human review with reason `TAX_TYPE_CONFLICT`.*
- Output schema `bill.v1`: vendor_gstin, invoice_no, invoice_date, taxable_value, cgst, sgst, igst, total, line_items, confidence per field.
- Accuracy: ≥ 97% exact match on key fields; **no bill posts without passing validation**.
- Out of scope: GST return filing, e-way bill generation, handwritten bills in regional scripts (phase 2).
- Customer responsibilities: 300 sample bills in 5 days; a reviewer available 2 hours a day.

## Deliverables (`03-requirements/`)
- `requirements-register.xlsx` (id, text, priority, acceptance criteria, owner)
- `input-output-spec-v1.md` and JSON schema files
- `scope-freeze-v1.pdf` (signed) and `change-request-template.docx`
- `effort-estimate.xlsx` (optimistic/expected/pessimistic)

## Done checklist
- [ ] Every requirement is numbered, testable, and has acceptance criteria.
- [ ] Input and output are fully specified, schema versioned.
- [ ] Accuracy targets and how they will be measured are written.
- [ ] Out-of-scope list exists.
- [ ] Estimate includes testing, documents, deployment, training.
- [ ] Customer and Founder signed; file is read-only.

## Common mistakes
- Vague words: "fast", "user-friendly", "accurate". Replace with numbers.
- Forgetting exceptions and error messages.
- Letting "small additions" in without a change request.

## Gate G4 sign-off
Both signatures. From now on: **no work outside the signed scope without an approved change request.**

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
