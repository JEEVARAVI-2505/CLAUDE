"""Generates the fictional sample datasets (seeded, so the files are reproducible).
All people, companies and domains are invented; e-mail domains use the reserved '.example' TLD. Phone numbers are random."""
import csv, random, os
random.seed(20251031)
first = "Aarav Vivaan Aditya Arjun Reyansh Krishna Ishaan Saanvi Aadhya Ananya Diya Myra Pari Anika Navya Rohan Kabir Dev Neel Vihaan Meera Riya Tara Kiran Sameer Priya Neha Rahul Amit Sunita Deepak Pooja Manoj Kavita Suresh Anjali Vikram Nisha".split()
last = "Sharma Verma Gupta Iyer Nair Reddy Patel Shah Mehta Kulkarni Joshi Desai Bose Banerjee Chatterjee Singh Kapoor Malhotra Menon Pillai Rao Naidu Khan Ahmed Fernandes DSouza Thomas Mathew".split()
companies = [("Acme Demo","acme-demo"),("Globex Sample","globex-demo"),("Initech Test","initech-demo"),("Umbrella Example","umbrella-demo"),("Stark Sample Industries","stark-demo"),
             ("Wayne Demo Enterprises","wayne-demo"),("Hooli Test Labs","hooli-demo"),("Vandelay Sample Imports","vandelay-demo"),("Pied Piper Demo","piedpiper-demo"),("Soylent Sample Foods","soylent-demo")]
suffix = ["Pvt Ltd","Private Limited","Ltd","LLP","Pvt. Ltd."]
titles = ["Marketing Manager","Sales Head","Founder","HR Executive","Operations Lead","Procurement Manager","Business Analyst","Account Executive","Director","Consultant"]
cities = [("Mumbai","Maharashtra"),("Pune","Maharashtra"),("Bengaluru","Karnataka"),("Chennai","Tamil Nadu"),("Hyderabad","Telangana"),("Delhi","Delhi"),("Kolkata","West Bengal"),("Ahmedabad","Gujarat"),("Kochi","Kerala"),("Jaipur","Rajasthan")]
sources = ["Website form","Referral","Trade show","LinkedIn campaign","Cold call","Webinar","Newsletter"]
statuses = ["Lead","Prospect","Customer","Inactive"]
SAMPLE_TAG = "Sample dataset"
header = ["First Name","Last Name","Email","Mobile","Alternate Phone","Company","Designation","Website","City","State","Country","Lead Source","Status","Created On","Notes"]

def phone(i):
    n = random.randint(6,9)*10**9 + random.randint(0,10**9-1)
    s = str(n); r = random.random()
    if r < .30: return s
    if r < .50: return f"{s[:5]} {s[5:]}"
    if r < .65: return f"+91 {s[:5]} {s[5:]}"
    if r < .75: return f"+91-{s}"
    if r < .83: return f"0{s}"
    if r < .90: return f"({s[:5]}) {s[5:]}"
    return f"91{s}"

def messy_name(w):
    r = random.random()
    return w.upper() if r < .15 else w.lower() if r < .27 else f"  {w}  " if r < .33 else w

rows = []
for i in range(1, 101):
    f, l = random.choice(first), random.choice(last)
    comp, dom = random.choice(companies); comp_full = f"{comp} {random.choice(suffix)}"
    city, state = random.choice(cities)
    email = f"{f}.{l}@{dom}.example".lower()
    r = random.random()
    if r < .06: email = ""                                           # missing
    elif r < .10: email = email.replace("@", "@@")                   # invalid
    elif r < .14: email = f"{f}{l}".lower() + "@gmial.com"           # typo'd provider
    elif r < .17: email = f"{f}.{l}at{dom}.example".lower()          # no @
    elif r < .27: email = email.upper() if random.random() < .5 else f"  {email}  "
    mob = phone(i)
    r = random.random()
    if r < .05: mob = ""
    elif r < .09: mob = "12345"
    elif r < .11: mob = "9999999999"
    country = random.choice(["India"]*7 + ["IN","india","India ","Indai","UAE","USA"])
    site = f"www.{dom}.example" if random.random() < .5 else (f"https://{dom}.example" if random.random() < .7 else "not a website")
    d = f"{random.randint(1,28):02d}/{random.randint(1,12):02d}/{random.randint(2022,2025)}"
    rows.append([messy_name(f), messy_name(l), email, mob, phone(i) if random.random() < .25 else "", comp_full if random.random() > .08 else "",
                 random.choice(titles) if random.random() > .15 else "", site if random.random() > .2 else "", city, state, country,
                 random.choice(sources), random.choice(statuses), d, SAMPLE_TAG if random.random() < .5 else "Fictional sample record"])

# deliberately planted problems (fixed positions so every demo shows the same findings)
for idx, col, val in [(1, 3, "12345"), (2, 3, "98-76"), (4, 2, "john..doe@acme-demo.example"), (6, 2, "priya@@globex-demo.example"), (9, 2, "amit.kumar@gmial.com"),
                      (10, 3, "9999999999"), (13, 10, "Indai"), (14, 5, ""), (15, 2, ""), (16, 2, "no-at-sign.example"), (18, 4, "9999999999"), (19, 13, "05/13/2099")]:
    rows[idx][col] = val
# deliberate duplicates: exact (different formatting), same email, same phone, spelling variants
base = rows[:]
dups = []
def dup(src, **chg):
    r = list(src)
    for k, v in chg.items(): r[header.index(k)] = v
    dups.append(r)
def reformat(m):
    d = "".join(ch for ch in m if ch.isdigit())[-10:]
    return f"+91 {d[:5]} {d[5:]}" if len(d) == 10 else m
for idx in (3, 17, 29, 41, 58):
    s = base[idx]; dup(s, **{"First Name": s[0].upper(), "Email": s[2].upper(), "Mobile": reformat(s[3])})   # same person, different formatting
for idx in (8, 22):
    s = base[idx]; dup(s, **{"First Name": s[0][:-1] if len(s[0].strip()) > 3 else s[0], "Mobile": phone(0), "Email": ""})                                                          # near-duplicate name only
for idx in (12, 36):
    s = base[idx]; dup(s, **{"First Name": "Different", "Last Name": "Person", "Email": s[2]})                                                                                      # same email, different name
rows += dups
random.shuffle(rows)
os.makedirs("CRM_Master.App/Assets/SampleData", exist_ok=True)
with open("CRM_Master.App/Assets/SampleData/sample-contacts.csv", "w", newline="", encoding="utf-8-sig") as fh:
    w = csv.writer(fh); w.writerow(header); w.writerows(rows)

# update file: changed city / company / title for 30 existing contacts (matched by e-mail) + 5 new people
good = [r for r in base if r[2] and "@@" not in r[2] and " " not in r[2].strip() and "@" in r[2]]
upd = [["Email","Designation","City","Company","Mobile","Lead Source"]]
for r in random.sample(good, 30):
    city, _ = random.choice(cities)
    upd.append([r[2].lower(), random.choice(titles), city, r[5] if random.random() < .5 else r[5].replace("Pvt Ltd","Private Limited"), phone(0) if random.random() < .4 else "", "Account review" if random.random() < .5 else ""])
for k in range(5):
    upd.append([f"new.contact{k+1}@newco-demo.example", random.choice(titles), "Mumbai", "NewCo Demo Pvt Ltd", phone(0), "Referral"])
with open("CRM_Master.App/Assets/SampleData/sample-update.csv", "w", newline="", encoding="utf-8-sig") as fh:
    csv.writer(fh).writerows(upd)
print(len(rows), "contacts,", len(upd)-1, "update rows")
