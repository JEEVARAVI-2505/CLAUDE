"""Client Registry API v1 - VEL Assignment 01."""
import logging
import uuid
from typing import Literal

from fastapi import Depends, FastAPI, Query, Request, Response

from .errors import ApiError, install_error_handlers
from .schemas import Client, ClientCreate, ClientUpdate, Page, Status
from .security import require_api_key
from .store import ClientStore

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
log = logging.getLogger("client-registry")

app = FastAPI(title="Client Registry API", version="1.0.0")
install_error_handlers(app)
store = ClientStore()


@app.middleware("http")
async def request_id_and_logging(request: Request, call_next):
    request.state.request_id = request.headers.get("x-request-id") or str(uuid.uuid4())
    response = await call_next(request)
    response.headers["X-Request-ID"] = request.state.request_id
    log.info("%s %s -> %s [%s]", request.method, request.url.path, response.status_code, request.state.request_id)
    return response


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


v1 = Depends(require_api_key)


@app.post("/v1/clients", response_model=Client, status_code=201, dependencies=[v1])
def create_client(body: ClientCreate, response: Response) -> Client:
    if store.email_exists(body.email):
        raise ApiError(409, "duplicate_email", "A client with this email already exists")
    row = store.create(body)
    response.headers["Location"] = f"/v1/clients/{row.id}"
    return row


@app.get("/v1/clients", response_model=Page, dependencies=[v1])
def list_clients(
    page: int = Query(1, ge=1),
    page_size: int = Query(20, ge=1, le=100),
    status: Status | None = None,
    industry: str | None = None,
    sort: Literal["name", "-name", "created_at", "-created_at"] = "name",
) -> Page:
    rows = store.all()
    if status:
        rows = [r for r in rows if r.status == status]
    if industry:
        rows = [r for r in rows if r.industry.lower() == industry.lower()]
    field = sort.lstrip("-")
    rows.sort(key=lambda r: getattr(r, field), reverse=sort.startswith("-"))
    start = (page - 1) * page_size
    return Page(items=rows[start:start + page_size], page=page, page_size=page_size, total=len(rows))


@app.get("/v1/clients/{client_id}", response_model=Client, dependencies=[v1])
def get_client(client_id: int) -> Client:
    row = store.get(client_id)
    if row is None:
        raise ApiError(404, "not_found", f"Client {client_id} not found")
    return row


@app.patch("/v1/clients/{client_id}", response_model=Client, dependencies=[v1])
def update_client(client_id: int, body: ClientUpdate) -> Client:
    if body.email and store.email_exists(body.email, exclude_id=client_id):
        raise ApiError(409, "duplicate_email", "A client with this email already exists")
    row = store.update(client_id, body)
    if row is None:
        raise ApiError(404, "not_found", f"Client {client_id} not found")
    return row


@app.delete("/v1/clients/{client_id}", status_code=204, dependencies=[v1])
def delete_client(client_id: int) -> Response:
    if not store.delete(client_id):
        raise ApiError(404, "not_found", f"Client {client_id} not found")
    return Response(status_code=204)
