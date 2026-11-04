"""Locust load script for the Practice API (LOCAL ONLY). Run headless:
   locust -f locustfile.py --headless -u 10 -r 2 -t 30s --host http://127.0.0.1:8080 --csv results
Workload (see handbook, Level 6): read-heavy browse with think time. Thresholds are checked by check_thresholds.py."""
from locust import HttpUser, between, task


class Browser(HttpUser):
    wait_time = between(0.5, 1.5)          # think time

    @task(5)
    def list_products(self):
        self.client.get("/api/products?limit=5", name="GET /api/products")

    @task(2)
    def product_detail(self):
        self.client.get("/api/products/1", name="GET /api/products/{id}")

    @task(1)
    def slow_call(self):
        self.client.get("/api/slow?delay=200", name="GET /api/slow?delay=200")
