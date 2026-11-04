// performance/k6-products.js   Run: k6 run performance/k6-products.js
// LOCAL practice API only - never point this at a system you are not authorised to load-test.
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // ramp-up
    { duration: '2m',  target: 10 },   // steady state (measure here)
    { duration: '20s', target: 0 },    // ramp-down
  ],
  thresholds: {                         // agreed pass/fail criteria
    http_req_failed:   ['rate<0.01'],   // < 1 % errors
    http_req_duration: ['p(95)<500'],   // 95 % of requests under 500 ms
  },
};

const BASE = __ENV.BASE_URL || 'http://127.0.0.1:8080';

export default function () {
  const res = http.get(`${BASE}/api/products?limit=5`);
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(1);                             // think time
}
