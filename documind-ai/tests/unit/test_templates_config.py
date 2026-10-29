import json
import os

import pytest

from documind.config import AppConfig, ConfigManager, Paths
from documind.domain.exceptions import ConfigError
from documind.domain.templates import TemplateDef, load_templates, pick_template
from documind.logging_setup import redact


def test_templates_load_and_bad_file_reported(tmp_path):
    (tmp_path / "ok.json").write_text(json.dumps({"name": "a", "match_keywords": ["x"]}))
    (tmp_path / "bad.json").write_text("{not json")
    (tmp_path / "evil.json").write_text(json.dumps({"name": "e", "unknown_key": 1}))
    ts, problems = load_templates(tmp_path)
    assert [t.name for t in ts] == ["a"] and len(problems) == 2


def test_pick_template_best_match():
    a = TemplateDef(name="a", match_keywords=["alpha"]); b = TemplateDef(name="b", match_keywords=["alpha", "beta"])
    assert pick_template([a, b], "Alpha and Beta Ltd").name == "b"
    assert pick_template([a, b], "nothing") is None


def test_config_roundtrip_and_validation(tmp_home):
    m = ConfigManager()
    cfg = m.load()
    cfg.ai.model = "x:1b"; cfg.limits.max_file_mb = 7
    m.save(cfg)
    assert ConfigManager().load().limits.max_file_mb == 7
    with pytest.raises(Exception):
        AppConfig.model_validate({"ocr": {"languages": "eng; rm -rf /"}})
    with pytest.raises(Exception):
        AppConfig.model_validate({"limits": {"max_file_mb": 0}})


def test_corrupt_config_is_quarantined_not_fatal(tmp_home):
    m = ConfigManager(); m.dir.mkdir(parents=True); m.file.write_text("{{{ broken")
    cfg = ConfigManager().load()
    assert cfg == AppConfig() and list(m.dir.glob("config.json.corrupt-*"))


def test_relative_data_dir_rejected(tmp_home, monkeypatch):
    monkeypatch.delenv("DOCUMIND_DATA_DIR")
    with pytest.raises(ConfigError):
        ConfigManager().resolve_paths(AppConfig(data_dir="relative/path"))


def test_unwritable_data_dir_gives_friendly_error(tmp_path):
    ro = tmp_path / "ro"; ro.mkdir(); os.chmod(ro, 0o500)
    if os.access(ro, os.W_OK):
        pytest.skip("running as root: permissions not enforced")
    from documind.domain.exceptions import PermissionDeniedError
    with pytest.raises(PermissionDeniedError):
        Paths(ro / "sub").ensure()


def test_log_redaction():
    s = redact("user bob@example.com id 1234567890123 password=hunter2 path C:\\Users\\Alice\\Docs /home/alice/x")
    assert "bob@" not in s and "1234567890123" not in s and "hunter2" not in s and "Alice" not in s and "alice" not in s
