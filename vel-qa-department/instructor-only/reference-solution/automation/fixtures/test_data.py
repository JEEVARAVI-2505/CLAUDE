"""Synthetic test-data helpers. Never put real personal data in tests."""
import uuid

VALID_PASSWORD = "Passw0rdOK"          # synthetic; satisfies REQ-004


def unique_email(prefix: str = "qa") -> str:
    return f"{prefix}+{uuid.uuid4().hex[:8]}@example.test"


def valid_user(**overrides) -> dict:
    user = {"name": "Asha Rao", "email": unique_email(), "age": 35, "password": VALID_PASSWORD}
    user.update(overrides)
    return user
