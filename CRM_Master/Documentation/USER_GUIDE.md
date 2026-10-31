# CRM MASTER — User Guide

## 1. First launch
A short setup asks for your name, your default **phone country code** (91 = India; used for numbers written without a country prefix), the theme, and whether to
load a small **sample dataset** (about 100 fictional, deliberately messy contacts) so you can try every feature. Everything is stored under
`%LOCALAPPDATA%\CRM Master`.

## 2. Importing data (Import Data)
1. Drag a `.xlsx / .xlsm / .csv` file onto the drop zone (or **Browse**). Excel files with several sheets show a sheet picker. The first rows are previewed.
2. **Map your columns.** CRM MASTER suggests a target for every column from its name (including typos like "Emial"), and from the data itself (a column of
   email-looking values becomes Email). Change any mapping; choose *Custom field…* to keep extra columns; *(ignore)* to skip one. You need at least a name, email or phone.
3. Choose what happens when a row matches an existing contact (same email or phone): **skip**, **import and flag**, or **propose an update** (reviewed later in Bulk Update).
4. **Check file (dry run)** shows how many rows are ready, have warnings, already exist, repeat inside the file, or cannot be imported — nothing is written.
5. **Import contacts.** The import is one transaction: *Cancel* or any failure leaves your data untouched. If you already have contacts, a backup is taken first.
   Rejected rows and the reason are kept in the import report (history table → *Report*).

## 3. Cleaning (Data Cleaning)
Choose the steps, press **Analyse data**, review the **before / after** table and the problems found, then **Apply changes**. A backup is taken first, previous values are
recorded, values you edited since the analysis are never overwritten, and **Undo this cleaning** restores everything. Exact duplicates (all fields identical) are only removed
if you tick that option; the full records stay in the audit log.

## 4. Duplicates (Duplicate Detection)
Pick what counts as evidence (email, phone, name, company, fuzzy names) and the thresholds, then **Scan**. Groups are classed **Exact**, **High**, **Possible** or **Low**.
Select a group to compare records side by side: green = same, amber = conflicting (choose which value to keep), blue = only one record has it. Choose the **master** record,
untick records to leave out, then **Merge selected fields**, **Skip for now**, or **Mark as separate customers** (remembered, never proposed again).
*Merge all exact* only touches groups classed Exact. Every merge takes a backup and can be undone from Activity Logs.

## 5. Contacts
Search (name, email, phone in any format, company, city), filter by status or data-quality flag, sort by clicking headers, page through large lists. Tick rows for bulk delete or
export. Double-click a row for the profile (details, custom fields, **activity history**). Adding a contact warns about an existing contact with the same email/phone.

## 6. Bulk update
Choose a file with updated values (or review proposals stored by an import), map columns, choose the **policy** and **fields**, then **Compare and propose updates**.
Every proposed change is listed with its current and incoming value; tick the ones you approve. Nothing is written until **Apply approved changes**
(backup first; previous and new values recorded; **Undo** and an Excel **update report** available).

| Policy | Behaviour |
|---|---|
| Empty fields only | fills blanks in the selected fields; never changes a value |
| All selected fields | replaces existing values in the selected fields with non-blank incoming values |
| Preserve existing | existing values always win; fills blanks in every field |
| Only if valid | like "all selected", but incoming email/phone/website/date/country must be valid |
| Skip conflicting records | a record with any conflicting field is skipped entirely |
| Manual approval | everything proposed, nothing pre-approved |

## 7. Validation
Switch rules on/off and **Validate all contacts**. Findings are Errors / Warnings / Suggestions with a suggested correction where possible (`gmial.com → gmail.com`, standard phone format,
proper-case names…). **Email checks are syntax-only** — a valid-looking address is not proof the mailbox exists. Apply a suggestion individually or for everything shown.

## 8. Automation rules
A rule = **trigger** (contact imported / created / edited, imported row matches existing, duplicates detected, import completed, manual) + **conditions** (all / any) + **actions**
(standardise phone, flag for review, mark incomplete, set a field, propose update, generate import report…). Six starter rules are included (four enabled). *Run now on all contacts*
applies a rule to existing data (backup, audit, undo). Execution history is kept per rule. The master switch is in Settings.

## 9. Reports
Choose a report, date range, contact category (status) and processing status, **Preview**, then export to **Excel**, **CSV** or **PDF**. PDFs contain basic Latin text — use Excel/CSV for other scripts.

## 10. Activity Logs, backup and restore
Every create / update / delete / import / cleaning / merge / bulk update / export / rule run is logged with previous and new values. Select an entry to see details; reversible batches can be undone.
**Settings → Backup and restore** creates consistent snapshots and restores them (a safety copy of the current data is always kept). Old log entries are purged after your retention period.

## 11. Privacy & security
Local-first: no telemetry, no network calls. Log files contain operation names and exception types, not contact details (the audit table inside your database does hold previous/new values — it is your data, on your machine).
Spreadsheet exports neutralise formulas (`=`, `@`, …) so a hostile cell cannot execute in Excel. Credentials for future CRM connectors are protected with Windows DPAPI and never stored in plain text.
