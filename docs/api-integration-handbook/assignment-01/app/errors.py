"""One error shape for the whole API (company standard)."""
from fastapi import FastAPI, HTTPException, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse


class ApiError(Exception):
    def __init__(self, status: int, code: str, message: str) -> None:
        self.status, self.code, self.message = status, code, message


def _body(request: Request, code: str, message: str, details: object = None) -> dict:
    return {"error": {"code": code, "message": message, "details": details,
                      "request_id": getattr(request.state, "request_id", None)}}


def install_error_handlers(app: FastAPI) -> None:
    @app.exception_handler(ApiError)
    async def _api(request: Request, exc: ApiError) -> JSONResponse:
        return JSONResponse(_body(request, exc.code, exc.message), status_code=exc.status)

    @app.exception_handler(RequestValidationError)
    async def _validation(request: Request, exc: RequestValidationError) -> JSONResponse:
        details = [{"field": ".".join(str(p) for p in e["loc"][1:]), "problem": e["msg"]} for e in exc.errors()]
        return JSONResponse(_body(request, "validation_error", "Request validation failed", details), status_code=422)

    @app.exception_handler(HTTPException)
    async def _http(request: Request, exc: HTTPException) -> JSONResponse:
        return JSONResponse(_body(request, "http_error", str(exc.detail)), status_code=exc.status_code)
