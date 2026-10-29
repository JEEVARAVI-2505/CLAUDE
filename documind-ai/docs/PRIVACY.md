# Privacy and data handling

**Summary:** everything stays on the customer's computer. DocuMind has no accounts, no telemetry, no advertising and no cloud service.

| Question | Answer |
|---|---|
| Where is data stored? | `%LOCALAPPDATA%\DocuMindAI` (database, copies of imported files, logs, backups, exports unless another folder is chosen). |
| Is anything sent over the network? | Not by DocuMind, except requests to the AI server address configured in Settings, which must be this computer (127.0.0.1) unless an administrator explicitly allows otherwise. Verified by an automated test that blocks all sockets (`tests/security/test_local_only.py`). Tesseract is a local process. |
| Optional components with network behaviour | **Ollama** (customer-installed) may download models/updates when the customer asks it to. **Windows**/antivirus have their own behaviour. DocuMind never downloads a model. |
| What is in logs? | Versions, error codes, timings, counts, document numeric IDs. Not document text, extracted values, supplier/customer names, or file names. E-mail addresses, long digit strings, secrets and user-profile names are redacted. Test: `test_logs_contain_no_document_text_or_filenames`. |
| Diagnostics report | Contains no document content; the customer reads it before sending. |
| Who can read the data? | Anyone with access to the Windows account/disk. Use Windows account security and BitLocker. Backups are not encrypted. |
| Temporary files | OCR page images are written to `<data>\temp` and deleted immediately after use; leftovers older than 1 h are removed at start. |
| Deletion | See ADMIN_GUIDE.md. Overwrites the stored original and erases rows including the audit trail; cannot recall earlier exports/backups; SSD/shadow-copy residue possible. |
| Retention | Configurable; default is to keep until deleted. |
| Personal data in documents | Invoices can contain names, addresses, tax IDs, bank details. The customer is the controller and must comply with applicable law (e.g. India's DPDP Act, GDPR where relevant). This document is not legal advice and DocuMind is not certified for any regime. |
| E-mail / third-party sending | Never done by the product. Any future integration would need explicit configuration and consent. |
| AI model data | Prompts and documents are sent only to the local Ollama process. The AI cannot take actions; its output is validated, grounded against the document and shown for review. |

## Limitations to state to customers
* No claim of compliance with any standard is made; no independent security audit has been performed.
* A person with access to the PC can read approved and unapproved data and the stored originals.
* Memory and swap may hold document text transiently.
