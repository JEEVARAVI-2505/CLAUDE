"""Minimal Ollama HTTP client (stdlib only).

Safety properties:
* talks only to the configured host, which must be loopback unless the admin
  explicitly enabled ``allow_remote_host``;
* environment proxy variables are ignored so documents can never be routed
  through a proxy by accident;
* asks Ollama for JSON constrained by a schema, never executes any output.
"""

from __future__ import annotations

import ipaddress
import json
import logging
import socket
import time
import urllib.error
import urllib.request
from dataclasses import dataclass, field
from typing import Any, Callable
from urllib.parse import urlparse

from ..config import AiSettings
from ..domain.exceptions import (AiModelMissingError, AiRemoteHostBlockedError, AiTimeoutError, AiUnavailableError,
                                 AiError, OperationCancelled)

log = logging.getLogger("documind.ai")
MIN_OLLAMA = (0, 5, 0)  # structured outputs (JSON-schema `format`) need Ollama >= 0.5.0


@dataclass
class AiStatus:
    reachable: bool
    version: str | None = None
    model_installed: bool = False
    models: list[str] = field(default_factory=list)
    message: str = ""

    @property
    def ready(self) -> bool:
        return self.reachable and self.model_installed


def is_loopback_host(host: str) -> bool:
    h = host.strip("[]").lower()
    if h == "localhost":
        return True
    try:
        return ipaddress.ip_address(h).is_loopback
    except ValueError:
        return False


class OllamaClient:
    def __init__(self, settings: AiSettings) -> None:
        self.s = settings
        u = urlparse(settings.host)
        if u.scheme not in ("http", "https") or not u.hostname:
            raise AiUnavailableError("The AI server address is not valid. Use something like http://127.0.0.1:11434")
        if not is_loopback_host(u.hostname) and not settings.allow_remote_host:
            raise AiRemoteHostBlockedError(
                "For privacy, DocuMind only talks to an AI server on this computer (127.0.0.1). "
                "A remote address must be enabled explicitly by an administrator in Settings > AI.")
        self.base = f"{u.scheme}://{u.netloc}".rstrip("/")
        self._opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))  # never use env proxies

    # ---- low level ----------------------------------------------------------------
    def _open(self, path: str, body: dict[str, Any] | None, timeout: float):
        data = json.dumps(body).encode() if body is not None else None
        req = urllib.request.Request(self.base + path, data=data, method="POST" if body is not None else "GET",
                                     headers={"Content-Type": "application/json", "User-Agent": "DocuMind/0.9"})
        try:
            return self._opener.open(req, timeout=timeout)
        except urllib.error.HTTPError as e:
            msg = e.read(2000).decode("utf-8", "replace")
            if e.code == 404 and "model" in msg.lower():
                raise AiModelMissingError(f"The AI model '{self.s.model}' is not installed. Run: ollama pull {self.s.model}", detail=msg[:200]) from e
            raise AiError(f"The AI server returned an error ({e.code}).", detail=msg[:300]) from e
        except (socket.timeout, TimeoutError) as e:
            raise AiTimeoutError(f"The AI model did not answer within {int(timeout)} seconds.") from e
        except urllib.error.URLError as e:
            if isinstance(e.reason, (socket.timeout, TimeoutError)):
                raise AiTimeoutError(f"The AI model did not answer within {int(timeout)} seconds.") from e
            raise AiUnavailableError("The local AI server (Ollama) is not running. Start Ollama, or continue in rules-only mode.", detail=str(e.reason)) from e
        except OSError as e:
            raise AiUnavailableError("The local AI server (Ollama) could not be reached.", detail=str(e)) from e

    # ---- health ----------------------------------------------------------------------
    def check(self) -> AiStatus:
        try:
            with self._open("/api/version", None, 4) as r:
                version = json.loads(r.read(2000)).get("version")
            with self._open("/api/tags", None, 6) as r:
                tags = json.loads(r.read(2_000_000))
        except AiError as e:
            return AiStatus(False, message=e.user_message)
        except ValueError:
            return AiStatus(False, message="The server at that address is not Ollama (unexpected reply).")
        models = sorted(m.get("name", "") for m in tags.get("models", []))
        want = self.s.model
        installed = want in models or (":" not in want and any(m.split(":")[0] == want for m in models))
        notes = []
        try:
            parts = tuple(int(x) for x in str(version).split("-")[0].split(".")[:3])
            if parts < MIN_OLLAMA:
                notes.append(f"Ollama {version} is older than {'.'.join(map(str, MIN_OLLAMA))}; update it for schema-constrained output.")
        except ValueError:
            pass
        msg = "Ready." if installed else f"Ollama is running but model '{want}' is not installed. Run: ollama pull {want}"
        return AiStatus(True, version, installed, models, " ".join([msg, *notes]))

    # ---- inference ---------------------------------------------------------------------
    def chat_json(self, system: str, user: str, schema: dict[str, Any], cancel: Callable[[], bool] | None = None) -> str:
        """Return the model's raw JSON text (streamed, cancellable between chunks)."""
        body = {"model": self.s.model, "stream": True, "format": schema,
                "options": {"temperature": 0, "seed": 0, "num_ctx": 8192},
                "messages": [{"role": "system", "content": system}, {"role": "user", "content": user}]}
        deadline = time.monotonic() + self.s.timeout_s
        parts: list[str] = []
        try:
            resp = self._open("/api/chat", body, self.s.timeout_s)
        except AiError as e:
            if type(e) is not AiError or "(400)" not in e.user_message:
                raise
            body["format"] = "json"  # older servers: plain JSON mode
            resp = self._open("/api/chat", body, self.s.timeout_s)
        with resp:
            while True:
                if cancel and cancel():
                    raise OperationCancelled()
                if time.monotonic() > deadline:
                    raise AiTimeoutError(f"The AI model did not finish within {self.s.timeout_s} seconds.")
                try:
                    line = resp.readline()
                except (socket.timeout, TimeoutError) as e:
                    raise AiTimeoutError(f"The AI model stopped responding ({self.s.timeout_s}s).") from e
                if not line:
                    break
                try:
                    obj = json.loads(line)
                except ValueError:
                    continue
                if obj.get("error"):
                    err = str(obj["error"])
                    if "not found" in err.lower():
                        raise AiModelMissingError(f"The AI model '{self.s.model}' is not installed. Run: ollama pull {self.s.model}")
                    raise AiError("The AI model reported an error.", detail=err[:300])
                parts.append(obj.get("message", {}).get("content", ""))
                if sum(map(len, parts)) > 400_000:
                    raise AiError("The AI response was unreasonably long and was discarded.")
                if obj.get("done"):
                    break
        return "".join(parts)
