"""Retry with exponential back-off + jitter, and an idempotent consumer."""
import random
import time

RETRYABLE = {429, 500, 502, 503, 504}


def call_with_retry(send, max_attempts=5, base=0.5, cap=30.0, sleep=time.sleep):
    """`send()` returns (status_code, body, retry_after_seconds_or_None)."""
    for attempt in range(1, max_attempts + 1):
        status, body, retry_after = send()
        if status < 400:
            return body
        if status not in RETRYABLE:                      # 4xx: do NOT retry blindly
            raise ValueError(f"non-retryable status {status}")
        if attempt == max_attempts:
            raise TimeoutError(f"gave up after {attempt} attempts (last {status})")
        delay = retry_after if retry_after else min(cap, base * 2 ** (attempt - 1))
        sleep(delay + random.uniform(0, delay * 0.25))   # jitter avoids thundering herd


class IdempotentConsumer:
    """Process each event id once, even if the broker delivers it many times."""

    def __init__(self):
        self.seen: set[str] = set()      # production: a unique-key table in PostgreSQL
        self.created: list[dict] = []

    def handle(self, event: dict) -> str:
        key = event["event_id"]
        if key in self.seen:
            return "duplicate-ignored"
        self.created.append({"invoice_for": event["order_id"]})
        self.seen.add(key)               # production: insert key + effect in ONE transaction
        return "processed"


if __name__ == "__main__":
    calls = iter([(503, None, None), (429, None, 1), (200, {"ok": True}, None)])
    print(call_with_retry(lambda: next(calls), sleep=lambda s: print(f"  wait {s:.2f}s")))
    c = IdempotentConsumer()
    evt = {"event_id": "evt_1", "order_id": "A-100"}
    print([c.handle(evt) for _ in range(3)], len(c.created))
