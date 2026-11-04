The department teaches and uses a broad toolbox, but **selects technology according to project requirements** instead of forcing one stack on every client. This section classifies each tool as **essential** or **optional**, and **free** or **paid**. Licence terms, free-tier limits and prices change - always check the official page on the day of use.

## Tool classification

| Tool / technology | Role | Essential or optional | Free or paid | Notes |
|---|---|---|---|---|
| HTML, CSS, JavaScript | Core web languages | **Essential** | Free (open standards) | MDN and the HTML Living Standard are the references |
| TypeScript | Typed JavaScript | **Essential** (all new projects) | Free (open source) | `strict` mode on |
| React | UI library | **Essential** | Free (open source) | Default for interactive UIs |
| Next.js | React framework (SSR/SSG, routing, API) | **Essential** | Free (open source); hosting may cost | Alternatives - Astro, Remix, Vite SPA, Nuxt, SvelteKit |
| Node.js | JavaScript runtime / backend | Essential | Free (open source) | LTS versions only for production |
| FastAPI | Python API framework | **Essential** (API-first products) | Free (open source) | Pydantic validation, OpenAPI docs |
| Django | Python full-stack framework | Optional (admin/ORM-heavy projects) | Free (open source) | Strong built-in admin and auth |
| PostgreSQL | Relational database | **Essential** (default) | Free (open source); managed hosting is paid | |
| MySQL / MariaDB | Relational database | Optional (WordPress/WooCommerce, legacy) | Free (community edition); managed hosting is paid | |
| Git | Version control | **Essential** | Free | |
| GitHub | Hosting, PRs, Actions | **Essential** | Free and paid plans - verify current limits | Organisation account with MFA |
| VS Code | Editor | **Essential** | Free | Shared extension pack |
| Docker | Containers | **Essential** | Engine free; Docker Desktop licence terms vary by company size - verify | Podman is an alternative |
| Playwright | End-to-end testing | **Essential** (critical journeys) | Free (open source) | Runs in CI |
| Postman | API testing | Essential (or curl/Bruno/Insomnia) | Free tier + paid plans - verify | Export collections to Git |
| AWS / Azure / GCP | Cloud hosting | One **essential** per project | Pay-as-you-go; free tiers have limits - verify | Choose per client, region, budget |
| Cloudflare (or similar CDN) | DNS, CDN, WAF, caching | Optional but recommended | Free and paid plans - verify | Speed and DDoS protection |
| Terraform / OpenTofu | Infrastructure as code | Optional (medium/enterprise) | Free (open source) | |
| Sentry or similar | Error tracking | Recommended | Free tier + paid - verify | |
| Figma | UI/UX design | Essential for design work | Free tier + paid - verify | |
| Lighthouse, axe, ZAP | Quality and security scans | **Essential** | Free | |

## How to choose a stack

{{diagram: stack_choice | Figure 4.1 - Stack selection guide (start with the simplest option that meets the requirement)}}

| Criterion | Questions to ask | Effect on the choice |
|---|---|---|
| Content or application? | Does the client mainly publish content, or do users log in and transact? | Content - static/CMS; application - Next.js + API + database |
| Who edits content? | Non-technical staff or developers? | Staff - headless CMS or WordPress; developers - Markdown/Git |
| Traffic and growth | Visitors per month now and in 12 months; peaks | Low - managed platform; high - CDN, caching, scaling |
| Data sensitivity | Personal, financial, health data? | Higher - private networking, encryption, audit, stricter hosting region |
| Integrations | Payment, ERP, CRM, SMS, WhatsApp | Prefer providers with good SDKs and webhooks |
| Team skills | Who maintains it after launch? | Prefer stacks the client's team or ours can maintain |
| Budget and running cost | Monthly cost the client can sustain | Managed services trade money for time |
| Time to market | Deadline | Buy (Shopify, WordPress, SaaS) before build |
| Compliance and region | Data residency, accessibility law, tax | Choose provider regions accordingly |

## Standard stacks (starting points, not rules)

| Project type | Frontend | Backend | Database | Hosting pattern |
|---|---|---|---|---|
| Brochure / landing page | Astro or Next.js static | Serverless function for forms | - | CDN static hosting |
| Corporate site with CMS | Next.js | Headless CMS (or WordPress) | CMS-managed | Managed platform + CDN |
| E-commerce (standard) | Shopify / WooCommerce theme | Platform | Platform | SaaS or managed WordPress |
| E-commerce (custom) | Next.js | FastAPI or Node | PostgreSQL | Container service + managed DB + CDN |
| Customer portal / admin | Next.js + React | FastAPI/Django/Node | PostgreSQL | Container service + managed DB |
| SaaS | Next.js | FastAPI/Node + queue worker | PostgreSQL + cache | Containers, CI/CD, monitoring, backups |
| Booking system | Next.js | FastAPI/Node | PostgreSQL | Container service + e-mail/SMS provider |

!!! tip "Record the decision"
    Write an **Architecture Decision Record (ADR)** for every major choice (see L1.5 for the template): the context, at least two options, the decision, the consequences and the date any vendor facts were verified.

## Licence and supply-chain rules

- Use only software whose licence allows commercial use; record each dependency's licence (`npm ls`, `pip-licenses`).
- Commit lock files (`package-lock.json`, `poetry.lock`/`requirements.txt` with hashes) and enable Dependabot.
- Do not install unknown packages or extensions; check download counts, maintainers and recent activity.
- Never paste client code or data into unapproved online tools or AI assistants. Use only company-approved tools and follow the client contract.
