# performance

**LOCAL practice API only.** Never run these scripts against shared, production or third-party systems without written authorisation.

```bash
# Locust (pip install locust), headless, 10 virtual users, 30 s
locust -f performance/locustfile.py --headless -u 10 -r 2 -t 30s --host http://127.0.0.1:8080
# k6 (install from the official documentation)
k6 run performance/k6-products.js
```
Write the plan first (workload model, thresholds, environment, stop conditions), validate with one user, then measure steady state only.
