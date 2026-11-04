This section teaches how to cost a web project, set the selling price, and price the monthly service - and gives worked examples for **small, medium and enterprise** projects. The **same numbers** are in the Excel workbook `VEL-Web-Project-Costing-Workbook.xlsx` (formulas live), so you can change an assumption and see every result update.

!!! danger "NO VENDOR PRICES ARE STATED IN THIS HANDBOOK"
    Vendor prices (hosting, cloud, domains, e-mail, payment fees, licences) change often and differ by region, plan and date, and this edition could not retrieve live vendor pricing pages. Every vendor and internal-rate figure below is a **clearly labelled placeholder in generic currency units (CU)**, shown only to demonstrate the method. **Before any quotation**, replace each placeholder with the figure from the vendor's **official pricing page or calculator** (links in 8.10), and record the region, plan, currency, date checked, usage assumptions and taxes in the *Vendor Pricing Log* sheet. A quotation containing an unverified price is not approved.

## 8.1 What goes into a project cost

| # | Cost item | Type | How it is priced | Official source of truth |
|---|---|---|---|---|
| 1 | Discovery and planning | One-time, labour | Hours x role rate | Internal rate card |
| 2 | UI/UX design | One-time, labour | Hours x rate (screens, components, prototype, accessibility) | Internal rate card |
| 3 | Frontend labour | One-time, labour | Hours x rate (pages, components, states, responsive) | Internal rate card |
| 4 | Backend labour | One-time, labour | Hours x rate (endpoints, rules, auth, jobs) | Internal rate card |
| 5 | Database work | One-time, labour | Hours x rate (design, migrations, tuning) | Internal rate card |
| 6 | Integrations | One-time, labour | Hours x rate per integration (payment, e-mail, CRM, SMS, analytics) | Internal rate card + vendor docs |
| 7 | Testing / QA | One-time, labour | Hours x rate (plan, automation, UAT support) | Internal rate card |
| 8 | Security review | One-time, labour | Hours x rate + scan tools | Internal rate card |
| 9 | Project management | One-time, labour | About 10-15% of build hours | Internal rate card |
| 10 | Software licences (one-time) | One-time, third party | Vendor quote | Vendor pricing page |
| 11 | Contingency | One-time, reserve | 10-20% of subtotal by risk | Policy |
| 12 | Hosting / compute | Recurring | Calculator, per month | Cloud/vendor calculator |
| 13 | Domain and DNS | Recurring (yearly) | Registrar price / 12 for monthly view | Registrar page |
| 14 | SSL/TLS where chargeable | Recurring (yearly) | Often free (managed or Let's Encrypt); paid for EV/OV or special cases | CA / platform page |
| 15 | E-mail services | Recurring | Per message or per mailbox | Provider page |
| 16 | Storage and backups | Recurring | Per GB-month + requests | Cloud calculator |
| 17 | Bandwidth / CDN | Recurring | Per GB transferred | CDN/cloud calculator |
| 18 | Monitoring and logging | Recurring | Per host/check/GB | Vendor page |
| 19 | Maintenance | Recurring, labour | Hours/month x rate | Internal rate card |
| 20 | Support | Recurring, labour | Hours/month x rate or SLA tier | Internal rate card |
| 21 | Software licences (recurring) | Recurring | Per seat or per month | Vendor pricing page |

## 8.2 Formulas

```text
Labour cost            = sum(hours x internal cost rate per role)
Subtotal               = Labour + one-time third-party costs
Contingency            = Subtotal x contingency %
Total delivery cost    = Subtotal + Contingency
Selling price          = Total delivery cost / (1 - target gross margin %)
Gross profit           = Selling price - Total delivery cost
Gross margin %         = Gross profit / Selling price
Markup %               = Gross profit / Total delivery cost        (NOT the same as margin)
Variable costs         = one-time third-party costs + commissions + bought-in contractor cost
Contribution margin    = Selling price - Variable costs            (what the project adds toward salaries and overhead)
Break-even (projects)  = Annual fixed costs / Contribution margin per project
Break-even (hours)     = hours at which gross profit reaches zero
Monthly operating cost = (vendor pass-through + service labour) x (1 + service contingency)
Annual operating cost  = Monthly operating cost x 12
Monthly service fee    = Monthly operating cost / (1 - service margin %)
MRR = sum of monthly fees      ARR = MRR x 12
Renewal fee            = max(current fee, next-year cost / (1 - service margin %))
Cost per user          = Monthly cloud cost / Active users
```

!!! note "Margin is not markup"
    A 40% **margin** requires dividing cost by 0.60 (a 66.7% **markup**). Multiplying cost by 1.40 gives only a 28.6% margin. Always say which one you mean in quotes and reports.

## 8.3 Rate card and assumptions (all placeholders)

{{costing: ratecard}}

{{costing: assumptions}}

!!! tip "How to set the real rate card"
    Internal cost per hour = (annual salary + employer costs + share of overhead) / productive hours per year (typically 70-75% of paid hours). Finance owns the rate card; developers never quote from personal rates.

## 8.4 Effort estimate (hours) for the three example projects

| Size | Example scope |
|---|---|
| **Small** | Corporate website - about 8 pages, CMS-lite, contact form, SEO basics |
| **Medium** | Booking / customer portal - login, roles, database, payment gateway, admin screens |
| **Enterprise** | Multi-tenant SaaS - roles, billing, dashboards, integrations, audit trail, high availability |

{{costing: hours}}

## 8.5 Project cost and selling price

{{costing: project}}

### Hand-calculated check - Small project

Every formula is checked by hand; compare this block with the table above.

{{costing: worked}}

## 8.6 Contribution margin and break-even

{{costing: margins}}

!!! note "Why contribution margin looks very high here"
    Our developers are salaried, so their pay is a **fixed** cost - it is paid whether or not this project exists. Contribution margin therefore excludes labour; it shows how much each project contributes toward salaries and overhead. If the work were bought from freelancers, their cost would be **variable** and the contribution margin would fall to about the gross margin. The break-even figure answers: *how many projects of this size per year cover our fixed costs?*

## 8.7 Monthly and annual operating cost, service fee and renewal

{{costing: ops}}

**Renewal pricing rule.** Renew at the next-year cost plus the service margin, using the vendors' published price changes (check at renewal time) and the company's wage plan. Give the client **60 days'** notice of the renewal price. Never reduce a service fee below the cost of delivering the agreed SLA. Offer a multi-year discount only if the margin stays at or above the policy minimum.

## 8.8 Summary of the three examples

{{costing: summary}}

{{diagram: cost_model | Figure 8.1 - From cost to price (schematic)}}

## 8.9 Scale scenario template (cloud cost per user)

For products with usage-based costs, estimate at four scales using the provider calculator. Fill the table from the **official calculator** - never from memory - and record region, date and assumptions.

| Active users | Compute | Database | Storage | Network / CDN | Monitoring | Total / month | Cost per active user |
|---|---|---|---|---|---|---|---|
| 100 | *fill from calculator* | | | | | | |
| 1,000 | | | | | | | |
| 10,000 | | | | | | | |
| 100,000 | | | | | | | |

Questions to answer for each row: does the architecture change at this scale (cache, read replica, queue, autoscaling)? What is the new risk? Does the price per user still support our margin?

## 8.10 Official pricing sources to verify before quoting

| Cost | Official page or calculator (check the date and region) |
|---|---|
| AWS | `https://calculator.aws/` and `https://aws.amazon.com/pricing/` |
| Microsoft Azure | `https://azure.microsoft.com/pricing/calculator/` |
| Google Cloud | `https://cloud.google.com/products/calculator` |
| Cloudflare | `https://www.cloudflare.com/plans/` |
| Vercel / Netlify | `https://vercel.com/pricing` - `https://www.netlify.com/pricing/` |
| GitHub | `https://github.com/pricing` |
| Docker | `https://www.docker.com/pricing/` |
| Domains / registrar | The chosen registrar's price list (renewal price differs from first-year price) |
| E-mail providers | The chosen provider's pricing page |
| Payment gateways | The gateway's official fee page for the client's country and payment methods |
| Taxes | Local tax authority guidance (for example GST/VAT); quote tax separately |

**Verification rule.** Each price in a quote has a row in the Vendor Pricing Log with: vendor, item, plan, region, currency, unit price, usage assumption, URL, date checked, checked by. A reviewer re-opens at least 20% of the links before the quote is approved. Prices older than 30 days are re-checked.

## 8.11 Expense classification (put it in every contract)

| Type | Meaning |
|---|---|
| **Customer-billed cloud** | Client's own cloud account, paid directly to the provider (preferred) |
| **Company-managed cloud** | Our account; re-billed with a stated management fee |
| **Pass-through third party** | Licences and subscriptions billed at cost |
| **Service fee** | Our labour and expertise |

Prefer the **client owning their cloud, domain and repository accounts**, with us holding limited, individual access. Define additional-usage rates and caps. Avoid "unlimited" support or usage unless it is priced in.

## 8.12 Commercial rules

- Quote **in writing** with scope, assumptions, exclusions, validity (for example 30 days), taxes, payment schedule and change-request rates.
- Typical payment milestones: advance on SOW signature; on design approval; on staging delivery; on go-live; retention until the end of the warranty. Milestones are tied to **accepted deliverables**.
- A quote includes **contingency**; it is not a hidden discount pool. Unused contingency is profit; overruns are managed through change control.
- Do not discount the price without removing scope. Discount only with approval from the authorised commercial representative.
- Estimates for fixed-price work must be reviewed by the Technical Lead; the project manager owns the budget tracking.
- Track actual hours against the estimate weekly; report variance above 10% to the PM and TL at once.
