# VEL Practice API (training target)

In-memory REST service with **deliberate defects**. Standard library only; binds to `127.0.0.1` by default. Run: `python3 practice-api/server.py --port 8080`.

Synthetic training credentials for the **local** server: `qa@example.test` / `Passw0rd!QA`. Override with env vars `PRACTICE_API_EMAIL` and
`PRACTICE_API_PASSWORD` (do this in CI via the secret store). Set `PRACTICE_API_LOG=1` to log requests.

| Method and path | Auth | Success | Specified errors |
|---|---|---|---|
| `GET /api/health` | No | 200 `{"status":"ok","service":"vel-practice-api"}` | — |
| `POST /api/auth/login` | No | 200 `{token, expiresIn}` | 400 missing field; 401 invalid credentials |
| `GET /api/products?page=&limit=` | No | 200 `{items,page,limit,total}`; `limit` 1–50, default 5 | 400 invalid page/limit |
| `GET /api/products/{id}` | No | 200 `{id,name,price,stock}` | 400 id not an integer; 404 unknown id |
| `POST /api/products` | Bearer | 201 + `Location` | 400 invalid body; 401; 409 duplicate name; 415 wrong content type |
| `PUT /api/products/{id}` | Bearer | 200 updated product | 400; 401; 404 |
| `DELETE /api/products/{id}` | Bearer | 204 | 401; 404 unknown or already deleted |
| `GET /api/orders` | Bearer | 200 `{items}` | 401 without valid token |
| `GET /api/slow?delay=ms` | No | 200 after delay (0–5000 ms) | 400 invalid delay |

Product rules: `name` required string 1–60 characters; `price` number **> 0**; `stock` integer **≥ 0**. Unsupported methods → 405 with `Allow`.
