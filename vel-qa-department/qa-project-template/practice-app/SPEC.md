# VEL Practice Shop — requirements specification (v1.0)

> A QA **training target** with deliberate defects. Do not deploy. Run: `python3 -m http.server 8000 --directory practice-app` → <http://localhost:8000>

Views: Login (`#/login`), Register (`#/register`), Shop (`#/shop`). Data is stored in the browser only.

| ID | Requirement |
|---|---|
| REQ-001 | Registration: **Full name** is required, 2–40 characters after trimming spaces. |
| REQ-002 | Registration: **Email** is required and must be a valid address (`name@domain.tld`). |
| REQ-003 | Registration: **Age** is required, a whole number from **18 to 60 inclusive**. |
| REQ-004 | Registration: **Password** 8–20 characters with at least one digit and at least one uppercase letter. |
| REQ-005 | Registration: **Confirm password** must match the password exactly (case-sensitive). |
| REQ-006 | Registration: the **Terms** checkbox must be ticked. |
| REQ-007 | A successful registration shows “Registration successful” and opens the login view. |
| REQ-008 | Login with registered credentials opens the shop. Any failure shows the generic message “Invalid email or password”. |
| REQ-009 | On the **3rd consecutive failed login** the account locks for 30 seconds and the message “Account locked. Try again in 30 seconds.” is shown; attempts during the lock show the same message. |
| REQ-010 | Shop: quantity per item is a whole number **1–10**. Cart total = sum(price × quantity) shown to 2 decimals; GST 18 % shown to 2 decimals (standard rounding). |
| REQ-011 | Session expires after **90 seconds of inactivity**: “Session expired. Please log in again.” and the login view opens. |
| REQ-012 | Logout ends the session. Using the browser Back button afterwards must **not** show shop content. |
| REQ-013 | The application is usable at **360 px** viewport width with no horizontal scrolling. |
| REQ-014 | Accessibility: every input has a programmatic label; every image has alt text; all functions work by keyboard with a visible focus indicator. |

## Testing aids (URL parameters)
`?build=1.0` (default) or `?build=1.1` (second build for the regression project) · `?idle=10` shortens the REQ-011 timeout to 10 s · `?lock=5` shortens the REQ-009 lock to 5 s.

## Products
Notebook 10.10 · Pen 5.50 · Backpack 49.99 · Desk Lamp 24.00 (shown with two decimals in the list).
