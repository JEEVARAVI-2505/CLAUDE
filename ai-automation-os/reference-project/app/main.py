"""Minimal FastAPI service: POST /bills/validate runs the business rules on extracted bill data.

Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com
Run:  uvicorn app.main:app --reload     Docs: http://127.0.0.1:8000/docs
"""
from datetime import date
from decimal import Decimal

from fastapi import FastAPI
from pydantic import BaseModel, Field

from app.rules import Bill, validate_bill

app = FastAPI(title="Bill Validation Service", version="0.1.0")


class BillIn(BaseModel):
    vendor_gstin: str
    invoice_no: str = Field(min_length=1)
    invoice_date: date
    taxable_value: Decimal = Field(ge=0)
    cgst: Decimal = Field(default=Decimal("0"), ge=0)
    sgst: Decimal = Field(default=Decimal("0"), ge=0)
    igst: Decimal = Field(default=Decimal("0"), ge=0)
    total: Decimal = Field(ge=0)


class ValidationOut(BaseModel):
    passed: bool
    errors: list[str]
    route: str  # "auto_post" or "human_review"


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.post("/bills/validate", response_model=ValidationOut)
def validate(bill: BillIn) -> ValidationOut:
    errors = validate_bill(Bill(**bill.model_dump()))
    return ValidationOut(passed=not errors, errors=errors, route="auto_post" if not errors else "human_review")
