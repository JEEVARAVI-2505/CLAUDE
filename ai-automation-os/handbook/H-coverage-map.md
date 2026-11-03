# Appendix H — Coverage Map: Your Master Prompt vs This Handbook

Use this to check that every phase of the VEL AI Automation Engineering Division master prompt is answered, and to see what is still open.

| Master-prompt phase | Where it is answered | Status |
|---|---|---|
| 1. Market research and opportunity discovery | Part 3 (method, TAM/SAM/SOM, competitor matrix, 15 industry pages); Stage S01; Annex B (opportunity ranking); `calculator/market_sizing.py` | **Method and templates complete; live research not done** (needs sources and verification) |
| 2. Customer discovery and process analysis | Stage S02 (audit, AS-IS pictures, baseline), Stage S03 (TO-BE, A/R/AI/H/E tags); templates T2, T3 | Complete |
| 3. Input, output and scope freeze | Stage S04; template T5; Part 4 section 4.2 (behaviour for invalid input, missing data, API failure, duplicates, interruptions) | Complete |
| 4. Technology and platform selection | Part 5 (scored matrix on 10 criteria, languages, databases, integrations, architecture patterns); Stage S05; Annex C | Complete as hypotheses; **ratings need vendor-page verification** |
| 5. Solution categories A to J | Part 4 section 4.3 (ten families with flow, rules vs AI, approvals, risks, metrics) | Specification complete; **reusable workflow templates still to be built per family** |
| 6. VS Code and GitHub setup | Stage S00 (tools), Stage S06 (repository, branch protection, CI); Part 2 (folder structure); `reference-project/` | Complete |
| 7. Automation development | Part 4 section 4.1 (13-step sequence and controls); Stages S07 and S08 | Complete; working example covers rules, API and AI-safety wrapper |
| 8. Testing and QA | Stage S09 and Stage S10 | Complete |
| 9. Development and production costing | Stage S11; Annex A (full worked costing); Annex C (formulas); `calculator/` (16 tests) | Complete; **all prices are unverified placeholders** |
| 10. Production deployment | Stage S12; Part 6 section 6.1 (disaster recovery, RPO/RTO) | Complete; **Dockerfile untested** |
| 11. Customer showcase and sales | Stage S13; Part 6 section 6.4; template T10 | Complete |
| 12. Maintenance and recurring revenue | Stage S14; Part 6 section 6.3 (Basic, Professional, Enterprise packages) | Complete; **response targets and hours are proposals to adopt** |
| 13. Employee management | Part 0 section 0.5; Annex D (role charters, responsibility matrix, project record) | Complete |
| 14. Internal management portal | Annex D section 4 (17+ modules, build order, access rules) | **Specification only; the portal itself is not built** |
| First execution task: company set-up and a complete sample | Annex B (intake), Annex A (GST bill automation from research to commercial delivery), `reference-project/` | Complete as a documented example; **not a live customer deployment** |

## Honest list of what remains open

1. **Prices and market facts are unverified** (provider, vendor, WhatsApp, cloud, firm counts). Verify with sources and dates before any quote or pitch.
2. **Customer evidence is missing**: the opportunity scores are hypotheses until 10 or more interviews are done.
3. **Reusable workflow templates** (n8n exports, schemas, test sets) for families A to J are specified but not yet built.
4. **The internal portal** is specified but not built; start with the spreadsheet/Notion version described in Annex D.
5. **The sample system is partial**: the rules, API and AI-safety wrapper are built and tested; OCR, the review screen, integrations and deployment are described but not built.
6. **Legal documents** (contract, data-processing agreement) need a lawyer.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
