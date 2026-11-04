# Answer key — seeded defects (INSTRUCTORS ONLY)

Severity/priority are guidance for marking; accept well-argued alternatives. “Fixed in 1.1” refers to `?build=1.1`.

## VEL Practice Shop (web)

| ID | Req | Defect | How a tester finds it | Severity / priority | Build 1.1 |
|---|---|---|---|---|---|
| D1 | REQ-003 | Age 18 rejected (`> 18`) | BVA lower boundary | S2 / High | Fixed |
| D2 | REQ-003 | Age 61 accepted (`<= 61`) | BVA upper boundary + 1 | S3 / Medium | Present |
| D3 | REQ-002 | `a@b` accepted as e-mail | EP on e-mail formats | S3 / Medium | Present |
| D4 | REQ-004 | Password without uppercase accepted | Rule-combination cases | S2 / High | Present |
| D5 | REQ-005 | Confirm password compared case-insensitively | Case-variant data | S3 / Medium | Regression (see R1) |
| D6 | REQ-001 | Name of spaces only accepted (no trim) | Error guessing | S3 / Medium | Present |
| D7 | REQ-006 | Terms checkbox not enforced | Negative case | S2 / High | Fixed |
| D8 | REQ-008 | Unknown e-mail → “Email not registered” (user enumeration) | Compare messages for unknown vs wrong password | S2 / High — **security: escalate** | Present |
| D9 | REQ-009 | Lock on 4th failure instead of 3rd (`?lock=5` helps) | State-transition test | S3 / Medium | Present |
| D10 | REQ-010 | Total not shown to 2 decimals; floating-point noise (`30.299999999999997`) | Notebook × 3, Pen × 3 | S2 / High | Present |
| D11 | REQ-010 | GST floored instead of rounded (10.10 → 1.81, expected 1.82) | Hand-calculate expected values | S2 / High | Present |
| D12 | REQ-010 | Quantity 0 and 11 accepted | BVA 0/1/10/11 | S3 / Medium | 11 rejected by accident (cap 5) |
| D13 | REQ-011 | Idle timer is not reset by activity (`?idle=10`) | Interact at 6 s, observe expiry at 10 s | S2 / High | Present |
| D14 | REQ-012 | Back after logout shows shop (stale remembered user) | Session testing | S2 / High (data exposure) | Present |
| D15 | REQ-013 | Register card has `min-width: 420px` → horizontal scroll at 360 px | Responsive test | S3 / Medium | Present |
| D16a | REQ-014 | Login e-mail field has no label (placeholder only; **axe-core does not flag it**) | Manual a11y review | S3 / Medium | Present |
| D16b | REQ-014 | Product images have no `alt` | DevTools / axe-core | S3 / Medium | Present |
| D16c | REQ-014 | Focus indicator removed (`outline: none`) | Keyboard-only walk-through | S2 / High | Present |
| D17 | — | Label typo “Totl” | Visual review | S4 / Low | Present |
| D18 | — | Passwords stored in plain text in `localStorage` | DevTools → Application | **Security: escalate** | Present |
| D19 | REQ-008 | Login password field shows typed characters (`type=text`) | Visual / inspection | S2 / High | Present |

**Regressions introduced in build 1.1:** **R1** confirm password never checked (REQ-005) · **R2** quantity capped at 5 instead of 10 (REQ-010).

## VEL Practice API

| ID | Defect | Expected per contract | Severity / priority |
|---|---|---|---|
| A1 | `POST /api/products` accepts `price: 0` | 400 | S3 / Medium |
| A2 | Duplicate product name accepted | 409 | S3 / Medium |
| A3 | `GET /api/products/abc` → 500 with internal detail (file/line) | 400, no internals | S2 / High — leakage, mention to coordinator |
| A4 | Second `DELETE` of the same id → 204 | 404 | S3 / Medium |
| A5 | `GET /api/orders` works **without** a token | 401 | **S1 / High — security: escalate privately** |
| A6 | Product 3 `price` returned as string `"49.99"` | number | S2 / High (schema) |
| A7 | Login with empty password for a valid e-mail returns a token | 400/401 | **S1 / High — security: escalate** |
| A8 | `limit` above 50 accepted | 400 | S3 / Medium |
| A9 | `PUT` ignores `stock` | 200 with updated stock | S2 / High |
| A10 | 404 for unknown product has `Content-Type: text/html` | `application/json` | S3 / Medium |

## Verification record (4 November 2026, build environment)
* API reference suite: **30 passed, 10 xfailed** (A1–A10 all confirmed; no unexpected passes).
* UI reference suite, build 1.0: **23 passed, 22 xfailed** (D1–D17 confirmed; D18 and D19 are inspection findings, not automated).
* UI reference suite, build 1.1: **5 failed, 22 passed, 18 xfailed** (R1, R2 and the quantity-11 defect fixed by accident), as designed.
* Accessibility: axe-core flags D16b but not D16a (teaching point). Locust (10 users, 15 s): 0 failures.
