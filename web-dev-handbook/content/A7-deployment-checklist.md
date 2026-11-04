Use for **every** staging and production deployment. The DevOps engineer executes; the Technical Lead gives go/no-go; the PM informs the client. Keep the completed checklist as deployment evidence.

| Project | | Version / tag | | Environment | |
|---|---|---|---|---|---|
| Date / window | | Executed by | | Approved by | |

## A. Pre-deployment (T minus 1 day)

| Item | Done |
|---|---|
| Client approved the change plan and the maintenance window (UAT signed for production) | <span class="box"></span> |
| Release candidate tagged; CHANGELOG and release notes complete | <span class="box"></span> |
| CI green on the exact commit; the same image digest was tested in staging | <span class="box"></span> |
| Infrastructure matches the approved IaC/plan (no manual drift) | <span class="box"></span> |
| Secrets present in the secret store for this environment; none in the repository | <span class="box"></span> |
| Database migrations reviewed; backward compatible; rollback or forward-fix plan written | <span class="box"></span> |
| **Fresh backup taken and restore verified** | <span class="box"></span> |
| Monitoring, alerts, on-call contacts and cost/budget alerts active | <span class="box"></span> |
| Rollback plan tested in staging; previous image tag retained | <span class="box"></span> |
| DNS plan ready; TTL lowered to 300 s at least 24 h before cut-over (if DNS changes) | <span class="box"></span> |
| Admin ports closed; database private; least-privilege IAM confirmed | <span class="box"></span> |
| Communication sent to client and team (time, expected impact, contact) | <span class="box"></span> |

## B. Deployment

| Step | Time | Done |
|---|---|---|
| Announce start in the team channel; open the deployment log | | <span class="box"></span> |
| Enable maintenance mode if needed | | <span class="box"></span> |
| Run the pipeline/promotion with approval; apply migrations | | <span class="box"></span> |
| Verify the running version and configuration | | <span class="box"></span> |
| Smoke tests: home, login, key journey, API `/health`, payment sandbox/live-safe check | | <span class="box"></span> |
| Disable maintenance mode | | <span class="box"></span> |

## C. Post-deployment (first 60 minutes)

| Item | Done |
|---|---|
| Logs show no error spike; p95 latency and 5xx rate within normal range | <span class="box"></span> |
| Monitoring shows green; uptime check passing | <span class="box"></span> |
| First production backup completed | <span class="box"></span> |
| HTTPS valid (padlock, chain, HSTS as planned); HTTP redirects to HTTPS; no mixed content | <span class="box"></span> |
| DNS records verified from two public resolvers; e-mail send/receive tested | <span class="box"></span> |
| Search engines: production indexable, staging blocked; sitemap submitted | <span class="box"></span> |
| Analytics and error tracking receiving events | <span class="box"></span> |
| Client smoke check and confirmation received | <span class="box"></span> |

## D. Evidence record

| Field | Value |
|---|---|
| Deployed version / image digest | |
| Start / end time | |
| Approver (name) | |
| Test results (link) | |
| Issues encountered and actions | |
| Rollback executed? (Y/N, reason) | |

!!! danger "ROLL BACK IMMEDIATELY IF"
    Login or payment fails; error rate is clearly above baseline; data corruption is suspected; the smoke tests fail and the cause is not found within the agreed 15 minutes. Then report to the TL and PM and write a post-incident review.
