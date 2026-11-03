"""App-store fee section (verified vs reported, clearly separated) and rollout plan."""
import calc
from calc import inr
from sections_a import table, callout

def store_fees():
    h = "<p>Applies only if the product is also distributed as an iOS or Android app and sells digital goods or subscriptions inside the app. A browser-based B2B SaaS billed through your own gateway has no store commission, but check each store's current rules before deciding the billing channel. Fees below were checked on <b>2026-11-03</b>.</p>"
    h += callout("ok", "Apple – verified from official Apple pages", "Fetched directly on 2026-11-03 from <a href='https://developer.apple.com/programs/whats-included/'>developer.apple.com/programs/whats-included</a> and <a href='https://developer.apple.com/app-store/small-business-program/'>developer.apple.com/app-store/small-business-program</a>.")
    h += table(["Item","Official wording / value","Notes"],[
     ("Developer Program fee","\"99 USD per membership year, or in local currency where available\"","The page gives no INR amount; read the local price at enrolment. Fee waiver exists for eligible nonprofits, accredited educational institutions and government entities."),
     ("Standard commission","30% on sale of digital goods and services","Page states 30%."),
     ("Small Business Program","15% on paid apps and In-App Purchases","Eligible if proceeds ≤ USD 1 million in the prior calendar year (all associated accounts) and ≤ USD 1 million in the current year; above that the standard rate applies to future sales; can re-qualify the following year. Rate change applies 15 days after the end of the fiscal month of approval."),
     ("Qualifying subscriptions","15%","Page lists 15% for qualifying subscriptions."),
     ("10% rate","\"further reduced commission of 10%\" for EU alternative-terms developers in the Small Business Program and for subscriptions after their first year","The sentence is ambiguous about who gets 10%; confirm on the page before modelling it."),
     ("Regional variations","Different rates/fees may apply in Brazil, EU, Japan, Netherlands, Russia, South Korea","India is not in that list on the page checked.")]) 
    h += callout("warn", "Google Play – NOT directly verified", "Google's help pages (support.google.com, play.google.com) could not be opened from this build environment (network block). The figures below come from web-search summaries of Google's own pages and <b>must be confirmed on the linked page before use</b>. Google has changed its fee model in 2026 and the sources below describe it differently by region and date.")
    h += table(["Item","Reported (search summary, 2026-11-03)","Verify at"],[
     ("Developer registration","One-time USD 25 (reported on Google's EEA access terms page)","<a href='https://support.google.com/googleplay/android-developer/answer/14659200'>Play Console Help</a> / Play Console sign-up page"),
     ("Older structure","15% on the first USD 1M of annual earnings, 30% above; 15% on auto-renewing subscriptions","<a href='https://support.google.com/googleplay/android-developer/answer/112622'>Service fees</a>"),
     ("2026 structure","Fee depends on whether the user's install is 'new' or 'existing' relative to a regional start date; first USD 1M at 10%; auto-renewing subscriptions 10%; other in-app purchases reported at 20% (new) / 25% (existing); billing fee reported at 5% where Google Play Billing is used (US/UK/EEA)","<a href='https://support.google.com/googleplay/android-developer/answer/16954621'>Understanding Google Play's lower service fees</a>; <a href='https://android-developers.googleblog.com/2026/06/play-expanded-billing.html'>Android Developers Blog</a>"),
     ("Reported start dates","30 Jun 2026: EEA, UK, US · 30 Sep 2026: Australia, Japan · 31 Dec 2026: South Korea · 30 Sep 2027: rest of world","Same page. <b>If correct, India (rest of world) would still be on the older structure until the 2027 date</b> – confirm."),
     ("Programs","Apps Experience / Games Level Up programs reported to give lower rates from 30 Sep 2026 in AU, EEA, JP, UK, US","Play Console Help")])
    P = calc.pricing(); rows = []
    for n, price, users, gb, api, sup in calc.PLANS:
        c = P["ct"][n]; base = c["var"] + c["support"] + c["leak"]
        def net(rate): return price - base - price*rate
        rows.append((n, inr(price), inr(c["contrib"]), inr(net(.30)), inr(net(.15)), f"{(c['contrib']-net(.15))/price:.0%} lower at 15% vs web"))
    h += "<h3>Effect on the sample plans (illustrative)</h3><p>Contribution if the same plan were sold in-app, replacing the gateway fee with a store commission on the full price. Uses the same variable-cost assumptions as the financial model; ignores tax handling differences, which vary by store and country.</p>" + \
         table(["Plan","Price","Contribution via own gateway","In-app at 30%","In-app at 15%","Impact"], rows, "num")
    h += "<h3>Decision rules</h3><ol><li>Prefer web billing for B2B subscriptions where the store's rules allow it; confirm the current rules for your category.</li><li>If in-app sales are required, price the in-app plan so contribution stays ≥ the web plan's margin target, or accept a lower margin knowingly.</li><li>Enrol in Apple's Small Business Program as soon as eligible; check Google programs for your region.</li><li>Budget fixed store costs: Apple USD 99/year (local price at enrolment) and Google USD 25 one-time (to confirm). Add them to the one-time/annual cost lines when mobile apps are in scope.</li><li>Re-verify all store fees at every G7 and G8 review.</li></ol>"
    return h

def rollout():
    rows = [("Days 1–30","Foundation","Fill intake; set up company GitHub org, registrar, password manager, MFA everywhere; adopt templates; create dashboard Project 0 backlog; start Stage 1 for first product","Owner, Tech Lead"),
            ("Days 31–60","First product to G3","Complete G1–G3; choose stack; start Stage 4 designs; build cost model with real provider prices","Product Owner, Tech Lead, Designer"),
            ("Days 61–90","Build and cost","G4–G5; weekly demos; start G6; approve cost and pricing (G7–G8); line up 3 pilot customers","All"),
            ("Days 91–150","Launch","Finish G6; security review; G9 deploy; pilots live; maintenance agreement (G11)","DevOps, QA, Support"),
            ("Days 151+","Sell and operate","G10 commercial approvals; monthly maintenance report; review CAC, churn, margin; iterate roadmap through change control","Sales, Support, Owner")]
    return table(["When","Focus","Key actions","Owners"], rows) + "<p class='note'>Durations are planning guides for a small team and should be recalibrated from your own timesheets after the first project.</p>"
