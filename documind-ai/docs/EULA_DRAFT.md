# DocuMind AI Professional - End User Licence Agreement (DRAFT FOR LAWYER REVIEW)

> **This is a draft template, not legal advice and not a statement that it is enforceable in any jurisdiction.** A qualified lawyer in your
> jurisdiction must review and adapt it (including Indian contract, IT Act, consumer and GST points) before use. Bracketed items are blanks.

## 1. Parties and definitions
"**Licensor**" is [name, address]. "**Customer**" is [name]. "**Software**" is DocuMind AI Professional version [x.y.z] as installed.
"**Documents**" are files the Customer imports. "**Data**" means the Documents and everything extracted from them.
"**Third-Party Components**" are listed in THIRD_PARTY_NOTICES.md and are governed by their own licences.

## 2. Licence grant and scope
2.1 Licensor grants Customer a non-exclusive, non-transferable, non-sublicensable licence to install and use the Software on **[one (1)] computer**
identified at installation ([Standard]) / **[__] computers** ([Professional/agreed]), for the Customer's internal business purposes only.
2.2 **Users:** any person authorised by the Customer who can sign in to that computer. No concurrent-user or seat counting is enforced by the Software.
2.3 **Restrictions:** no copying except one backup copy of the installer; no reverse engineering except as the law permits and as the licences of
Third-Party Components (including the LGPL right to replace Qt libraries) allow; no renting, resale, or offering the Software as a service to others;
no removing notices.

## 3. Third-Party Components
They are licensed under their own terms, which prevail for those components. Optional components (Tesseract, Ollama, AI models) are installed and
licensed separately; **the Customer is responsible for complying with the licence of any AI model it chooses to use.**

## 4. Delivery, installation, updates
4.1 Licensor (or its installer) installs the Software as described in the Customer Installation Agreement (Schedule A).
4.2 **Updates:** [Standard: bug-fix releases of the licensed major version for 6 months from installation] / [Professional: 12 months]. Updates are
delivered only on request and installed by the Licensor or by the Customer from a file; the Software never updates itself. Major versions may be
charged separately.

## 5. Support
Support covers installation, configuration and reproducible defects in the Software during the support period in Schedule B, by [channel] in
[days/hours], response target [n] business days (a target, not a guarantee). **Excluded:** fixing extraction results on particular documents (other
than defects), training on third-party software, hardware/network/Windows problems, custom development, data recovery without a valid backup.

## 6. Data, privacy and backups
6.1 **Ownership:** Customer owns the Data. Licensor acquires no rights to it.
6.2 **Local processing:** the Software stores and processes Data on the Customer's computer only. The Software has no telemetry. Licensor does not
receive Data unless Customer sends a file (for example a diagnostics report, which excludes document content) or grants access.
6.3 **Customer responsibilities:** Windows account security, disk encryption, access control, regular backups (Settings > Data & backup), retention
settings, and lawful handling of personal data in Documents. Backups are not encrypted by the Software.
6.4 Licensor's personnel who access the Customer's computer during installation or support will do so only with the Customer's permission and will not
copy Data.

## 7. Accuracy and responsibility - important
7.1 The Software uses automated text extraction, optional OCR and optional AI. **Results can be wrong or incomplete** and must be checked by a competent
person before being used for payment, accounting, tax or compliance. Customer is solely responsible for decisions based on the Data.
7.2 No accuracy, completeness, tax-compliance or fitness-for-purpose guarantee is given. Any accuracy figures supplied are measured on stated
samples only (see ACCEPTANCE_TESTS.md) and do not predict results on other documents.

## 8. Warranty and liability  [lawyer to adapt]
8.1 Licensor warrants for [90] days that the Software substantially performs as the User Guide describes; the remedy is repair or replacement or
refund of the licence fee for the affected licence. 8.2 Otherwise the Software is provided "as is" to the extent the law allows.
8.3 Licensor is not liable for indirect or consequential loss, lost profits or data loss (Customer must keep backups); total liability is limited to the
fees paid in the 12 months before the claim, except where the law does not allow a limit.

## 9. Fees and taxes
Fees are in the quotation. Taxes (e.g. GST), travel, third-party software or hardware, and custom work are separate unless the quotation says otherwise.

## 10. Term and termination
Perpetual for the licensed version unless terminated. Either party may terminate for uncured material breach after [30] days' notice. On termination
Customer stops using the Software and removes it; **Data remains Customer's and the Software's data folder is not deleted automatically.** Sections 6-8 survive.

## 11. General
Governing law and courts: [jurisdiction]. Entire agreement; changes in writing; if a clause is invalid the rest continues. Assignment only with consent.

---

# Customer Installation Agreement (DRAFT - Schedule A)

1. **Scope of installation:** install DocuMind on [computer name/serial]; create the data folder at [path]; configure OCR (Tesseract + languages [..])
   [and Ollama + model [..] **only if the customer has accepted that model's licence**]; import [n] sample documents; show the user the Quick start.
2. **Customer provides:** administrator access during the session, a Windows account that will be used daily, enough disk space (see INSTALLATION.md), a
   backup location, and sample documents (copies, which the installer returns/deletes afterwards).
3. **Installer will not:** copy customer documents off the machine, use the customer's accounts, install other software without consent, or change
   security settings beyond those listed in the handover sheet.
4. **Acceptance:** the Acceptance Tests in docs/ACCEPTANCE_TESTS.md, signed on the handover sheet (docs/CUSTOMER_HANDOVER.md).
5. **Out of scope unless quoted:** template building for new layouts, bulk historic data entry, integration with accounting software, training sessions.
6. **After installation:** the support period in Schedule B starts on the handover date.

# Schedule B - support periods (fill in)
Standard: [__] days from handover, [email/phone], [hours]. Professional: [__] days, includes [__] remote sessions and [__] template adjustments.
