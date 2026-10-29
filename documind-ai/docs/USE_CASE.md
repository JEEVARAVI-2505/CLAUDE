# Who this is for, and the job it does

## The person
**An accounts clerk or owner at a small firm** (accounting practice, contractor, distributor, wholesaler, procurement desk) who receives 20 to a few hundred
supplier invoices and purchase orders a month as PDFs, WhatsApp/e-mail attachments, phone photos and scans, and then re-types them into Excel or the
accounting software for the accountant or for GST filing.

## The problem today
* Re-typing is slow and mistakes creep in (a wrong digit in a total, an invoice entered twice, a wrong date).
* Nobody notices when an invoice's own maths does not add up until the books do not reconcile.
* Sending client invoices to online AI/OCR services is a privacy worry and often costs a monthly fee.

## What DocuMind does for them (the whole job in three steps)
| Step | What the person does | What DocuMind does |
|---|---|---|
| **1 Add documents** | Drags the month's files (or a whole folder) into the box | Reads each file on the computer (digital PDFs directly, scans/photos with OCR), straightens sideways pages, rejects unusable files with a reason, skips files already added, never stops because one file is bad |
| **2 Check and approve** | Opens each document: page on the left, what was read on the right. Fixes anything wrong by typing over it. Presses **Approve** (or one button for every document with no warnings) | Finds supplier, invoice no., dates, GSTIN, currency, line items, tax and totals; **re-adds the numbers independently** and flags lines, subtotals, tax or totals that do not add up; warns about missing fields, ambiguous dates, possible duplicates, invalid GSTIN check digits; keeps a log of every change |
| **3 Export to Excel** | Picks "Purchase register (for my accountant)", picks the month, saves | Writes a clean Excel file with real dates and numbers, a Summary sheet (totals by month and by supplier, currencies never mixed) and only documents the person approved |

**The promise is not "no checking". It is "checking takes minutes, and mistakes cannot hide".** The software reads; a person decides.

## Worked example: month-end (about 15 minutes for 30 invoices once set up)
1. Open DocuMind with `START_DOCUMIND.bat`. On **Home** the first box says *1 Add documents: nothing added yet*.
2. Drag the folder "October supplier invoices" into **1 Add documents**. The list shows each file being read; two files are marked *Not imported*: a `.docx`
   (reason shown) and a duplicate of a file already added.
3. Home now says *28 documents need your check*. Click **Start checking**.
4. In **2 Check and approve** press **Approve the 19 with no warnings** and confirm. These 19 passed every check (all required fields found, every total adds up).
5. Go through the remaining 9 (amber fields are the ones to look at): two had a smudged digit in the total, so the Checks tab shows *"Subtotal - discount + tax =
   11,800 but the total is 11,300"*. Type the correct value from the page; the warning clears; Approve. One is a repeated invoice: Reject.
6. Open **3 Export to Excel** > *Purchase register* > *Last month* > **Create the Excel file**. Send the file to the accountant. The Summary sheet already shows
   total purchases and tax per supplier.

## How it stays easy to understand
* The first screen shows the three steps with live counts and one "what to do next" sentence.
* A short how-to banner on each step (can be hidden in Settings).
* Plain words: *Read by software / You changed it / You approved it / Not found* instead of technical terms.
* Amber = look here. Red = does not add up. Nothing is ever silently changed.
* First-run questions are asked once (data folder, how dates are written, scans, optional AI) and remembered.
* **Try it with sample documents** on Home creates fictional invoices to practise on.

## What it is not
Not an accounting package, not a tax adviser, not a payment tool. It does not e-mail anyone or upload anything. It does not promise every invoice is read
correctly; it promises every difference it can detect is shown to you before you rely on the data. Scanned or photographed invoices need the free
Tesseract add-on (see INSTALLATION.md). A local AI model is an optional extra for unusual layouts.

## Fit check before you rely on it (10 minutes)
Run 20-30 of **your** real invoices through steps 1-2 and count how many fields you had to correct (the "You changed it" label shows them). If your suppliers
use layouts the standard reader does not understand, the Professional package adds templates for them (see COST_MODEL.md and ACCEPTANCE_TESTS.md).
