# Stage S07 — Build the Core (Gates G6 and G7)

## Purpose
Build the parts that do **not** need AI: the business rules, the data structures, the API and the review screen. These parts are exact, fast to test, and make the AI step safe later.

## Who and when
- **Owner:** Developer (you, with the Technical Lead reviewing). **Approvers:** G6 customer (workflow and screens), G7 Technical Lead (code).
- **Time:** 2–3 weeks.

## Before you start
S06 done; tickets in *Ready*; you have run `reference-project` tests (S00).

## The method (repeat for every ticket)

```
Read ticket → Write a failing test → Write the smallest code that passes → Refactor → Run all tests
→ Commit → Push → Pull request → Review → Merge → Move ticket
```

This loop is called **test-driven development**. It forces you to understand the requirement before coding and gives you a safety net for later changes.

## Worked tutorial: the GSTIN rule (follow along in `reference-project/`)

**Ticket GBA-12: validate GSTIN.** Acceptance: a 15-character GSTIN is valid only if the state code is 01–38 and the last character equals the checksum of the first 14.

1. **Branch:** `git switch -c feature/GBA-12-gstin-check`
2. **Write the test first** (see `tests/test_rules.py`):
   ```python
   def test_valid_gstin_accepts_known_good():
       assert is_valid_gstin("27AAPFU0939F1ZV")
   ```
   Run `python -m pytest -q`. It fails because the function does not exist yet. A failing test proves the test can fail.
3. **Write the code** (see `app/rules.py`): the function `gstin_check_char` computes the check character; `is_valid_gstin` checks length, characters, state code and the check character.
4. **Run the tests.** They should pass.
5. **Add the bad cases:** wrong check character, 14 characters, state code 99, illegal symbol, empty string. Each is one small test. Bad cases catch more bugs than good cases.
6. **Commit:** `git commit -m "GBA-12: add GSTIN check-character validation"`.
7. **Open a pull request**, describe what and how you tested, request review.

## Reading the example code

| File | What to notice |
|---|---|
| `app/rules.py` | No database, no internet, no randomness. Same input always gives same output. This is **pure** logic, the easiest kind to test. Money uses `Decimal`, never `float`, because floats make rounding errors. |
| `app/main.py` | A thin FastAPI layer. It checks the shape of incoming data (types, required fields) then calls the rules. Business logic is **not** written here. |
| `tests/test_rules.py` | Each test has the hand calculation in a comment (e.g. 10,000 + 900 + 900 = 11,800). Hand-calculated expected values are our standard. |
| `tests/test_api.py` | Tests the service the way another program would call it. |

### Run the service
```bash
cd ai-automation-os/reference-project
uvicorn app.main:app --reload
```
Open `http://127.0.0.1:8000/docs`. You get an automatic page to try `POST /bills/validate`. Send a valid bill; the answer is `route: auto_post`. Change the total; the answer becomes `human_review` with reason `TOTAL_MISMATCH`.

## Step by step for the whole stage

1. **Data models:** define the tables (`bills`, `review_tasks`, `audit_log`) in `src/models/`; add database migrations (versioned database changes) so every environment gets the same structure.
2. **Rules engine:** implement each numbered business rule from S02 as a small function with tests. One rule, one function, one or more tests.
3. **Schemas:** put JSON schemas in `src/schemas/`; load them in the API so bad input is rejected with a clear error.
4. **API endpoints** from the API spec; each returns clear status codes (200 OK, 400 bad data, 401 not authorised, 404 not found, 422 invalid shape, 500 our fault).
5. **Authentication and permission:** every endpoint requires a valid credential; every query is filtered by `customer_id`.
6. **Idempotency:** accept an idempotency key; if the same key arrives twice, return the first result and do nothing again. Test it.
7. **Audit log:** record who or what did what and when, for every important action. Never log secrets or full personal data.
8. **Review screen:** show the bill image beside the extracted fields; show rule failures in plain words; show exactly what will be posted if approved; buttons: Approve, Edit, Reject.
9. **Workflow in n8n** (or the chosen engine): trigger → validate → route → notify; export the workflow JSON into `workflows/n8n/` and commit it.
10. **Customer review (G6):** demo the screens and the flow with fake data; collect changes (through change control if outside scope).
11. **Technical review (G7):** Technical Lead reviews code quality, tests, documentation.

## Coding standards (short)
- Functions do one thing and are short enough to read without scrolling.
- Names say what things are: `validate_bill`, not `process`.
- Comments explain **why**; the code shows **what**.
- Every public function has a one-line docstring.
- No copy-pasted blocks; make a function.
- Handle errors deliberately; never leave an empty `except:`.
- No secrets, no print-debugging left in.

## Deliverables
- Working code in the repository with ≥ 1 test per business rule.
- Database migrations; API documentation (`/docs` plus `docs/api/`).
- Review-screen prototype; workflow export.
- Pull requests merged with approvals; tickets moved to Done.

## Done checklist
- [ ] Every numbered business rule has code **and** tests.
- [ ] All tests pass locally and in CI.
- [ ] Idempotency tested: the same request twice posts once.
- [ ] Every query filters by customer.
- [ ] Audit log records key events without secrets.
- [ ] Review screen shows the exact action and consequence.
- [ ] Customer approved screens (G6); Technical Lead approved code (G7).

## Common mistakes
- Writing code first and "adding tests later" (they never come).
- Using `float` for money.
- Mixing business rules into the web layer.
- Huge pull requests nobody can review.
- Hard-coding values that belong in settings or rule files.

## Gate sign-off
**G6:** customer signs the screen and workflow approval. **G7:** Technical Lead signs the code review record.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
