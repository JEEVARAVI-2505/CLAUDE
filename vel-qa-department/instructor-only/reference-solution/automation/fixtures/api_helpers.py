"""Helpers shared by API tests: JSON-schema validation of products."""
import json
import pathlib

from jsonschema import Draft202012Validator

SCHEMA = json.loads((pathlib.Path(__file__).resolve().parents[1] / "api" / "schemas" / "product.schema.json").read_text())


def schema_errors(instance: dict) -> list[str]:
    """Return a list of human-readable schema violations (empty list = valid)."""
    return [e.message for e in Draft202012Validator(SCHEMA).iter_errors(instance)]
