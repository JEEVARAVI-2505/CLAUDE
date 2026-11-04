"""Request and response models (the API contract)."""
from datetime import datetime
from typing import Literal

from pydantic import BaseModel, Field

Status = Literal["lead", "active", "inactive"]


class ClientCreate(BaseModel):
    name: str = Field(min_length=2, max_length=100)
    email: str = Field(pattern=r"^[^@\s]+@[^@\s]+\.[^@\s]+$")
    industry: str = Field(min_length=2, max_length=60)
    status: Status = "lead"


class ClientUpdate(BaseModel):
    """PATCH body: every field optional."""
    name: str | None = Field(default=None, min_length=2, max_length=100)
    email: str | None = Field(default=None, pattern=r"^[^@\s]+@[^@\s]+\.[^@\s]+$")
    industry: str | None = Field(default=None, min_length=2, max_length=60)
    status: Status | None = None


class Client(ClientCreate):
    id: int
    created_at: datetime


class Page(BaseModel):
    items: list[Client]
    page: int
    page_size: int
    total: int
