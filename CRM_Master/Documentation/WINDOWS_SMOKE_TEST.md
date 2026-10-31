# Windows smoke test (≈ 45 minutes)

The WPF interface was built and compiled on a Linux machine and has **never been launched there**. Run this checklist once on a clean Windows 10 and 11 machine
(no .NET installed) against the published build or installer **before** giving the product to anyone. Tick each line; any failure → note the screen, the steps, and the newest file in `%LOCALAPPDATA%\CRM Master\Logs`.

Prep: install via the installer or unzip the portable zip. For data use `SampleData\sample-contacts.csv` and `sample-update.csv`.

## A. Start-up and shell
- [ ] A1 App starts; first-run dialog appears (no crash). Entering `+91` or `abc` as country code shows an inline error; `91` continues.
- [ ] A2 With "load sample dataset" ticked, the Dashboard shows ~100+ contacts; cards, growth chart (one point per month, last = total) and quality bars are drawn.
- [ ] A3 Sidebar: all 12 items navigate; collapse/expand button animates; title in the top bar changes; DB pill is green; processing pill shows "Idle".
- [ ] A4 Theme: profile menu → switch to dark. Every screen visited below is readable (no black-on-dark text, no white controls). Switch back.
- [ ] A5 Second launch while running shows "already running" and exits.
- [ ] A6 Window resize to 1100×680 and maximise: no clipped primary buttons, scrolling works (mouse wheel over tables passes to the page at the end of a table).
## B. Import
- [ ] B1 Drag `sample-update.csv` onto the drop zone → preview + mapping appear; every column maps (Email, Designation→Job Title, City, Company, Mobile→Phone, Lead Source).
- [ ] B2 Dry run: counts shown; most rows "Already in CRM". Choose a worksheet in an Excel file with 2 sheets (create one) → sheet picker works.
- [ ] B3 Import an `.xlsx` of ~50,000 rows: progress bar moves, UI stays responsive, Cancel mid-way → "cancelled", contact count unchanged.
- [ ] B4 Open a corrupted file (rename a .txt to .xlsx) → friendly error, no crash. Open an Excel file while it is open in Excel → readable or a clear message.
- [ ] B5 History table lists each attempt (including the failed/cancelled ones); **Report** opens an Excel file listing rejected rows and reasons.
## C. Contacts
- [ ] C1 Search "9876" / an email fragment / company; filters (status, quality) update the list; sorting by clicking each header works; paging buttons work; page text correct.
- [ ] C2 Add contact (name only, then invalid email → warning dialog "Save anyway"); duplicate-email warning appears for an existing email.
- [ ] C3 Double-click a row → profile; Edit → Save; Activity history tab shows the change with previous/new values; custom field add/save.
- [ ] C4 Tick 3 rows → Delete → confirm dialog; contacts vanish; Activity Logs shows the deletion; **Undo** (Activity Logs → select → Undo this whole operation) restores them.
- [ ] C5 Export Excel / CSV / PDF (selected rows and filtered); files open; phone numbers stay text (`+91 …`).
## D. Cleaning / Validation / Duplicates
- [ ] D1 Cleaning: Analyse → before/after rows plausible; Apply → success banner; **Undo this cleaning** restores original text (spot-check one name).
- [ ] D2 Validation: Run → cards and list; "Apply" on a `gmial.com` suggestion changes that contact only; disclaimer banner visible.
- [ ] D3 Duplicates: Scan → groups with classes; select an Exact group → side-by-side columns, colours, master radio, "Value to keep" boxes; Merge → group disappears, contact count drops by 1; **Mark as separate** on another, rescan → not proposed again.
## E. Bulk update / Automation / Reports
- [ ] E1 Bulk Update with `sample-update.csv`, policy "Manual approval": nothing ticked; tick a few, Apply → only those change; Undo works; Excel update report opens.
- [ ] E2 Policy "Skip conflicting records" skips records with conflicts (reason shown).
- [ ] E3 Automation: list shows six rules; New rule (trigger Manual, condition City equals Pune, action Set status = Local) → Run now → contacts change; History shows the run; disable/delete work.
- [ ] E4 Reports: each of the 7 types previews; export all three formats; PDF opens with a title bar, table, page numbers.
## F. Settings, backup, restore, about
- [ ] F1 Change country code or required fields → prompt to re-evaluate; dashboard numbers change accordingly. Invalid thresholds (low ≥ high) show an error.
- [ ] F2 Back up now → entry appears; delete all contacts; Restore selected → contacts return; a "PreRestore" backup exists.
- [ ] F3 Restore from a non-backup file → refused with a message, data untouched.
- [ ] F4 About: version, licence status banner, notices text, folders open.
- [ ] F5 Notifications bell shows entries and a count badge; Clear works.
## G. Installer
- [ ] G1 Installer shows the evaluation notice, desktop-shortcut option; Start menu + desktop shortcuts launch the app.
- [ ] G2 Install over an older build keeps data. Uninstall asks about deleting data; "No" keeps `%LOCALAPPDATA%\CRM Master`.
- [ ] G3 SmartScreen/AV behaviour noted (expected for unsigned builds).
