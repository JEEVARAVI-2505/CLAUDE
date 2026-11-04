The Web Development Engineering Department builds, launches and maintains web products for business clients. Fourteen commercially deliverable services are offered. Each can be sold alone or combined, and each follows the same 27-stage lifecycle (Section 5) - the lifecycle is scaled to the project, never skipped.

## Commercial service portfolio

| # | Service | What we deliver | Typical client trigger | Typical stack (chosen per project) |
|---|---|---|---|---|
| 1 | **Corporate website** | Multi-page brand site, CMS-editable content, SEO, analytics, contact/lead forms | "We need a professional company site." | Next.js or Astro + headless CMS, or WordPress |
| 2 | **Business website** | Small business site (3-8 pages), maps, WhatsApp/phone links, enquiry form | "We only have a Facebook page." | Static/Next.js + serverless form, or WordPress |
| 3 | **Landing page** | One-page campaign site, fast, tracked, A/B-test ready | "We run ads and need a page that converts." | Static HTML/Next.js on CDN |
| 4 | **E-commerce platform** | Catalogue, cart, checkout, payments, shipping, tax invoices, admin | "We want to sell online." | Shopify / WooCommerce, or custom Next.js + PostgreSQL |
| 5 | **Custom web application** | Workflow software built to the client's process | "Our spreadsheets cannot cope." | React/Next.js + FastAPI/Node + PostgreSQL |
| 6 | **SaaS web application** | Multi-tenant product with billing, roles, onboarding, analytics | "We want to launch a software product." | Next.js + API + PostgreSQL + queue + billing provider |
| 7 | **Customer portal** | Secure login area - orders, invoices, tickets, documents | "Customers keep e-mailing us for statements." | Next.js + API + managed identity |
| 8 | **Admin dashboard** | Internal control panel - users, content, reports, exports | "We need to manage the data without a developer." | React admin + API, or Django admin |
| 9 | **CMS development** | Content model, editor experience, preview, roles, workflows | "Staff must update the site without us." | Sanity/Strapi/Directus/WordPress/Django CMS |
| 10 | **Booking and appointment system** | Services, availability, calendar sync, reminders, payment, cancellations | "Phone bookings waste our time." | Next.js + API + PostgreSQL + e-mail/SMS provider |
| 11 | **Payment gateway integration** | Hosted checkout, webhooks, refunds, reconciliation | "Add online payments to our site." | Gateway SDK + server-side verification |
| 12 | **Website redesign and modernisation** | Audit, new design, migration of content/URLs, redirects, SEO protection | "Our site looks old and is slow." | Depends on audit |
| 13 | **Performance optimisation** | Core Web Vitals audit and fixes, caching, CDN, image pipeline, database tuning | "The site is slow and ranks badly." | Lighthouse, WebPageTest, CDN, profiling |
| 14 | **Maintenance and managed support** | Monitoring, patching, backups, content changes, SLA support, monthly report | "Nobody looks after our site." | Uptime monitor, Dependabot, ticket system |

## Service packaging

| Package | Typical contents | Pricing approach |
|---|---|---|
| **Starter** | Landing page or small business site, 1 revision round, 30-day warranty | Fixed price |
| **Professional** | Corporate site or portal, 2 revision rounds, staging review, 60-day warranty, 3-month support option | Fixed price + monthly service |
| **Advanced** | Custom app / e-commerce / SaaS with phased milestones, security review, 90-day warranty | Milestone-based fixed price or time-and-materials with cap |
| **Managed** | Maintenance, monitoring, hosting management, SLA | Monthly retainer (MRR) |

!!! gate "SELECT TECHNOLOGY BY REQUIREMENT"
    We never force one stack on every client. A five-page brochure site does not need a Kubernetes cluster; a payment-processing SaaS does not belong on a 30-page WordPress theme. The Technical Lead records every stack decision in an Architecture Decision Record (Stage 09).

## Delivery models

| Model | Best for | Control | Risk to us |
|---|---|---|---|
| **Fixed price** | Clearly defined scope | Change-request procedure | Under-estimation - mitigated by contingency and a frozen scope |
| **Time and materials (capped)** | Evolving requirements | Weekly timesheet report; cap needs written approval to raise | Low |
| **Milestone-based** | Large custom products | Payment tied to accepted deliverables | Medium |
| **Retainer / managed service** | Ongoing support and improvement | SLA, monthly report, hour bank | Un-priced "unlimited" support - never offer it |

## Department structure

| Role | Reports to | Core responsibility |
|---|---|---|
| Technical Lead | Director | Architecture, standards, code quality, security sign-off |
| Project Manager | Director | Scope, schedule, budget, client communication |
| UI/UX Designer | Technical Lead / PM | Research, wireframes, design system, accessibility |
| Frontend and backend developers (junior to senior) | Technical Lead | Build and test features |
| Full-stack developer | Technical Lead | Features end to end |
| QA engineer | Technical Lead | Test planning, automation, release readiness |
| DevOps engineer | Technical Lead | Environments, CI/CD, hosting, monitoring, backups |

Detailed role descriptions are in Section 9.

!!! danger "WHAT WE DO NOT DO"
    We do not copy a competitor's text, images or code. We do not build deceptive, illegal or privacy-violating systems. We do not store card numbers ourselves. We do not offer legal, tax or regulatory advice; we offer **compliance-readiness support** and say that certification and legal compliance are determined by qualified auditors and counsel. We never claim a system is "secure" or "guaranteed" - we state what was tested.
