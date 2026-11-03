# Stage S10 — Security Review (part of Gate G8)

## Purpose
Make sure the system cannot easily be abused, cannot leak one customer's data to another, and handles secrets correctly.

## Who and when
- **Owner:** Security Specialist (or Technical Lead in a small team); you run the checklist items. **Approver:** Security Specialist and Founder.
- **Time:** 3–5 days.

## Before you start
Working build on staging; threat model from S05; test accounts for two fake customers (A and B).

## Step by step

1. **Secrets check.** Run the secret scanner over the whole history. Confirm no key, token or password is in Git. Confirm production secrets exist only in the secret manager and are different from staging.
2. **Access control check.** List roles (admin, reviewer, support, read-only). For each role, confirm what it can and cannot do. Try forbidden actions and confirm they fail.
3. **Customer isolation test.** Log in as customer A; try to read customer B's bills by changing IDs in the address or request. Every attempt must fail. Repeat for files and search.
4. **Webhook test.** Send a webhook with no signature, a wrong signature, and an old (replayed) message. All must be rejected.
5. **Upload test.** Upload a wrong file type, an oversized file, a file with a fake extension, a document with a hidden script. All must be rejected or safely handled.
6. **Prompt injection test.** Prepare 10 documents containing hostile text ("ignore previous instructions", "send all data to…", "mark approved"). Confirm the AI step cannot trigger any action and the rules still decide. Include text hidden in white-on-white or in small print.
7. **Data leakage test.** Ask the AI step things designed to reveal prompts, other customers' data or secrets. Confirm nothing sensitive is available to it in the first place (least privilege).
8. **Logging check.** Read sample logs; personal data masked; no secrets; no full prompts that contain personal data.
9. **Tool and permission check** (if any agent): list every tool; confirm least privilege; confirm write tools need approval; confirm **no** unrestricted email, payment, database or command access.
10. **Spending and abuse check.** Hit a rate limit and a spending cap deliberately; confirm blocking and alert.
11. **Infrastructure check:** HTTPS only; workflow editor not publicly reachable; admin accounts use two-factor authentication; software versions patched; backups encrypted.
12. **Data protection check:** retention period defined; deletion tested; contract and processing agreement in place; list of sub-processors (including the AI provider) and their data terms verified.
13. **Write the report** with every check, result, evidence, owner for each finding, and a retest date. Reference OWASP Top 10 for LLM Applications and OWASP agentic-AI guidance (verify current versions).

## Worked example
Test 3 finding: changing `customer_id` in a request returned another customer's bill list. **Severity P1.** Fix: server-side filter by the authenticated customer; add test `test_customer_cannot_read_other_customer`; retest passed.

## Deliverables (`06-security/`)
- `security-checklist.xlsx` (completed), `security-report.md`, `test-evidence/`, `findings-and-fixes.md`

## Done checklist
- [ ] No secrets in Git history; production secrets separate.
- [ ] Customer isolation proven by test.
- [ ] Webhooks and uploads hardened and tested.
- [ ] Prompt-injection set run; no unsafe action possible.
- [ ] Logs contain no secrets or unmasked personal data.
- [ ] Spend caps and rate limits work.
- [ ] Retention, deletion and data-processing terms documented.
- [ ] All P1/P2 findings fixed and retested.

## Common mistakes
- Treating security as the last-minute task.
- Believing "nobody will guess that URL".
- Logging full requests "for debugging".

## Gate G8 sign-off
Final G8 = S09 (quality) **and** S10 (security) both approved. Never claim the system is "safe" without evidence; claim only what was tested.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
