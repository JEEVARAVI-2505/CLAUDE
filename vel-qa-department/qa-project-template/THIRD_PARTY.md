# Third-party tools and packages

Record every tool, exact version and licence **before first use on a client project**.
Versions below were read from npm / PyPI metadata on 2026-11-04 and will move on; re-verify when upgrading.

| Tool / package | Version | Licence | Used for |
|---|---|---|---|
| pytest | 9.2.0 | MIT | Test runner |
| pytest-playwright | 0.10.0 | Apache-2.0 | Playwright fixtures for pytest |
| playwright (Python) | 1.64.1 | Apache-2.0 | Browser automation |
| requests | 2.34.2 | Apache-2.0 | HTTP client for API tests |
| jsonschema | 4.26.0 | MIT | JSON-schema validation |
| pytest-xdist | 3.8.0 | MIT | Parallel test execution |
| locust (optional) | 2.47.0 | MIT | Load testing the local practice API |
| axe-core (optional, npm) | 4.14.0 | MPL-2.0 | Automated accessibility scans |
| k6 (optional, external binary) | — | AGPL-3.0 (verify on the official repository) | Load testing scripts |

Not verified from package registries: k6, Apache JMeter, SonarQube, Postman, Jira, TestRail — check each vendor's official page.
