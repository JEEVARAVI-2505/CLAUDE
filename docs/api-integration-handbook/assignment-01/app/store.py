"""In-memory store. Assignment 02 replaces this with PostgreSQL."""
from datetime import datetime, timezone

from .schemas import Client, ClientCreate, ClientUpdate


class ClientStore:
    def __init__(self) -> None:
        self._rows: dict[int, Client] = {}
        self._next_id = 1

    def create(self, data: ClientCreate) -> Client:
        row = Client(id=self._next_id, created_at=datetime.now(timezone.utc), **data.model_dump())
        self._rows[row.id] = row
        self._next_id += 1
        return row

    def get(self, client_id: int) -> Client | None:
        return self._rows.get(client_id)

    def email_exists(self, email: str, exclude_id: int | None = None) -> bool:
        return any(r.email.lower() == email.lower() and r.id != exclude_id for r in self._rows.values())

    def update(self, client_id: int, data: ClientUpdate) -> Client | None:
        row = self._rows.get(client_id)
        if row is None:
            return None
        updated = row.model_copy(update=data.model_dump(exclude_unset=True))
        self._rows[client_id] = updated
        return updated

    def delete(self, client_id: int) -> bool:
        return self._rows.pop(client_id, None) is not None

    def all(self) -> list[Client]:
        return list(self._rows.values())
