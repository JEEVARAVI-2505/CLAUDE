#!/usr/bin/env python3
"""Auto-quote plain YAML scalars that contain ': ' or ' #' so authored content never breaks the parser."""
import re, sys, yaml
for path in sys.argv[1:]:
    lines, out, block_indent = open(path, encoding="utf-8").read().split("\n"), [], None
    for ln in lines:
        ind = len(ln) - len(ln.lstrip())
        if block_indent is not None:
            if ln.strip() == "" or ind > block_indent:
                out.append(ln); continue
            block_indent = None
        m = re.match(r"^(\s*)((?:- )?(?:[A-Za-z_][\w]*: )?)(.*)$", ln)
        if m and m.group(3):
            pre, key, val = m.group(1), m.group(2), m.group(3)
            if re.match(r"^[|>][+-]?\d*$", val):
                block_indent = ind; out.append(ln); continue
            if key and not val.startswith(('"', "'", "[", "{")) and (": " in val or " #" in val or val[0] in "*&!%@`" or val.endswith(":")):
                val = '"' + val.replace("\\", "\\\\").replace('"', '\\"') + '"'
                ln = pre + key + val
            elif not key.strip().endswith(":") and key.strip() == "-" and not val.startswith(('"', "'", "[", "{")) and (": " in val or " #" in val):
                val = '"' + val.replace("\\", "\\\\").replace('"', '\\"') + '"'
                ln = pre + key + val
        out.append(ln)
    open(path, "w", encoding="utf-8").write("\n".join(out))
    try:
        d = yaml.safe_load(open(path, encoding="utf-8")); print(path, "OK", {k: len(v) for k, v in d.items()})
    except yaml.YAMLError as e:
        print(path, "ERROR", e)
