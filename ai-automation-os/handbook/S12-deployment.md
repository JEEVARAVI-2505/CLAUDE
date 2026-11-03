# Stage S12 — Deployment (Gate G10)

## Purpose
Put the system on real servers so it runs reliably, securely and visibly, and so we can undo a bad release quickly.

## Who and when
- **Owner:** DevOps Engineer (you assist). **Approver (G10):** DevOps, Technical Lead, Founder.
- **Time:** 3–5 days.

## Before you start
G8 and G9 passed; staging working; production accounts and domain approved by the Founder.

## Concepts in plain words
- **Container (Docker):** a sealed box holding the program and everything it needs, so it runs the same everywhere.
- **Reverse proxy:** the front door that handles HTTPS and forwards requests inside.
- **Migration:** a scripted database change applied the same way in every environment.
- **Rollback:** returning to the previous working version.
- **Runbook:** step-by-step instructions for fixing common problems.

## Step by step

1. **Containerise.** The reference project includes a `Dockerfile` as a starting example (it has not been built in the authoring environment; test it on your machine and fix as needed):
   ```bash
   docker build -t bill-service:0.1.0 .
   docker run --rm -p 8000:8000 bill-service:0.1.0
   ```
2. **Choose hosting** (cloud virtual machine or managed service) per the architecture and budget; record in `decisions.md`.
3. **Prepare the server:** operating-system updates, firewall allowing only required ports (443 for HTTPS), non-root user, automatic security updates.
4. **Set up domain and HTTPS:** DNS records, TLS certificate with automatic renewal.
5. **Set up the database:** PostgreSQL with strong passwords, encrypted storage, access only from the application. For n8n, use PostgreSQL (not the default SQLite) in production.
6. **Store secrets** in the secret manager or secure environment configuration. Production values differ from staging. Record who has access.
7. **n8n specifics (if used):** keep the encryption key backed up separately (without it stored credentials are unrecoverable); limit execution-history retention; do **not** expose the editor publicly — put it behind VPN or single sign-on with an IP allowlist; use queue mode with workers when volume grows; upgrade on staging first.
8. **Deploy to staging** with the same steps you will use in production. Run the smoke tests (health check, one sample bill end to end).
9. **Monitoring and alerts:** collect logs; dashboards for success rate, failures, queue size, latency, daily AI spend; alerts to a person for: failure rate above threshold, queue growing, spend at 80% of cap, certificate near expiry, server disk above 80%.
10. **Backups:** nightly database and file backups, encrypted, kept in a separate place. **Do a restore drill** on a test server and record how long it took. A backup never restored is a hope, not a backup.
11. **Rate and usage limits:** per-customer and global, with alerts.
12. **Rollback plan:** keep the previous container version and workflow export; write the exact commands; rehearse once on staging.
13. **Release:** tag the version (e.g. `v1.0.0`), write release notes, deploy at a quiet time, run smoke tests, watch dashboards for the first hour.
14. **Write the runbook:** common failures, what to check, who to call, how to restart, how to roll back, how to restore.
15. **Fill the Production Readiness Checklist (T12)** and request G10.

## Deliverables (`08-deployment/`)
- `deployment-guide.md`, `runbook.md`, `release-notes-v1.0.0.md`
- `readiness-checklist.pdf` (signed), restore-drill record, monitoring dashboard link list.

## Done checklist
- [ ] Production credentials are separate and stored safely.
- [ ] HTTPS works; editor/admin not publicly exposed.
- [ ] Smoke tests pass on production after deploy.
- [ ] Alerts reach a named person and were tested.
- [ ] Backup taken **and restored successfully**; time recorded.
- [ ] Rollback rehearsed.
- [ ] Runbook written and reviewed by Support.

## Common mistakes
- Deploying on Friday evening.
- Using the staging database or keys in production.
- Alerts that nobody receives.
- Never testing the restore.

## Gate G10 sign-off
DevOps, Technical Lead and Founder sign the readiness checklist.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
