"""Rule packs and customer templates (JSON, never hard-coded).

A *rule pack* is the default vocabulary shipped with the app. A *template* is
a small JSON file (bundled or placed by the installer in the data directory)
that adds labels / defaults for a particular supplier or document layout.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, ValidationError

from .exceptions import ConfigError


class TemplateDef(BaseModel):
    model_config = ConfigDict(extra="forbid")
    name: str = Field(min_length=1, max_length=80)
    description: str = ""
    match_keywords: list[str] = Field(default_factory=list, max_length=20)
    document_kind: Literal["invoice", "purchase_order", "credit_note"] | None = None
    date_order: Literal["DMY", "MDY"] | None = None
    default_currency: str | None = Field(default=None, pattern=r"^[A-Z]{3}$")
    extra_labels: dict[str, list[str]] = Field(default_factory=dict)
    supplier_name: str | None = None  # fixed supplier for single-vendor layouts


@dataclass
class RulePack:
    labels: dict[str, list[str]]
    kind_markers: dict[str, list[str]]
    total_exclusions: list[str]
    tax_components: list[str]
    line_header_keywords: dict[str, list[str]]
    totals_row_markers: list[str]
    company_suffixes: list[str]
    title_words: list[str]
    units: list[str]
    templates: list[TemplateDef] = field(default_factory=list)

    def with_template(self, t: TemplateDef) -> "RulePack":
        labels = {k: list(v) for k, v in self.labels.items()}
        for key, extra in t.extra_labels.items():
            labels[key] = [e.lower() for e in extra] + labels.get(key, [])
        return RulePack(labels, self.kind_markers, self.total_exclusions, self.tax_components,
                        self.line_header_keywords, self.totals_row_markers, self.company_suffixes,
                        self.title_words, self.units, self.templates)


def load_pack(path: Path) -> RulePack:
    try:
        d = json.loads(path.read_text(encoding="utf-8"))
        return RulePack(
            labels={k: [x.lower() for x in v] for k, v in d["labels"].items()},
            kind_markers=d["kind_markers"], total_exclusions=d["total_exclusions"],
            tax_components=d["tax_components"], line_header_keywords=d["line_header_keywords"],
            totals_row_markers=d["totals_row_markers"], company_suffixes=d["company_suffixes"],
            title_words=d["title_words"], units=d["units"],
        )
    except (OSError, KeyError, ValueError) as e:
        raise ConfigError("The extraction rule pack is missing or damaged.", detail=str(e)) from e


def load_templates(*dirs: Path) -> tuple[list[TemplateDef], list[str]]:
    """Load every *.json template; invalid files are reported, not fatal."""
    out: list[TemplateDef] = []
    problems: list[str] = []
    for d in dirs:
        if not d.is_dir():
            continue
        for f in sorted(d.glob("*.json")):
            try:
                if f.stat().st_size > 64_000:
                    raise ValueError("template file too large")
                out.append(TemplateDef.model_validate(json.loads(f.read_text(encoding="utf-8"))))
            except (OSError, ValueError, ValidationError) as e:
                problems.append(f"{f.name}: {str(e).splitlines()[0][:120]}")
    return out, problems


def pick_template(templates: list[TemplateDef], text: str) -> TemplateDef | None:
    """Choose the template whose keywords match most often (>=1), else None."""
    low = text.lower()
    best, score = None, 0
    for t in templates:
        s = sum(1 for k in t.match_keywords if k.strip() and k.lower() in low)
        if s > score:
            best, score = t, s
    return best
