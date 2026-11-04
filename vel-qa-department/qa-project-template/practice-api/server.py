#!/usr/bin/env python3
"""VEL Practice API - a tiny in-memory REST service used as a QA TRAINING TARGET.

* Standard library only (Python 3.9+).  Binds to 127.0.0.1 by default - never expose it to a network.
* It contains DELIBERATE defects for trainees to find (see instructor-only/ANSWER-KEY.md). Do not deploy.
* Credentials are SYNTHETIC training values for this local server; override with PRACTICE_API_EMAIL / PRACTICE_API_PASSWORD.

Run:  python3 server.py --port 8080
"""
from __future__ import annotations

import argparse
import json
import os
import re
import secrets
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

EMAIL = os.getenv("PRACTICE_API_EMAIL", "qa@example.test")
PASSWORD = os.getenv("PRACTICE_API_PASSWORD", "Passw0rd!QA")   # synthetic training credential, local use only
TOKEN_TTL = 3600

PRODUCTS = {
    1: {"id": 1, "name": "Notebook", "price": 10.10, "stock": 100},
    2: {"id": 2, "name": "Pen", "price": 5.50, "stock": 250},
    3: {"id": 3, "name": "Backpack", "price": 49.99, "stock": 20},
    4: {"id": 4, "name": "Desk Lamp", "price": 24.00, "stock": 35},
    5: {"id": 5, "name": "Water Bottle", "price": 12.75, "stock": 80},
    6: {"id": 6, "name": "Headphones", "price": 79.90, "stock": 15},
    7: {"id": 7, "name": "Keyboard", "price": 35.00, "stock": 40},
}
ORDERS = [{"id": 1, "productId": 1, "qty": 2, "total": 20.2}, {"id": 2, "productId": 4, "qty": 1, "total": 24.0}]
DELETED: set[int] = set()
TOKENS: dict[str, float] = {}
NEXT_ID = [8]


def public(p: dict) -> dict:
    out = dict(p)
    if out["id"] == 3:
        out["price"] = str(out["price"])          # DEFECT A6: price returned as a string for product 3 (schema violation)
    return out


class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"
    server_version = "VELPracticeAPI/1.0"

    # ---------------------------------------------------------------- helpers
    def log_message(self, fmt, *args):            # quiet by default; set PRACTICE_API_LOG=1 to see requests
        if os.getenv("PRACTICE_API_LOG"):
            super().log_message(fmt, *args)

    def send(self, status: int, body=None, headers=None, raw: bytes | None = None, ctype="application/json"):
        data = raw if raw is not None else (b"" if body is None else json.dumps(body, separators=(",", ":")).encode())
        self.send_response(status)
        if status != 204:
            self.send_header("Content-Type", ctype)
        if status != 204:
            self.send_header("Content-Length", str(len(data)))
        for k, v in (headers or {}).items():
            self.send_header(k, v)
        self.end_headers()
        if status != 204:
            self.wfile.write(data)

    def error(self, status: int, msg: str, **extra):
        self.send(status, {"error": msg, **extra})

    def body(self):
        n = int(self.headers.get("Content-Length") or 0)
        if n == 0:
            return {}
        if "application/json" not in (self.headers.get("Content-Type") or ""):
            raise ValueError("415")
        try:
            return json.loads(self.rfile.read(n) or b"{}")
        except json.JSONDecodeError:
            raise ValueError("400")

    def authed(self) -> bool:
        h = self.headers.get("Authorization") or ""
        tok = h[7:] if h.startswith("Bearer ") else ""
        return TOKENS.get(tok, 0) > time.time()

    def validate_product(self, d: dict):
        name, price, stock = d.get("name"), d.get("price"), d.get("stock", 0)
        if not isinstance(name, str) or not 1 <= len(name) <= 60:
            return "name is required (1-60 characters)"
        if isinstance(price, bool) or not isinstance(price, (int, float)) or price < 0:   # DEFECT A1: price 0 accepted (spec: price > 0)
            return "price must be a number greater than 0"
        if isinstance(stock, bool) or not isinstance(stock, int) or stock < 0:
            return "stock must be an integer >= 0"
        return None

    # ---------------------------------------------------------------- routing
    def route(self, method: str):
        u = urlparse(self.path)
        path, qs = u.path.rstrip("/") or "/", parse_qs(u.query)
        routes = [
            (r"^/api/health$", {"GET": self.health}),
            (r"^/api/auth/login$", {"POST": self.login}),
            (r"^/api/products$", {"GET": lambda m: self.list_products(qs), "POST": self.create_product}),
            (r"^/api/products/([^/]+)$", {"GET": self.get_product, "PUT": self.update_product, "DELETE": self.delete_product}),
            (r"^/api/orders$", {"GET": self.orders}),
            (r"^/api/slow$", {"GET": lambda m: self.slow(qs)}),
        ]
        for pattern, methods in routes:
            m = re.match(pattern, path)
            if m:
                fn = methods.get(method)
                if fn is None:
                    return self.send(405, {"error": "Method not allowed"}, {"Allow": ", ".join(methods)})
                try:
                    return fn(m)
                except ValueError as e:
                    return self.error(415, "Content-Type must be application/json") if str(e) == "415" else self.error(400, "Invalid JSON")
        self.error(404, "Not found")

    def do_GET(self): self.route("GET")
    def do_POST(self): self.route("POST")
    def do_PUT(self): self.route("PUT")
    def do_DELETE(self): self.route("DELETE")

    # ---------------------------------------------------------------- handlers
    def health(self, m):
        self.send(200, {"status": "ok", "service": "vel-practice-api"})

    def login(self, m):
        d = self.body()
        email, pw = d.get("email"), d.get("password")
        if not isinstance(email, str) or not email or not isinstance(pw, str):
            return self.error(400, "email and password are required")
        if email != EMAIL or (pw != PASSWORD and pw != ""):       # DEFECT A7: empty password accepted for a valid e-mail
            return self.error(401, "Invalid credentials")
        tok = secrets.token_hex(16)
        TOKENS[tok] = time.time() + TOKEN_TTL
        self.send(200, {"token": tok, "expiresIn": TOKEN_TTL})

    def list_products(self, qs):
        try:
            page, limit = int(qs.get("page", ["1"])[0]), int(qs.get("limit", ["5"])[0])
        except ValueError:
            return self.error(400, "page and limit must be integers")
        if page < 1 or limit < 1:                                   # DEFECT A8: no upper limit enforced (spec: 1-50)
            return self.error(400, "page must be >= 1 and limit must be 1-50")
        items = [public(p) for _, p in sorted(PRODUCTS.items())]
        start = (page - 1) * limit
        self.send(200, {"items": items[start:start + limit], "page": page, "limit": limit, "total": len(items)})

    def get_product(self, m):
        raw = m.group(1)
        if not re.fullmatch(r"-?\d+", raw):                         # DEFECT A3: should be 400; leaks internals with a 500
            return self.send(500, {"error": "Internal Server Error",
                                   "detail": f"ValueError: invalid literal for int() with base 10: '{raw}' (server.py, line 142, in get_product)"})
        pid = int(raw)
        if pid not in PRODUCTS:                                     # DEFECT A10: wrong Content-Type (HTML instead of JSON) on 404
            return self.send(404, raw=b"<html><body><h1>Product not found</h1></body></html>", ctype="text/html")
        self.send(200, public(PRODUCTS[pid]))

    def create_product(self, m):
        if not self.authed():
            return self.error(401, "Authentication required")
        d = self.body()
        msg = self.validate_product(d)
        if msg:
            return self.error(400, msg)
        # DEFECT A2: duplicate names are accepted (spec: 409 Conflict)
        pid = NEXT_ID[0]; NEXT_ID[0] += 1
        PRODUCTS[pid] = {"id": pid, "name": d["name"], "price": d["price"], "stock": d.get("stock", 0)}
        self.send(201, public(PRODUCTS[pid]), {"Location": f"/api/products/{pid}"})

    def update_product(self, m):
        if not self.authed():
            return self.error(401, "Authentication required")
        raw = m.group(1)
        pid = int(raw) if re.fullmatch(r"\d+", raw) else -1
        if pid not in PRODUCTS:
            return self.error(404, "Product not found")
        d = self.body()
        msg = self.validate_product(d)
        if msg:
            return self.error(400, msg)
        PRODUCTS[pid].update(name=d["name"], price=d["price"])        # DEFECT A9: stock is silently ignored on update
        self.send(200, public(PRODUCTS[pid]))

    def delete_product(self, m):
        if not self.authed():
            return self.error(401, "Authentication required")
        raw = m.group(1)
        pid = int(raw) if re.fullmatch(r"\d+", raw) else -1
        if pid in DELETED:                                           # DEFECT A4: already-deleted product returns 204 (spec: 404)
            return self.send(204)
        if pid not in PRODUCTS:
            return self.error(404, "Product not found")
        del PRODUCTS[pid]; DELETED.add(pid)
        self.send(204)

    def orders(self, m):                                             # DEFECT A5: no authentication required (spec: 401) - SECURITY escalation
        self.send(200, {"items": ORDERS})

    def slow(self, qs):
        try:
            delay = int(qs.get("delay", ["0"])[0])
        except ValueError:
            return self.error(400, "delay must be an integer")
        if not 0 <= delay <= 5000:
            return self.error(400, "delay must be 0-5000 ms")
        time.sleep(delay / 1000)
        self.send(200, {"delayMs": delay})


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--host", default="127.0.0.1", help="bind address (default 127.0.0.1 - keep it local)")
    ap.add_argument("--port", type=int, default=8080)
    a = ap.parse_args()
    srv = ThreadingHTTPServer((a.host, a.port), Handler)
    print(f"VEL Practice API listening on http://{a.host}:{a.port}  (Ctrl+C to stop)", flush=True)
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
