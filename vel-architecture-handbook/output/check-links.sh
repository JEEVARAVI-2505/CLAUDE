#!/usr/bin/env bash
# Checks every official source link used in the handbook. Run from a machine with normal internet access.
set -u
ok=0; bad=0
for u in \
  https://c4model.com/ \
  https://martinfowler.com/architecture/ \
  https://www.opengroup.org/togaf \
  https://www.iso.org/ \
  https://learn.microsoft.com/azure/architecture/ \
  https://docs.aws.amazon.com/wellarchitected/ \
  https://cloud.google.com/architecture/framework \
  https://owasp.org/ \
  https://www.nist.gov/cyberframework \
  https://www.uml.org/ \
  https://plantuml.com/ \
  https://mermaid.js.org/ \
  https://www.drawio.com/ \
  https://adr.github.io/ \
  https://arc42.org/ \
  https://docs.structurizr.com/ \
  https://developer.hashicorp.com/terraform/docs \
  https://learning.postman.com/ \
  https://owasp.org/www-project-threat-dragon/ \
  https://learn.microsoft.com/azure/security/develop/threat-modeling-tool \
  https://spec.openapis.org/oas/latest.html \
  https://sre.google/books/ \
  https://12factor.net/
do
  code=$(curl -s -o /dev/null -L -m 25 -A "Mozilla/5.0 (VEL link check)" -w "%{http_code}" "$u")
  case "$code" in 2*|3*) echo "OK   $code $u"; ok=$((ok+1));; *) echo "FAIL $code $u"; bad=$((bad+1));; esac
done
echo "OK=$ok FAIL=$bad  ($(date -u +%F))"
[ "$bad" -eq 0 ]
