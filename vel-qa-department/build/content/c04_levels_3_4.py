"""Level 3 (Web & mobile) and Level 4 (API testing)."""
from lib import *  # noqa: F401,F403


def level3() -> str:
    return join(
        h1("Level 3 — Web and Mobile Testing",
           "Browsers, devices, forms, navigation, sessions, operating-system compatibility and accessibility: the "
           "environment-specific skills that turn generic test cases into real-world coverage."),
        level_header("About 2 weeks", "Level 2 passed.",
                     "Builds a risk-based browser/device matrix, uses developer tools to investigate failures, tests forms, "
                     "navigation and sessions systematically, and performs first-line accessibility checks.",
                     "Responsive + cross-browser test report on the Practice Shop, session-test checklist, accessibility checklist run, practical demo."),
        h2("Choosing the browser, device and OS matrix"),
        p("Never promise “all browsers”. Agree a **supported matrix** with the client, based on their analytics, contract "
          "and risk. A typical starting point is shown below; replace it with the client's own data."),
        table(["Dimension", "Typical minimum", "How to decide"], [
            ["Desktop browsers", "Latest Chrome, Edge, Firefox, Safari (macOS) + one previous major version where analytics show > 2 % share", "Client analytics (top 90 % of sessions)"],
            ["Mobile browsers", "Chrome on Android, Safari on iOS", "Analytics; app store minimum OS"],
            ["Viewports", "360×800 (small phone), 768×1024 (tablet), 1280×800 (laptop), 1920×1080 (desktop)", "Responsive breakpoints in the CSS"],
            ["Operating systems", "Windows 11, macOS current, Android (current and current−2), iOS (current and current−1)", "Support policy in the contract"],
            ["Real devices", "At least 1 low-end Android, 1 mid-range, 1 iPhone, 1 tablet", "Budget and risk; use a device cloud for the long tail"],
        ], widths=["20%", "50%", "30%"], first_bold=True),
        h2("Browser developer tools: the tester's microscope"),
        table(["Panel", "What to use it for", "Evidence to save"], [
            ["Elements", "Inspect DOM and CSS; check labels, alt text, `lang`, viewport meta; test CSS states.", "Screenshot of the element and computed styles"],
            ["Console", "JavaScript errors and warnings (red entries).", "Copy of the message with the URL and time"],
            ["Network", "Requests, status codes, timing, payloads, caching; throttle to Slow 3G; block a request to test error handling.", "HAR export (remove tokens and personal data first)"],
            ["Application", "Cookies, local/session storage, service workers; check cookie flags (`Secure`, `HttpOnly`, `SameSite`) and session clearing on logout.", "Screenshot of storage before and after logout"],
            ["Device toolbar", "Responsive viewport and device emulation (an approximation, not a real device).", "Screenshot at each tested viewport"],
            ["Lighthouse / Accessibility", "Quick audits; see Level 6.", "Report file attached to the test run"],
        ], widths=["17%", "55%", "28%"], first_bold=True),
        h2("Form testing checklist"),
        table(["Area", "Check"], [
            ["Required fields", "Submit with each required field empty, one at a time and all together; error is next to the field, announced, and the value already typed is preserved."],
            ["Type and format", "Wrong type (letters in numeric), invalid e-mail forms (`a@b`, `a@b.`, spaces), leading/trailing spaces, Unicode names, emoji, very long text."],
            ["Limits", "Min/max length and range boundaries (BVA); paste a value longer than the limit."],
            ["Behaviour", "Enter key submits; double-click on Submit sends one request; Tab order follows visual order; autofill and password managers work."],
            ["Messages", "Plain language, no stack traces, no leakage of internal data; same wording for credential failures (do not reveal which part was wrong)."],
            ["State", "Browser Back/Refresh after submit; form keeps or clears data as specified; success confirmation shown once."],
        ], widths=["20%", "80%"], first_bold=True),
        h2("Navigation and session testing"),
        table(["Topic", "What to verify"], [
            ["Links and routes", "No broken links (404); external links open as specified; deep links work when logged in and redirect to login (then back) when logged out; breadcrumbs and active menu state."],
            ["Back / forward / refresh", "No duplicate submissions; protected pages are **not** shown after logout when pressing Back (REQ-012)."],
            ["Session timeout", "Idle expiry after the specified time; clear message; unsaved work warning where required; session cannot be revived by simply reloading."],
            ["Concurrent sessions", "Same account in two browsers: behaviour matches the specification (allowed, or older session ended)."],
            ["Cookies / storage", "Session identifier changes after login; removed on logout; flags set; sensitive values are not stored in local storage in clear text (report, do not exploit)."],
            ["Tabs and windows", "Actions in one tab are reflected (or safely rejected) in another."],
        ], widths=["22%", "78%"], first_bold=True),
        h2("Mobile testing"),
        table(["Approach", "Strengths", "Limits", "Use for"], [
            ["Real devices", "True performance, sensors, gestures, OEM quirks.", "Cost, availability, setup time.", "Final verification, performance feel, camera/biometrics"],
            ["Emulators / simulators", "Cheap, fast, many OS versions.", "Not exact on performance, hardware, vendor skins.", "Early functional testing, OS-version coverage"],
            ["Device cloud", "Large device catalogue, remote access.", "Licence cost; network latency; data-privacy review needed.", "Long-tail coverage"],
        ], widths=["20%", "28%", "28%", "24%"], first_bold=True),
        p("Mobile-specific cases: install, upgrade (data preserved), uninstall; permissions (allow, deny, revoke later); "
          "interruptions (call, notification, low battery, app switch, screen rotation); network (offline, Wi-Fi ↔ mobile data switch, "
          "slow 3G); storage full; different languages and font sizes; push notifications; deep links; app store rules."),
        h2("Accessibility testing (first-line checks)"),
        p("Accessibility follows the four **POUR** principles of WCAG: Perceivable, Operable, Understandable, Robust. Automated "
          "tools find only a portion of issues; manual checks are mandatory."),
        table(["Check", "How", "Pass criterion"], [
            ["Keyboard only", "Unplug the mouse; use Tab, Shift+Tab, Enter, Space, Esc, arrow keys.", "Every function reachable and operable; focus is always visible; no keyboard trap."],
            ["Labels and names", "Inspect inputs/buttons; check `<label for>` or `aria-label`.", "Every control has an accessible name."],
            ["Alt text", "Inspect images.", "Informative images have meaningful `alt`; decorative `alt=\"\"`."],
            ["Colour contrast", "DevTools contrast ratio or an accessibility checker.", "Text ≥ 4.5 : 1 (large text ≥ 3 : 1)."],
            ["Zoom and reflow", "Browser zoom 200 %; 320 px width.", "No loss of content or function; no horizontal scrolling for text."],
            ["Screen reader", "NVDA (Windows), VoiceOver (macOS/iOS), TalkBack (Android); read a form and a table.", "Order, names, states and errors are announced."],
        ], widths=["20%", "42%", "38%"], first_bold=True),
        callout("info", "Standard to quote",
                "Contracts normally require **WCAG 2.1 or 2.2 Level AA** (published by the W3C). Confirm the exact version and "
                "level in the scope. We report conformance **findings**; we do not issue legal compliance certificates."),
        h2("Topic packs (ten-part teaching method)"),
        card10("3.1 Browser testing",
               "Checking that the application works in each supported browser and version.",
               "Rendering and JavaScript engines differ; users are split across browsers.",
               "Supported-browser matrix; BrowserStack-class cloud or local browsers.",
               "1. Fix the matrix. 2. Run smoke + top journeys per browser. 3. Compare screenshots. 4. Log browser-specific defects with version.",
               "Matrix with pass/fail per journey.",
               "Each fail reproduced twice and checked in a second browser to confirm it is browser-specific.",
               "Testing only in the developer's browser; not recording the exact version.",
               "Cross-browser table in the execution report.",
               "Run the A0 suite in Chrome, Firefox and WebKit.",
               "Report reviewed by QA Lead.")
        + card10("3.2 Responsive testing",
               "Verifying layout and function at different viewport sizes and orientations.",
               "A large share of traffic is mobile.",
               "DevTools device toolbar; real phone/tablet.",
               "1. Test the breakpoints and 10 px either side. 2. Rotate. 3. Check touch targets (≥ 24 px) and text zoom.",
               "Screenshots per viewport; list of layout defects.",
               "No horizontal scroll, no overlap, no clipped controls.",
               "Relying on emulation only; ignoring landscape.",
               "Evidence folder per viewport.",
               "Test REQ-013 at 360 px.",
               "Practical review.")
        + card10("3.3 Form testing",
               "Validation, submission, error handling and usability of input forms.",
               "Forms are where users and data enter the system.",
               "Form test checklist; synthetic data.",
               "1. Map every field and rule. 2. Apply EP/BVA. 3. Test submission behaviours. 4. Verify messages and persistence.",
               "Form test suite and results.",
               "Each rule has at least one passing and one failing input.",
               "Not testing paste, autofill, double submit or whitespace.",
               "Test cases linked to requirements.",
               "Full registration-form suite (A0).",
               "A0 marking rubric.")
        + card10("3.4 Navigation testing",
               "Verifying links, menus, routing, back/forward and deep links.",
               "Broken navigation means lost users.",
               "Sitemap; link checker (optional).",
               "1. Build a route map. 2. Click every route. 3. Test logged-in/out access. 4. Test Back/forward/refresh.",
               "Route map with verdicts.",
               "No 404s; protected routes redirect correctly.",
               "Skipping footer and error pages.",
               "Route table.",
               "Map and test all routes of the Practice Shop.",
               "Demonstration.")
        + card10("3.5 Session testing",
               "Verifying login state, timeout, logout and concurrent-session behaviour.",
               "Session faults cause data exposure and lost work.",
               "Two browsers; DevTools Application panel.",
               "1. Log in. 2. Stay idle until timeout. 3. Log out and use Back. 4. Open second session. 5. Compare with REQ-011/012.",
               "Session checklist with evidence.",
               "Observed behaviour matches the spec in each step.",
               "Using a stopwatch only once; forgetting to clear storage between cases.",
               "Checklist + screenshots.",
               "Run the session checklist.",
               "QA Lead review.")
        + card10("3.6 Mobile device testing",
               "Testing apps and mobile websites on real or virtual devices.",
               "Hardware, OS and network conditions change behaviour.",
               "Android emulator, one real phone, cloud account if licensed.",
               "1. Install the build. 2. Run core journeys. 3. Run interruptions and network cases. 4. Record device/OS.",
               "Device-by-journey results.",
               "Every failure notes device model, OS version, build.",
               "Not recording the OS version; testing only on a flagship phone.",
               "Execution report.",
               "Test the Practice Shop on a phone browser.",
               "Practical review.")
        + card10("3.7 Operating-system compatibility",
               "Confirming the product works across supported OS versions and configurations.",
               "OS updates frequently break software.",
               "VMs or test machines; compatibility matrix.",
               "1. List supported OS versions. 2. Install/run smoke. 3. Test upgrade from previous version.",
               "Compatibility matrix.",
               "Install, run, upgrade and uninstall pass on each OS.",
               "Skipping upgrade and uninstall paths.",
               "Matrix document.",
               "Build a matrix for a desktop app of your choice.",
               "Reviewed by the QA Lead.")
        + card10("3.8 Accessibility testing",
               "Checking that people with disabilities can use the product.",
               "Legal duty, ethical duty and a bigger market.",
               "Keyboard, NVDA/VoiceOver, axe DevTools, Lighthouse.",
               "1. Keyboard walk-through. 2. Automated scan. 3. Screen-reader read. 4. Zoom/contrast checks. 5. Log issues with WCAG criterion.",
               "Accessibility findings list with WCAG references.",
               "Every automated finding triaged; manual checks documented.",
               "Trusting automated scores alone.",
               "Findings in the defect tracker with the `a11y` label.",
               "Audit the registration page.",
               "Assessment by the QA Lead."),
        callout("gold", "Level 3 exit gate",
                "Cross-browser/responsive report, session checklist and accessibility findings for the Practice Shop reviewed and "
                "approved. Sign-off: QA Lead."),
        end_chapter(),
    )


def level4() -> str:
    return join(
        h1("Level 4 — API Testing",
           "Testing the interface behind the screen: HTTP, authentication, JSON, schemas, negative cases, "
           "and the foundations of API automation."),
        level_header("About 2 weeks", "Level 3 passed. Basic JSON understanding.",
                     "Sends and validates requests with curl and Postman, designs positive and negative API cases, validates JSON against a schema, "
                     "and reports API defects with request/response evidence.",
                     "Practice API test collection with ≥ 40 requests, ≥ 8 defects correctly reported, schema validation demonstrated."),
        h2("HTTP methods"),
        table(["Method", "Purpose", "Safe?", "Idempotent?", "Typical success"], [
            ["GET", "Read a resource.", "Yes", "Yes", "200"],
            ["POST", "Create a resource or trigger an action.", "No", "No", "201 (+ `Location`), 200, 202"],
            ["PUT", "Replace a resource completely.", "No", "Yes", "200 or 204"],
            ["PATCH", "Partially update a resource.", "No", "Not guaranteed", "200 or 204"],
            ["DELETE", "Remove a resource.", "No", "Yes", "204 (or 200)"],
            ["HEAD / OPTIONS", "Headers only / allowed methods and CORS.", "Yes", "Yes", "200 / 204"],
        ], widths=["14%", "38%", "10%", "16%", "22%"], first_bold=True),
        callout("gold", "Idempotent means repeat-safe",
                "Repeating an idempotent request leaves the server in the same state. Test it: send the same `PUT` twice and "
                "check state is unchanged; send `DELETE` twice and check the second response is `404` (resource already gone), "
                "not another `204`."),
        h2("Status codes to know by heart"),
        table(["Code", "Meaning", "When a test expects it"], [
            ["200 OK", "Success with a body.", "Valid GET or update"],
            ["201 Created", "Resource created; `Location` header gives its URL.", "Valid POST"],
            ["204 No Content", "Success, no body.", "Valid DELETE"],
            ["400 Bad Request", "Malformed or invalid input.", "Missing field, wrong type, bad range"],
            ["401 Unauthorized", "Not authenticated (missing/invalid/expired token).", "No token or bad token"],
            ["403 Forbidden", "Authenticated but not allowed.", "Normal user calls admin endpoint"],
            ["404 Not Found", "Resource does not exist.", "Unknown id; deleted resource"],
            ["405 Method Not Allowed", "Method unsupported on this URL.", "`DELETE /api/health`"],
            ["409 Conflict", "State conflict.", "Duplicate unique value"],
            ["415 / 422", "Unsupported media type / semantically invalid.", "Wrong `Content-Type`; business-rule failure"],
            ["429 Too Many Requests", "Rate limit.", "Burst above the limit (only if rate limiting is specified)"],
            ["500 / 502 / 503 / 504", "Server error / bad gateway / unavailable / gateway timeout.", "**Never** expected from client mistakes; report as defects"],
        ], widths=["20%", "42%", "38%"], first_bold=True),
        h2("Authentication types"),
        table(["Type", "How it travels", "What to test"], [
            ["API key", "Header such as `X-API-Key`.", "Missing, wrong, revoked key; key never in URLs or logs."],
            ["Basic auth", "`Authorization: Basic base64(user:pass)`.", "Only over HTTPS; wrong credentials → 401."],
            ["Bearer / JWT", "`Authorization: Bearer <token>`.", "Missing, malformed, expired, tampered token → 401; role checks → 403."],
            ["OAuth 2.0", "Access token obtained from an authorisation server.", "Scopes, token expiry, refresh flow, invalid grant."],
        ], widths=["18%", "34%", "48%"], first_bold=True),
        h2("Practice API contract (the target for Projects 3 and 5)"),
        table(["Method and path", "Auth", "Success", "Specified errors"], [
            ["`GET /api/health`", "No", "200 `{\"status\":\"ok\",\"service\":\"vel-practice-api\"}`", "—"],
            ["`POST /api/auth/login`", "No", "200 `{token, expiresIn}`", "400 missing field; 401 invalid credentials"],
            ["`GET /api/products?page=&limit=`", "No", "200 `{items,page,limit,total}`; `limit` 1–50, default 5", "400 invalid page/limit"],
            ["`GET /api/products/{id}`", "No", "200 product `{id,name,price,stock}`", "400 id not an integer; 404 unknown id"],
            ["`POST /api/products`", "Bearer", "201 + `Location`", "400 invalid body; 401; 409 duplicate name"],
            ["`PUT /api/products/{id}`", "Bearer", "200 updated product", "400; 401; 404"],
            ["`DELETE /api/products/{id}`", "Bearer", "204", "401; 404 unknown or already deleted"],
            ["`GET /api/orders`", "Bearer", "200 list", "401 without valid token"],
            ["`GET /api/slow?delay=ms`", "No", "200 after delay (0–5000 ms)", "400 invalid delay"],
        ], widths=["31%", "9%", "32%", "28%"]),
        p("Validation rules for products: `name` required string 1–60 characters; `price` number **> 0**; `stock` integer **≥ 0**. "
          "Practice credentials (synthetic, for the local server only): `qa@example.test` / `Passw0rd!QA`."),
        h2("Worked example with curl"),
        code("""# 1. Health
curl -i http://localhost:8080/api/health

# 2. Login and capture the token (jq optional)
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \\
  -H 'Content-Type: application/json' \\
  -d '{"email":"qa@example.test","password":"Passw0rd!QA"}' | python3 -c 'import sys,json;print(json.load(sys.stdin)["token"])')

# 3. Create a product (positive)
curl -i -X POST http://localhost:8080/api/products \\
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \\
  -d '{"name":"Marker","price":3.5,"stock":10}'
# Expect: HTTP/1.1 201 Created  +  Location: /api/products/<id>

# 4. Negative: price must be > 0
curl -i -X POST http://localhost:8080/api/products \\
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \\
  -d '{"name":"Free item","price":0,"stock":1}'
# Expect: 400 with an error message  (if you get 201, you have found a defect)""", "bash"),
        h2("JSON schema validation"),
        p("A **JSON Schema** states the shape of a valid response: required fields, types, ranges. Validating every response "
          "against a schema catches type drift (for example `price` returned as a string) that status-code checks miss."),
        file_code("automation/api/schemas/product.schema.json"),
        h2("API negative-testing checklist"),
        table(["Category", "Cases"], [
            ["Authentication", "No token; malformed token; expired token; token of another user; token in the wrong header."],
            ["Input validation", "Missing required field; wrong type (string for number); null; empty string; too long; negative; zero; huge number; special characters; wrong `Content-Type`; invalid JSON."],
            ["Boundaries", "`limit` 0, 1, 50, 51; `page` 0, −1; price 0.01 and 0."],
            ["Resource state", "Unknown id; already-deleted id; duplicate create; update of a deleted resource."],
            ["Methods", "Unsupported method on each URL → 405 with `Allow` header."],
            ["Errors and leakage", "No stack traces, SQL, file paths or internal hostnames in error bodies; errors use the documented JSON shape and `Content-Type`."],
            ["Performance sanity", "Response time for a single request within the agreed budget; large `limit` does not exhaust the server."],
        ], widths=["20%", "80%"], first_bold=True),
        h2("Postman essentials"),
        ol(["Create a **Collection** (Practice API) with a **Collection Variable** `baseUrl` = `http://localhost:8080`.",
            "Create an **Environment** `local`; store the token in a variable (secrets use *secret*-type variables and are never committed).",
            "Add requests per endpoint; add **Tests** scripts (JavaScript) to assert status, time, headers and body.",
            "Run the collection with the **Collection Runner**, or from CI with `newman run collection.json -e local.json` (Newman is Apache-2.0, registry-verified).",
            "Export collection and a sanitised environment into `automation/api/postman/`."]),
        code("""// Postman "Tests" tab for POST /api/products
pm.test("status is 201", () => pm.response.to.have.status(201));
pm.test("Location header present", () => pm.response.to.have.header("Location"));
pm.test("response time < 500 ms", () => pm.expect(pm.response.responseTime).to.be.below(500));
const body = pm.response.json();
pm.test("price is a number", () => pm.expect(body.price).to.be.a("number"));
pm.collectionVariables.set("createdId", body.id);""", "postman-tests.js"),
        h2("Topic packs (ten-part teaching method)"),
        card10("4.1 HTTP methods",
               "Verbs that tell the server what operation to perform on a resource.",
               "Wrong method use is a common API defect and a security smell.",
               "curl or Postman; Practice API running.",
               "1. Call each endpoint with each method. 2. Record status. 3. Compare with the contract.",
               "A method × endpoint matrix of observed statuses.",
               "Unsupported methods return 405 with `Allow`.",
               "Testing only GET and POST.",
               "API test sheet.",
               "Build the matrix for all Practice API endpoints.",
               "Reviewed against the contract.")
        + card10("4.2 Request and response validation",
               "Checking headers, status, body and timing of every call.",
               "Tests are only as good as their assertions.",
               "Postman Tests tab or pytest.",
               "1. Define the expected status, headers and body. 2. Assert each. 3. Add time budget.",
               "A request with 4+ assertions.",
               "Break the request on purpose; the assertion must fail.",
               "Only asserting 200.",
               "Collection export.",
               "Add assertions to five requests.",
               "Demo.")
        + card10("4.3 Status codes",
               "Three-digit codes that summarise the result of a request.",
               "Clients branch on them; wrong codes break integrations.",
               "Status-code table.",
               "1. For each endpoint list success and error codes from the contract. 2. Provoke each. 3. Compare.",
               "Expected vs actual table.",
               "All specified codes provoked at least once.",
               "Accepting 500 as an “error response”.",
               "Defects for each mismatch.",
               "Provoke 400, 401, 404, 409 on the Practice API.",
               "Quiz: 10 codes.")
        + card10("4.4 Authentication",
               "Proving identity to the API (key, basic, bearer/JWT, OAuth).",
               "Broken authentication exposes data.",
               "Practice credentials; Postman.",
               "1. Call without token. 2. Wrong token. 3. Valid token. 4. Expired token (if available).",
               "401 for each invalid case; 200/201 for valid.",
               "No protected endpoint works without a valid token.",
               "Sharing tokens in screenshots; committing tokens.",
               "Defects; security coordinator notified for access-control faults.",
               "Test all protected endpoints without a token.",
               "QA Lead review; escalation rule explained.")
        + card10("4.5 JSON validation",
               "Checking the structure and values of JSON bodies.",
               "Bad JSON breaks clients.",
               "Browser/JSON viewer, Postman.",
               "1. Check types. 2. Check required fields. 3. Check nested objects and arrays. 4. Check ranges.",
               "Validation notes per endpoint.",
               "Matches the documented model.",
               "Eyeballing instead of asserting.",
               "Assertions in tests.",
               "Validate `/products` response items.",
               "Practical test.")
        + card10("4.6 Schema validation",
               "Automated check of a response against a JSON Schema.",
               "Detects silent type changes and missing fields.",
               "Schema file; Postman `tv4`/Ajv or Python `jsonschema` (MIT, registry-verified).",
               "1. Write the schema. 2. Validate every product in the list. 3. Add negative schema (extra field).",
               "Schema test that fails for the broken item.",
               "Schema catches the seeded string-price defect.",
               "Schema too loose (`type: object` only).",
               "Defect with schema error message.",
               "Write schemas for Product and Product list.",
               "Peer review.")
        + card10("4.7 API negative testing",
               "Sending invalid, missing or hostile input to prove safe failure.",
               "Real clients and attackers send unexpected data.",
               "Negative-testing checklist.",
               "1. Pick endpoint. 2. Apply each category. 3. Record actual response. 4. Raise defects for 5xx and silent acceptance.",
               "20+ negative cases with results.",
               "No 5xx; clear 4xx; no data change on rejected requests.",
               "Stopping at obvious cases.",
               "Defect reports with request/response.",
               "Write 20 negative cases for `/api/products`.",
               "Review.")
        + card10("4.8 API automation",
               "Running API checks from code (pytest + requests) or Newman, repeatably.",
               "Fast, stable regression for integrations.",
               "Python 3.11+, `pytest`, `requests`, `jsonschema`.",
               "1. Create fixtures for base URL and token. 2. Write parametrised tests. 3. Run in CI. 4. Publish results.",
               "Passing and failing tests with HTML/JUnit report.",
               "Known defects show as expected failures (`xfail` with defect ID).",
               "Shared state between tests; hard-coded ids and secrets.",
               "`automation/api/` and reports.",
               "Automate 15 cases (Project 5).",
               "Code review against checklist."),
        callout("gold", "Level 4 exit gate",
                "Collection ≥ 40 requests with assertions, ≥ 8 correctly reported API defects, schema validation demonstrated. "
                "Sign-off: QA Lead."),
        end_chapter(),
    )
