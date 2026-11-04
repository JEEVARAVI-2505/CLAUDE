import pytest
from retry_idempotent import IdempotentConsumer, call_with_retry


def test_retries_then_succeeds():
    calls = iter([(503, None, None), (429, None, 1), (200, {"ok": 1}, None)])
    waits = []
    assert call_with_retry(lambda: next(calls), sleep=waits.append) == {"ok": 1}
    assert len(waits) == 2 and waits[1] >= 1


def test_4xx_not_retried():
    n = []
    def send():
        n.append(1); return 400, None, None
    with pytest.raises(ValueError):
        call_with_retry(send, sleep=lambda s: None)
    assert len(n) == 1


def test_gives_up():
    with pytest.raises(TimeoutError):
        call_with_retry(lambda: (500, None, None), max_attempts=3, sleep=lambda s: None)


def test_duplicate_event_creates_one_record():
    c = IdempotentConsumer()
    evt = {"event_id": "e1", "order_id": "A"}
    assert [c.handle(evt) for _ in range(3)] == ["processed", "duplicate-ignored", "duplicate-ignored"]
    assert len(c.created) == 1
