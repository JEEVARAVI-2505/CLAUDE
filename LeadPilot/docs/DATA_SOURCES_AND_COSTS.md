# Data sources, limits and costs

LeadPilot AI never invents leads. Every lead stored by discovery has a source name, a source URL, a retrieval time and (per field) evidence.

| Source | Status in 1.0 | Cost | Notes |
|---|---|---|---|
| **OpenStreetMap** (Nominatim geocoding + Overpass) | **Built in, works with no key** | Free | Public servers allow light use only; the app waits ≥1.2 s between requests and retries politely. For heavy use run your own Overpass server and enter its address in API Integrations. ODbL: keep the attribution (stored automatically). Coverage and contact-data completeness vary a lot by city and category. |
| **Company websites** (public pages) | Built in | Free | `robots.txt` and `Crawl-delay` respected, 1 request/second per host, 2 MB page cap, no forms/logins/CAPTCHA solving, private-network addresses refused. Reads home, contact and about pages only. |
| **Excel / CSV files** | Built in | Free | Import Centre. Legacy `.xls` must be re-saved as `.xlsx`/CSV. |
| **Brave Search API** (optional, finds a company's official site) | Built in, needs *your* key | Free tier, then paid by Brave | Used only by AI Enrichment when a lead has no website. Results must match the company name and are not directories; otherwise nothing is applied. |
| **AI model** (optional) | Built in, off by default | Ollama: free, local. OpenAI-compatible: paid by your provider | Never required. AI text is stored as an *unapplied suggestion*; it never changes phone/e-mail/address. |
| Licensed B2B data providers, CRMs, ad-platform lead forms, website enquiry forms | **Not bundled** | Your own account/licence | Export a CSV/XLSX from the provider and use Import Centre. Map enquiry text to "Notes / enquiry text" so it is stored as buying-intent evidence. |
| Social-media profiles | **Not supported** | - | Deliberately: restricted/terms-protected. |
| E-mail / WhatsApp sending | **Drafts only** | - | The app opens your mail app / WhatsApp with the draft. It does not send, so it can never claim delivery. |

**Volume is not unlimited or guaranteed.** How many prospects you get depends on what the selected sources publish for your location and category, and on their rate limits.

## What the checks do and do not mean
* E-mail: *Valid format* → syntax only. *Domain exists / Mail server found* → DNS says the domain can receive mail. Neither proves the mailbox exists or belongs to a person. "Verified by provider" is reserved for a future authorised verification provider and is never set by this version.
* Phone: *Country identified* → the number is valid for that country's numbering plan. *Publicly listed* → found on a public page. Ownership is never claimed.
* Website: reachable / redirected / different domain / unavailable, from one HTTP request.
* Relevance score → how well the company matches your target market. It is **not** buying intent; intent only counts when evidence (e.g. an enquiry) is recorded.
