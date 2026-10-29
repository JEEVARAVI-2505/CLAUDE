# Costs, packages and pricing hypotheses

> The prices are **test prices to validate with real customers**, not evidence that customers will pay them. Nothing in this repository establishes
> demand. Before building more, sell or pilot to 5-10 target firms and record what they actually pay for, what they refuse, and how many of their
> documents the software handles without help.

## 1. Unavoidable and optional costs (you / the customer)

| Item | Needed? | Cost | Notes |
|---|---|---|---|
| DocuMind itself, Tesseract OCR, Ollama, Python libraries | yes / optional | ₹0 licence fees | All permissively or LGPL licensed; see THIRD_PARTY_NOTICES.md. |
| AI model weights | optional | ₹0 to download, **licence varies per model** | Read each model's licence for commercial use; several GB download and disk. Not verified from this environment. |
| Computer | yes | existing PC | Windows 10/11 64-bit, 4 GB RAM minimum (8 GB+ and ideally a GPU or fast CPU if a local AI model is used), 2 GB free disk plus space for documents. |
| Windows code-signing certificate | **optional** | recurring annual fee (quote from a certificate authority; amount not verified here) | Without it Windows SmartScreen may warn on the installer. Signing reduces but does not guarantee the absence of warnings. |
| Domain, hosting, cloud, API accounts | **no** | ₹0 | The product runs offline and needs none. |
| Your time to install (~1-2 h incl. OCR/AI setup), travel to site | yes | your cost | Include in price or bill separately - see below. |
| Taxes (GST etc.), payment fees | as applicable | - | Confirm treatment with your accountant; shown separately on quotes. |
| Ongoing maintenance (Windows/library updates, bug fixes) | you | your time | Only the support period in the quote is promised. |
| Professional liability / E&O insurance | consider | quote | Software that handles financial data has downside risk. |
| Legal review of EULA | yes (once) | lawyer's fee | The draft is not legal advice. |

## 2. Packages (test prices)

### Standard - ₹9,999 (single computer)
**Includes:** licence for 1 computer (version installed); on-site or remote installation of DocuMind, Tesseract OCR with English (and one more
language pack if freely available); data-folder + backup-folder setup and one test backup/restore; the bundled demo documents; a 60-minute walkthrough
using the customer's own sample files; User Guide; **support period: 30 days** from handover for installation and DocuMind defects (target reply within
2 business days); bug-fix updates to that version for 6 months on request.
**Excludes:** local-AI setup, templates for specific suppliers, custom fields beyond 3, importing historic documents in bulk, integration with
accounting software, training for several staff, hardware, travel beyond [x] km, taxes, any guarantee of extraction accuracy.

### Professional - ₹19,999 (single computer, expanded setup)
**Everything in Standard, plus:** up to **5 supplier/layout templates** built from the customer's own documents and tested on them; up to **10 custom
fields / export column mappings**; assisted setup of Ollama and one local model that the customer has accepted the licence for (if the PC is capable; if
not, rules-only mode is configured and documented); an acceptance session measuring results on up to 30 of the customer's real documents with a short
written results sheet (counts per field, not a promise); **support period 90 days**, including up to 2 remote sessions and 1 template adjustment round;
updates to that version for 12 months on request.
**Excludes:** more than the stated templates/fields (quoted separately), additional computers (second licence at an agreed rate), development of new
integrations, guarantees of accuracy or tax compliance, unlimited support.

### Optional extras (quote separately): extra computer licence, more templates, bulk back-entry, accounting-software export mapping, on-site training,
annual support renewal.

## 3. Rough margin check (fill with your numbers)
Time per Standard job ≈ 2 h install + 1 h walkthrough + ~1 h support allowance; Professional ≈ 6-8 h including templates and acceptance session.
At ₹9,999 and ₹19,999 an hourly return of roughly ₹2,500 and ₹2,500-3,300 respectively results **before** taxes, travel and certificate costs - check
this against your own rate. If templates take longer than planned, Professional becomes unprofitable: cap the template count in the quote.

## 4. What you must not promise
Unlimited support; accuracy percentages; tax or statutory compliance; that scans of any quality will work; that AI will fix poor documents; that
unsigned installers never trigger warnings; that backups are encrypted (they are not).
