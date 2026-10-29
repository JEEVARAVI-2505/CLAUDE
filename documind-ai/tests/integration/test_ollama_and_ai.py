"""AI path against a protocol-compatible local stub server (no real model is available in this environment)."""
import json
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import pytest

from documind.config import AiSettings
from documind.domain.exceptions import (AiMalformedOutputError, AiModelMissingError, AiRemoteHostBlockedError, AiTimeoutError,
                                        AiUnavailableError, OperationCancelled)
from documind.domain.models import DocStatus, PageText
from documind.domain.rules_extractor import extract_with_rules
from documind.domain.schemas import SYSTEM_PROMPT, build_llm_model
from documind.application.llm_extraction import extract_json_object, merge, parse_payload
from documind.application.extraction_service import validation_options
from documind.infrastructure.ollama_client import OllamaClient
from tests.helpers import make_ctx, pack, process_all, sample_pdf


class Stub(BaseHTTPRequestHandler):
    cfg: dict = {}
    seen: list = []

    def log_message(self, *a): pass

    def _send(self, code, obj):
        body = json.dumps(obj).encode()
        self.send_response(code); self.send_header("Content-Type", "application/json"); self.send_header("Content-Length", str(len(body))); self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path == "/api/version":
            self._send(200, {"version": self.cfg.get("version", "0.6.0")})
        elif self.path == "/api/tags":
            self._send(200, {"models": [{"name": m} for m in self.cfg.get("models", ["m:1"])]})
        else:
            self._send(404, {})

    def do_POST(self):
        n = int(self.headers.get("Content-Length", 0)); body = json.loads(self.rfile.read(n))
        Stub.seen.append(body)
        if self.cfg.get("chat_status"):
            return self._send(self.cfg["chat_status"], {"error": self.cfg.get("chat_error", "model 'm:1' not found")})
        time.sleep(self.cfg.get("delay", 0))
        out = self.cfg["answer"](body) if callable(self.cfg.get("answer")) else self.cfg.get("answer", "{}")
        self.send_response(200); self.send_header("Content-Type", "application/x-ndjson"); self.end_headers()
        for i in range(0, len(out), 40):
            self.wfile.write((json.dumps({"message": {"content": out[i:i + 40]}, "done": False}) + "\n").encode()); self.wfile.flush()
        self.wfile.write((json.dumps({"message": {"content": ""}, "done": True}) + "\n").encode())


@pytest.fixture()
def server():
    Stub.cfg, Stub.seen = {}, []
    srv = ThreadingHTTPServer(("127.0.0.1", 0), Stub)
    threading.Thread(target=srv.serve_forever, daemon=True).start()
    yield srv, f"http://127.0.0.1:{srv.server_port}"
    srv.shutdown()


def client(url, **kw):
    t = kw.pop("timeout_s", 5)
    cfg = AiSettings(enabled=True, host=url, model="m:1", **kw)
    cfg.timeout_s = t          # assignment skips the UI-level minimum so tests can use short timeouts
    return OllamaClient(cfg)


def test_status_ready_missing_model_and_old_version(server):
    srv, url = server
    assert client(url).check().ready
    Stub.cfg["models"] = ["other:7b"]
    st = client(url).check()
    assert st.reachable and not st.model_installed and "ollama pull m:1" in st.message
    Stub.cfg.update(models=["m:1"], version="0.3.0")
    assert "older" in client(url).check().message


def test_ollama_not_running():
    st = client("http://127.0.0.1:9").check()      # nothing listens on port 9
    assert not st.reachable and "not running" in st.message
    with pytest.raises(AiUnavailableError):
        client("http://127.0.0.1:9").chat_json("s", "u", {})


def test_remote_host_blocked_unless_explicitly_allowed():
    with pytest.raises(AiRemoteHostBlockedError):
        OllamaClient(AiSettings(host="http://203.0.113.5:11434"))
    with pytest.raises(AiRemoteHostBlockedError):
        OllamaClient(AiSettings(host="http://example.com"))
    OllamaClient(AiSettings(host="http://[::1]:11434")); OllamaClient(AiSettings(host="http://localhost:11434"))
    OllamaClient(AiSettings(host="http://192.168.1.5:11434", allow_remote_host=True))


def test_env_proxy_is_never_used(server, monkeypatch):
    srv, url = server
    monkeypatch.setenv("HTTP_PROXY", "http://127.0.0.1:9"); monkeypatch.setenv("http_proxy", "http://127.0.0.1:9")
    assert client(url).check().reachable


def test_request_is_schema_constrained_deterministic_and_wraps_document(server):
    srv, url = server
    Stub.cfg["answer"] = "{}"
    client(url).chat_json(SYSTEM_PROMPT, "<document>x</document>", build_llm_model().model_json_schema())
    body = Stub.seen[-1]
    assert body["options"]["temperature"] == 0 and isinstance(body["format"], dict) and "properties" in body["format"]
    assert "untrusted" in body["messages"][0]["content"] and body["stream"] is True


def test_model_missing_on_chat(server):
    srv, url = server; Stub.cfg["chat_status"] = 404
    with pytest.raises(AiModelMissingError):
        client(url).chat_json("s", "u", {})


def test_timeout_and_cancel(server):
    srv, url = server; Stub.cfg.update(answer="{}", delay=2)
    with pytest.raises(AiTimeoutError):
        client(url, timeout_s=1).chat_json("s", "u", {})
    Stub.cfg["delay"] = 0; Stub.cfg["answer"] = "x" * 4000
    with pytest.raises(OperationCancelled):
        client(url).chat_json("s", "u", {}, cancel=lambda: True)


@pytest.mark.parametrize("raw", ["not json at all", '{"supplier_name": ', '{"unknown_field": 1}', '{"total": "5"}', '{"line_items": "x"}',
                                 '{"total": {"value": 5, "quote": "5", "extra": 1}}', "[]"])
def test_malformed_ai_output_rejected(raw):
    with pytest.raises(AiMalformedOutputError):
        parse_payload(raw, [])


def test_json_wrapped_in_prose_and_fences_is_extracted():
    txt = 'Sure! ```json\n{"total": {"value": "10", "quote": "Total 10"}, "line_items": []}\n``` hope that helps'
    assert parse_payload(txt, []).total.value == "10"
    assert extract_json_object('{"a": "}{", "b": {"c": 1}} trailing') == '{"a": "}{", "b": {"c": 1}}'


PAGES = [PageText(page=1, text="Zeta Traders\nInvoice No: Z-9\nInvoice Date: 2025-05-05\nTotal 100.00\nShip via courier")]


def _merge(payload_json):
    rules = extract_with_rules(PAGES, pack())
    p = parse_payload(json.dumps(payload_json), [])
    from documind.config import AppConfig
    return merge(p, rules, PAGES, "DMY", [], validation_options(AppConfig())), rules


def test_ungrounded_ai_values_are_discarded_and_reported():
    (kind, fields, items, notes), _ = _merge({"customer_name": {"value": "Evil Corp", "quote": "Evil Corp"},
                                              "total": {"value": "1.00", "quote": "Total 1.00"}})
    assert "customer_name" not in fields and fields["total"].value == "100"
    assert "ungrounded_ai" in fields["total"].flags and any("discarded" in n for n in notes)


def test_grounded_ai_fills_gap_and_conflicts_are_surfaced_not_resolved():
    (kind, fields, items, notes), _ = _merge({"payment_terms": {"value": "courier", "quote": "courier"},
                                              "invoice_number": {"value": "Z-9", "quote": "Z-9"},
                                              "total": {"value": "Z-9", "quote": "Z-9"}})
    assert fields["payment_terms"].source == "llm" and fields["invoice_number"].source == "rules+llm"
    f = fields["total"]
    assert f.value == "100" and "conflict_ai" not in f.flags      # "Z-9" is not a number: dropped, not treated as a conflict
    (k2, f2, _i, _n), _ = _merge({"invoice_date": {"value": "05/05/2025", "quote": "2025-05-05"}})
    assert f2["invoice_date"].value == "2025-05-05"


def test_ai_conflict_keeps_rules_value_and_flags():
    pages = [PageText(page=1, text="Invoice No: A-1\nInvoice Date: 2025-05-05\nGrand Total 100.00\nSub Total 90.00\nTotal 80.00")]
    rules = extract_with_rules(pages, pack())
    p = parse_payload(json.dumps({"total": {"value": "80.00", "quote": "Total 80.00"}}), [])
    from documind.config import AppConfig
    _k, f, _i, _n = merge(p, rules, pages, "DMY", [], validation_options(AppConfig()))
    assert f["total"].value == rules.fields["total"].value and "conflict_ai" in f["total"].flags and f["total"].alt == "80"


def test_pipeline_uses_ai_when_available_and_degrades_when_not(tmp_home, server):
    srv, url = server
    ctx = make_ctx(); ctx.cfg.ai.enabled, ctx.cfg.ai.host, ctx.cfg.ai.model, ctx.cfg.ai.timeout_s = True, url, "m:1", 3
    d, p = sample_pdf(tmp_home, 81, "labels_below")
    Stub.cfg["answer"] = json.dumps({"payment_terms": {"value": d.terms, "quote": d.terms},
                                     "supplier_name": {"value": d.supplier, "quote": d.supplier}})
    rec = process_all(ctx, [ctx.docs.import_one(p).doc_id])[0]
    assert rec.info.mode == "rules+ai" and rec.info.model == "m:1" and rec.status == DocStatus.NEEDS_REVIEW
    Stub.cfg["answer"] = "garbage not json"                                  # malformed twice -> rules-only result
    p2 = sample_pdf(tmp_home, 82, "labels_below", name="two.pdf")[1]
    rec2 = process_all(ctx, [ctx.docs.import_one(p2).doc_id])[0]
    assert rec2.info.mode == "rules" and rec2.status == DocStatus.NEEDS_REVIEW and any("AI step skipped" in w for w in rec2.info.warnings)
    srv.shutdown(); ctx.cfg.ai.host = "http://127.0.0.1:9"                      # server gone
    p3 = sample_pdf(tmp_home, 83, "labels_below", name="three.pdf")[1]
    rec3 = process_all(ctx, [ctx.docs.import_one(p3).doc_id])[0]
    assert rec3.status == DocStatus.NEEDS_REVIEW and any("not running" in w for w in rec3.info.warnings)
    ctx.shutdown()
