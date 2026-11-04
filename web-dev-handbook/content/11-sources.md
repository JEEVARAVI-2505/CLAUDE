Learn from **official documentation first**. Blog posts and videos are supplements; when they disagree with the official documentation, the documentation wins. Links point to the documentation roots; sites reorganise their pages, so use each site's search if a deep link moves. Check version numbers - documentation for the wrong version causes real bugs.

## Required official sources

| Source | Link | Best used for | Relevant modules |
|---|---|---|---|
| MDN Web Docs | [developer.mozilla.org](https://developer.mozilla.org/) | HTML, CSS, JavaScript, HTTP, web APIs, accessibility - the primary reference | L1.1-L1.4, L1.9, L2.1-L2.7 |
| HTML Living Standard | [html.spec.whatwg.org](https://html.spec.whatwg.org/) | Exact element and attribute behaviour (reference, not tutorial) | L2.1 |
| TypeScript documentation | [typescriptlang.org/docs](https://www.typescriptlang.org/docs/) | Types, generics, `tsconfig`, handbook | L2.4, L2.8 |
| React documentation | [react.dev/learn](https://react.dev/learn) | Components, state, effects, thinking in React | L2.8 |
| Next.js documentation | [nextjs.org/docs](https://nextjs.org/docs) | Routing, rendering, data fetching, deployment | L2.9 |
| Node.js documentation | [nodejs.org/docs](https://nodejs.org/docs/) | Runtime APIs, modules, `http`, streams, test runner | L1.2, L3.1 |
| FastAPI | [fastapi.tiangolo.com](https://fastapi.tiangolo.com/) | Python APIs, validation, dependencies, security, testing | L3.1-L3.9 |
| Django documentation | [docs.djangoproject.com](https://docs.djangoproject.com/) | ORM, admin, forms, authentication, migrations | L3.1, L4.7 |
| PostgreSQL documentation | [postgresql.org/docs](https://www.postgresql.org/docs/) | SQL, indexes, transactions, backup, performance | L4.1-L4.8 |
| GitHub Docs | [docs.github.com](https://docs.github.com/) | Repositories, PRs, Actions, security features | L5.1, L5.2, L5.7 |
| Docker Docs | [docs.docker.com](https://docs.docker.com/) | Dockerfile, Compose, networking, security | L5.6 |
| OWASP | [owasp.org](https://owasp.org/) | Top 10, ASVS, cheat sheets, API security | L3.3-L3.8, L5.11 |
| web.dev | [web.dev](https://web.dev/) | Performance, Core Web Vitals, accessibility, PWA, learning paths | L1.9, L2.6, L2.7, L5.10 |

## Recommended additional official sources

| Source | Link | Used for |
|---|---|---|
| Pro Git book | [git-scm.com/book](https://git-scm.com/book/en/v2) | Git in depth (L5.1) |
| Playwright | [playwright.dev](https://playwright.dev/docs/intro) | End-to-end testing (L5.5) |
| WCAG 2.2 quick reference | [w3.org/WAI/WCAG22/quickref](https://www.w3.org/WAI/WCAG22/quickref/) | Accessibility success criteria (L2.6) |
| MySQL reference manual | [dev.mysql.com/doc](https://dev.mysql.com/doc/) | MySQL/MariaDB (L4.1) |
| OWASP Cheat Sheet Series | [cheatsheetseries.owasp.org](https://cheatsheetseries.owasp.org/) | Practical secure-coding guidance |
| OWASP ASVS | [owasp.org/www-project-application-security-verification-standard](https://owasp.org/www-project-application-security-verification-standard/) | Security verification requirements |
| Pydantic | [docs.pydantic.dev](https://docs.pydantic.dev/) | Validation models (L3.6) |
| Cloudflare Developers | [developers.cloudflare.com](https://developers.cloudflare.com/) | DNS, CDN, WAF, Pages (L1.4, L5.8) |
| AWS documentation | [docs.aws.amazon.com](https://docs.aws.amazon.com/) | AWS services (L5.8) |
| Microsoft Learn - Azure | [learn.microsoft.com/azure](https://learn.microsoft.com/azure/) | Azure services (L5.8) |
| Google Cloud documentation | [cloud.google.com/docs](https://cloud.google.com/docs) | GCP services (L5.8) |
| VS Code documentation | [code.visualstudio.com/docs](https://code.visualstudio.com/docs) | Editor (L5.3) |
| Chrome DevTools | [developer.chrome.com/docs/devtools](https://developer.chrome.com/docs/devtools) | Debugging and performance (L2.7) |
| Astro docs | [docs.astro.build](https://docs.astro.build/) | Content-site alternative (L2.9) |
| Postman Learning Center | [learning.postman.com](https://learning.postman.com/) | API testing (L1.8, L5.5) |

## Reading plan by level

| Level | Read first | Then |
|---|---|---|
| **1** | MDN "Learn web development" and HTTP overview; web.dev "Learn Responsive Design" | OWASP Top 10 introduction |
| **2** | MDN HTML, CSS, JavaScript guides; HTML Living Standard (reference); web.dev Accessibility and Performance paths | TypeScript handbook; react.dev/learn (complete the tutorial); Next.js "Learn" course |
| **3** | FastAPI tutorial (all sections: query, body, dependencies, security, testing) or Django tutorial; Node.js docs | OWASP API Security Top 10, Authentication and Session cheat sheets |
| **4** | PostgreSQL tutorial, SQL commands, indexes, transactions, backup | `EXPLAIN` guide; migrations tool docs |
| **5** | Pro Git ch. 1-3, 7; GitHub Actions docs; Docker "Get started" | Playwright intro; cloud provider "getting started"; OWASP ASVS |

!!! tip "How to read documentation"
    1) Read the overview and concepts page. 2) Run the smallest example. 3) Change one thing and observe. 4) Read the reference for the exact option you used. 5) Write three lines of notes with the link in your training repository. Always note the **version** you used.

## Reliability rules for sources

- Prefer the official site; check the "last updated" date and the version selector.
- For anything involving **prices, limits, licences, security advisories or compliance**, use only the official page dated within the last 30 days.
- Do not rely on AI-generated answers, forums or videos as evidence for a client decision - verify against official documentation or run a test.
- Record source links in ADRs and in `docs/architecture.md`.
