# Part 8: First Practical Assignment: The Client Registry API

**Project 1 of 10 · Week 4 · Gate 1 deliverable.** The complete reference solution is in `assignment-01/` and its 12 automated tests pass.

## 8.1 Definition

An **API** (Application Programming Interface) is a contract that lets one program ask another program to do something or return data, using agreed requests and responses. A **REST API** does this over HTTP using resources (nouns such as `clients`) and standard methods (`GET`, `POST`, `PATCH`, `DELETE`).

## 8.2 Business purpose

VEL needs a **Client Registry**: the first internal API in the department's inventory. Later projects connect it to PostgreSQL, add OAuth, and sync it with a CRM. Building it teaches the habits that every client API needs: validation, a standard error shape, authentication, pagination and tests.

## 8.3 Real-world example

A sales app sends `POST /v1/clients` when a lead is won. The accounting app later calls `GET /v1/clients?status=active` to create customer records. Neither needs to know how the other stores data; they share only the API contract.

## 8.4 Prerequisites

Week 1 to 3 complete · comfortable with Python functions, dicts and exceptions · know HTTP methods and status codes · Git basics.

## 8.5 Required tools

Python 3.12+ · VS Code · Git · Postman or Insomnia · a terminal.

## 8.6 Installation

```bash
cd assignment-01
python -m venv .venv
source .venv/bin/activate          # Windows PowerShell: .venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

## 8.7 Configuration

Copy `.env.example` to `.env`, set a long random key, and **never commit `.env`** (it is in `.gitignore`).

```bash
cp .env.example .env
# generate a key:
python -c "import secrets; print(secrets.token_urlsafe(32))"
```

`.env.example`:

{{file:assignment-01/.env.example|bash}}

`requirements.txt`:

{{file:assignment-01/requirements.txt|text}}

## 8.8 Step-by-step implementation

**Specification you must meet**

| Method and path | Purpose | Success | Errors |
|---|---|---|---|
| `GET /health` | Liveness (public) | 200 | none |
| `POST /v1/clients` | Create client | 201 + `Location` header | 401, 409 duplicate email, 422 |
| `GET /v1/clients` | List with `page`, `page_size`, `status`, `industry`, `sort` | 200 | 401, 422 |
| `GET /v1/clients/{id}` | Read one | 200 | 401, 404 |
| `PATCH /v1/clients/{id}` | Partial update | 200 | 401, 404, 409, 422 |
| `DELETE /v1/clients/{id}` | Delete | 204 | 401, 404 |

**Order of work (do each step, then commit):**

1. Create the folder layout: `app/` (code) and `tests/`.
2. **Schemas first** (`schemas.py`): the contract. Validation lives here.
3. **Store** (`store.py`): an in-memory class; no HTTP knowledge.
4. **Errors** (`errors.py`): one error shape for every failure.
5. **Security** (`security.py`): API-key dependency using constant-time comparison.
6. **Routes** (`main.py`): thin handlers, request-ID middleware, logging.
7. **Tests** (`tests/test_clients.py`): write the failing cases first, then make them pass.
8. Run the server, explore `/docs`, and call it from Postman.
9. Write the README and export the OpenAPI file.

### 8.9 Complete code

**`app/schemas.py`**: request and response models

{{file:assignment-01/app/schemas.py|python}}

**`app/store.py`**: storage (replaced by PostgreSQL in Project 2)

{{file:assignment-01/app/store.py|python}}

**`app/errors.py`**: the company error standard

{{file:assignment-01/app/errors.py|python}}

**`app/security.py`**: API-key authentication

{{file:assignment-01/app/security.py|python}}

**`app/main.py`**: routes, middleware, logging

{{file:assignment-01/app/main.py|python}}

**`tests/test_clients.py`**: automated tests (success **and** failure)

{{file:assignment-01/tests/test_clients.py|python}}

## 8.10 Expected output

Run the tests:

```bash
python -m pytest -q
```

```text
............                                                             [100%]
12 passed
```

Start the server and call it:

```bash
export CLIENT_API_KEY="$(python -c 'import secrets; print(secrets.token_urlsafe(32))')"
uvicorn app.main:app --reload
```

```bash
curl -s -X POST localhost:8000/v1/clients \
  -H "X-API-Key: $CLIENT_API_KEY" -H "Content-Type: application/json" \
  -d '{"name":"Acme Traders","email":"ops@acme.example","industry":"retail"}'
```

```json
{"name":"Acme Traders","email":"ops@acme.example","industry":"retail","status":"lead","id":1,"created_at":"2026-11-04T09:30:00.123456Z"}
```

Missing key gives `401` with `{"error":{"code":"unauthorized",...}}`. Interactive docs are at `http://localhost:8000/docs`.

## 8.11 Verification checklist

| # | Check | Done |
|---|---|---|
| 1 | All tests pass, including failure cases | ☐ |
| 2 | Every error response uses the standard shape and includes `request_id` | ☐ |
| 3 | Request without `X-API-Key` is rejected on every `/v1` route | ☐ |
| 4 | `page_size=1000` is rejected (resource-consumption control) | ☐ |
| 5 | Duplicate email returns 409 | ☐ |
| 6 | Logs show method, path, status and request ID, and **no key** | ☐ |
| 7 | `git log` shows small commits; `git status` shows no `.env` | ☐ |
| 8 | A teammate can run it from your README alone | ☐ |

## 8.12 Common errors

| Symptom | Cause | Fix |
|---|---|---|
| `ModuleNotFoundError: app` | Running pytest from the wrong folder | Run from `assignment-01/` |
| `500 server_misconfigured` | `CLIENT_API_KEY` not set | Export it or load `.env` |
| `422` on a valid-looking body | Missing `Content-Type: application/json` or wrong field names | Check the details array |
| Tests leak data between cases | Store not reset | The fixture reinitialises the store |
| `401` in Postman | Header name typo | Use exactly `X-API-Key` |

## 8.13 Security requirements

- Key from the environment only; compared in constant time; never logged.
- Validation rejects unknown shapes before any logic runs.
- Page size capped (API4). Errors never expose internals (API8).
- No real client data in tests or in the repository.
- Known limitation to report in your README: one shared key means **no object-level authorisation yet**. That is solved in Project 3.

## 8.14 Documentation to submit

`README.md` (purpose, setup, run, test, example calls) · exported `openapi.json` / `openapi.yaml` · Postman collection (no secrets; use variables) · a short **error catalogue** listing each `code`.

## 8.15 Practical assignment (what you must deliver)

1. Re-create the API **yourself** from the specification (do not copy-paste; reading the reference is allowed once you are stuck for 30 minutes).
2. **Extend it** with: (a) `contacts` as a sub-resource of a client (`/v1/clients/{id}/contacts`); (b) a `search` query parameter on the list endpoint; (c) tests for each.
3. Add a `/v1/clients/export` endpoint design note in the README: how would you stream 1 million rows safely?
4. Open a pull request using the template and respond to review comments.

## 8.16 Assessment

Marked with the rubric in section 6.4. **Must-have for Gate 1:** all tests green · standard errors · authentication · pagination cap · no secrets in Git · README. **Stretch:** OpenAPI examples, structured JSON logging, request-size limit.

## 8.17 Module release plan (what comes next)

| Module | Content | Released when |
|---|---|---|
| 2 | Levels 1 to 3 deep lessons with full lab code | Start of week 1 |
| 3 | Level 4 REST design workshop and Postman | Week 3 |
| 4 | **Project 2:** PostgreSQL, migrations, pooling | After Gate 1 |
| 5 | **Project 3:** OAuth 2.0, JWT, RBAC | Week 7 |
| 6 | **Project 4:** two-app integration | Week 8 |
| 7 | Projects 5 to 7: webhooks, sync, retry, idempotency | Week 10 |
| 8 | Projects 8 and 9: monitoring and Docker | Week 12 |
| 9 | Capstone brief and panel guide | Week 13 |
