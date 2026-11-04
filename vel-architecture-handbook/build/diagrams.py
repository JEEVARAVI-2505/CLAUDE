from lib import Svg, NAVY, MAG, GOLD

def three_tier():
    s = Svg(760, 330, 'Three-tier application architecture')
    s.person(70, 40, 'Customer', 'browser / phone')
    s.zone(150, 20, 590, 290, 'Network boundary: Internet-facing and private zones')
    s.box(180, 70, 170, 90, 'PRESENTATION TIER\nWeb UI (React SPA)\nruns in the browser', fill='#eef2fb')
    s.box(400, 70, 170, 90, 'APPLICATION TIER\nBusiness logic + REST API\n(runs on app servers)', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.db(620, 70, 100, 90, 'DATA TIER\nSQL database')
    s.arrow(105, 70, 180, 110, 'HTTPS')
    s.arrow(350, 100, 400, 100, 'JSON/REST'); s.arrow(570, 100, 620, 100, 'SQL')
    s.label(265, 190, 'Shows data, collects input', 10.5); s.label(485, 190, 'Rules, validation, security checks', 10.5); s.label(670, 190, 'Stores data safely', 10.5)
    s.box(180, 230, 540, 54, 'Rule: each tier talks only to its neighbour. The browser never talks to the database directly.', fill='#fff4e8', stroke=MAG, color='#7a1040', weight=500, size=11.5)
    return s.render()

def c4_context():
    s = Svg(760, 360, 'C4 Level 1 system context diagram (GreenLeaf Organics, fictional)')
    s.person(90, 50, 'Customer', '[Person]'); s.person(90, 240, 'Store Staff', '[Person]')
    s.box(290, 120, 190, 100, 'GreenLeaf Online\nOrdering System\n[Software System]\nTakes and tracks orders', fill=NAVY, color='#fff', stroke=NAVY)
    s.box(590, 30, 150, 70, 'Payment Gateway\n[External System]', fill='#eceff4', stroke='#6b7390', color='#333c58')
    s.box(590, 145, 150, 70, 'Email / SMS Service\n[External System]', fill='#eceff4', stroke='#6b7390', color='#333c58')
    s.box(590, 260, 150, 70, 'Accounting Package\n[Existing System]', fill='#eceff4', stroke='#6b7390', color='#333c58')
    s.arrow(130, 70, 290, 150, 'Places orders', dy=-10); s.arrow(130, 260, 290, 200, 'Manages stock\nand orders', dy=14)
    s.arrow(480, 140, 590, 70, 'Takes payment', dy=-8); s.arrow(480, 170, 590, 180, 'Sends notices', dy=-8); s.arrow(480, 205, 590, 285, 'Exports invoices', dy=14)
    s.label(380, 345, 'Key: person = navy figure · focus system = navy box · external systems = grey boxes', 10.5)
    return s.render()

def c4_container():
    s = Svg(760, 400, 'C4 Level 2 container diagram')
    s.person(60, 40, 'Customer'); 
    s.zone(130, 15, 620, 370, 'GreenLeaf Online Ordering System (system boundary)', stroke=NAVY)
    s.box(160, 60, 150, 70, 'Web App (SPA)\n[React]\nStorefront UI', fill='#eef2fb')
    s.box(350, 60, 150, 70, 'API Application\n[.NET / Node]\nBusiness rules, REST', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.db(560, 55, 110, 80, 'Orders DB\n[PostgreSQL]')
    s.box(350, 190, 150, 60, 'Background Worker\n[Queue consumer]', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.box(160, 190, 150, 60, 'Message Queue\n[Managed broker]', fill='#f3edfa', stroke='#6b3fa0', color='#4a2a78')
    s.box(560, 190, 150, 60, 'Object Storage\n[Product images]', fill='#fff4dc', stroke=GOLD, color='#6b4a00')
    s.box(160, 300, 550, 55, 'Observability: logs · metrics · traces · alerts', fill='#fff4e8', stroke=MAG, color='#7a1040', weight=500)
    s.arrow(100, 65, 160, 90, 'HTTPS', dy=-10); s.arrow(310, 95, 350, 95, 'JSON/HTTPS', dy=-8); s.arrow(500, 95, 560, 95, 'SQL/TLS', dy=-8)
    s.arrow(425, 130, 310, 205, 'publishes events', dy=-4, dx=-20); s.arrow(310, 220, 350, 220, 'consume'); s.arrow(500, 215, 560, 215, 'reads/writes')
    return s.render()

def c4_component():
    s = Svg(760, 330, 'C4 Level 3 component diagram of the API Application')
    s.zone(20, 15, 720, 300, 'API Application [container]', stroke=NAVY)
    s.box(40, 60, 130, 60, 'Orders Controller\n[REST endpoints]', fill='#eef2fb')
    s.box(40, 160, 130, 60, 'Catalogue Controller\n[REST endpoints]', fill='#eef2fb')
    s.box(250, 60, 140, 60, 'Order Service\n[rules, pricing]', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.box(250, 160, 140, 60, 'Catalogue Service\n[search, stock]', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.box(470, 60, 130, 60, 'Order Repository\n[data access]', fill='#fff4dc', stroke=GOLD, color='#6b4a00')
    s.box(470, 160, 130, 60, 'Payment Adapter\n[gateway client]', fill='#f3edfa', stroke='#6b3fa0', color='#4a2a78')
    s.box(40, 250, 560, 45, 'Cross-cutting: authentication · validation · logging · error handling', fill='#fff4e8', stroke=MAG, color='#7a1040', weight=500)
    s.db(645, 55, 85, 70, 'Orders DB'); s.box(630, 165, 100, 50, 'Payment\nGateway', fill='#eceff4', stroke='#6b7390', color='#333c58')
    for y in (90, 190): s.arrow(170, y, 250, y)
    s.arrow(390, 90, 470, 90); s.arrow(390, 190, 470, 190); s.arrow(600, 90, 645, 90); s.arrow(600, 190, 630, 190)
    return s.render()

def deployment():
    s = Svg(760, 430, 'Cloud deployment diagram: two availability zones')
    s.person(50, 30, 'Users'); s.box(120, 35, 120, 50, 'DNS + CDN + WAF', fill='#f3edfa', stroke='#6b3fa0', color='#4a2a78')
    s.zone(20, 120, 720, 295, 'Cloud region (one VPC / virtual network)', stroke=NAVY)
    s.box(280, 135, 200, 40, 'Load balancer (public subnet)', fill='#eef2fb')
    for i, x in enumerate((60, 410)):
        s.zone(x, 195, 290, 205, f'Availability zone {"AB"[i]}', stroke='#6b7390')
        s.box(x + 20, 220, 120, 55, f'App server {i+1}\n(private subnet)', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
        s.box(x + 155, 220, 115, 55, 'Worker\n(private subnet)', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
        s.db(x + 20, 305, 110, 70, 'Primary DB' if i == 0 else 'Standby DB')
        s.box(x + 155, 315, 115, 50, 'Monitoring\nagent', fill='#fff4e8', stroke=MAG, color='#7a1040')
        s.arrow(380 if i == 0 else 380, 175, x + 80, 220)
    s.arrow(350, 330, 410, 340, 'replication', both=True, dy=-6)
    s.arrow(240, 60, 330, 135, 'HTTPS'); s.arrow(85, 70, 120, 62)
    s.label(380, 424, 'Backups → separate-region storage · Secrets in a managed vault · Infrastructure defined as code', 10.5)
    return s.render()

def dataflow():
    s = Svg(760, 300, 'Data flow diagram for placing an order with trust boundaries')
    s.zone(10, 10, 130, 280, 'UNTRUSTED', stroke='#b3261e')
    s.zone(150, 10, 330, 280, 'APPLICATION ZONE (authenticated)', stroke=MAG)
    s.zone(490, 10, 260, 280, 'DATA ZONE (private)', stroke=NAVY)
    s.box(25, 110, 100, 60, 'Customer\nbrowser', fill='#fdecea', stroke='#b3261e', color='#7a1a14')
    s.box(165, 60, 130, 55, '1 Validate &\nauthenticate', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.box(165, 170, 130, 55, '2 Price & create\norder', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.box(335, 115, 130, 55, '3 Call payment\ngateway', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.db(520, 50, 100, 70, 'D1 Orders'); s.db(640, 50, 95, 70, 'D2 Customers')
    s.db(520, 180, 100, 70, 'D3 Audit log')
    s.arrow(125, 135, 165, 95, 'order + token'); s.arrow(230, 115, 230, 170, 'valid request', dx=40, dy=0)
    s.arrow(295, 200, 335, 160, 'amount'); s.arrow(295, 190, 520, 100, 'write order', dy=-18, dx=20)
    s.arrow(295, 215, 520, 220, 'log event', dy=-6)
    s.label(380, 280, 'Every arrow crossing a dashed boundary needs authentication, encryption and validation.', 10.5)
    return s.render()

def lifecycle():
    names = ['01 Lead','02 Discovery','03 Current state','04 Problem','05 Requirements','06 Feasibility','07 Alternatives','08 Target arch.','09 Documentation','10 Cost & risk','11 Presentation','12 Approval','13 Handover','14 Governance','15 Post-impl. review']
    s = Svg(760, 330, 'Fifteen-stage consulting lifecycle')
    cols = [('#eef2fb', NAVY, 'A. UNDERSTAND', 0, 5), ('#e8f5ee', '#1f7a4d', 'B. DESIGN & DECIDE', 5, 10), ('#fff4e8', MAG, 'C. DELIVER & LEARN', 10, 15)]
    for fill, st, title, a, b in cols:
        y = 20 + (a // 5) * 105
        s.label(20, y - 2, title, 11, st, 700, 'start')
        for k in range(a, b):
            x = 20 + (k - a) * 144
            s.box(x, y + 6, 128, 46, names[k], fill=fill, stroke=st, color='#1b2540', size=11.5)
            if k < b - 1: s.arrow(x + 128, y + 29, x + 144, y + 29)
    s.label(380, 322, 'Gates: no stage starts until the previous stage is approved. Stage 12 is the formal architecture approval.', 10.5)
    return s.render()

def mono_vs_micro():
    s = Svg(760, 310, 'Monolith versus microservices')
    s.zone(10, 10, 350, 290, 'MODULAR MONOLITH: one deployable unit', stroke=NAVY)
    for i, n in enumerate(['Orders', 'Catalogue', 'Billing', 'Users']):
        s.box(35 + (i % 2) * 160, 50 + (i // 2) * 70, 140, 55, n + ' module', fill='#eef2fb')
    s.db(135, 200, 110, 60, 'ONE database'); s.label(190, 285, 'Simple to build, test, deploy · scale as a whole', 10.5)
    s.zone(400, 10, 350, 290, 'MICROSERVICES: many deployable units', stroke=MAG)
    for i, n in enumerate(['Orders', 'Catalogue', 'Billing', 'Users']):
        x = 415 + (i % 2) * 165; y = 45 + (i // 2) * 120
        s.box(x, y, 100, 42, n + ' svc', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d', size=11.5)
        s.db(x + 5, y + 50, 90, 38, 'own DB')
    s.arrow(515, 66, 580, 66, '', both=True); s.arrow(465, 100, 465, 165, '', dash=True)
    s.label(575, 285, 'Independent scaling & teams · network, data & ops complexity', 10.5)
    return s.render()

def hexagonal():
    s = Svg(760, 300, 'Hexagonal architecture: ports and adapters')
    s.zone(230, 25, 300, 250, 'APPLICATION CORE (no framework code)', stroke='#1f7a4d')
    s.box(280, 80, 200, 130, 'Domain model\n+ use cases\n(business rules)', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d', size=13)
    s.box(20, 60, 150, 50, 'REST controller\n(adapter)', fill='#eef2fb'); s.box(20, 180, 150, 50, 'Message consumer\n(adapter)', fill='#eef2fb')
    s.box(590, 60, 150, 50, 'SQL repository\n(adapter)', fill='#fff4dc', stroke=GOLD, color='#6b4a00'); s.box(590, 180, 150, 50, 'Payment client\n(adapter)', fill='#fff4dc', stroke=GOLD, color='#6b4a00')
    for y in (85, 205): s.arrow(170, y, 280, y + (10 if y == 85 else -10)); s.arrow(480, 100 if y == 85 else 190, 590, y)
    s.label(225, 130, 'inbound ports', 10); s.label(535, 140, 'outbound ports', 10)
    s.label(380, 292, 'Dependencies point inward: adapters depend on the core, never the reverse.', 10.5)
    return s.render()

def eventdriven():
    s = Svg(760, 240, 'Event-driven architecture')
    s.box(20, 90, 130, 60, 'Order Service\n(producer)', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.box(250, 80, 160, 80, 'Event broker\nOrderPlaced event', fill='#f3edfa', stroke='#6b3fa0', color='#4a2a78')
    for i, n in enumerate(['Inventory Service', 'Email Service', 'Analytics']):
        s.box(540, 20 + i * 75, 190, 50, n + ' (consumer)', fill='#eef2fb'); s.arrow(410, 120, 540, 45 + i * 75)
    s.arrow(150, 120, 250, 120, 'publish')
    s.label(380, 232, 'Producers do not know who consumes. Gains decoupling; costs eventual consistency and harder debugging.', 10.5)
    return s.render()

def network():
    s = Svg(760, 330, 'Network architecture and trust boundaries')
    s.box(20, 140, 100, 50, 'Internet', fill='#fdecea', stroke='#b3261e', color='#7a1a14')
    s.zone(150, 20, 590, 295, 'Virtual network 10.0.0.0/16', stroke=NAVY)
    s.zone(170, 50, 130, 245, 'Public subnet', stroke='#b3261e'); s.box(185, 90, 100, 55, 'Load balancer\n+ WAF', fill='#fdecea', stroke='#b3261e', color='#7a1a14'); s.box(185, 190, 100, 55, 'NAT / bastion\n(controlled)', fill='#fdecea', stroke='#b3261e', color='#7a1a14')
    s.zone(330, 50, 180, 245, 'Private app subnet', stroke=MAG); s.box(350, 90, 140, 55, 'App servers', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d'); s.box(350, 190, 140, 55, 'Workers', fill='#e8f5ee', stroke='#1f7a4d', color='#14532d')
    s.zone(540, 50, 185, 245, 'Isolated data subnet', stroke=NAVY); s.db(575, 100, 110, 70, 'Database'); s.box(560, 205, 140, 45, 'Backups (encrypted)', fill='#fff4dc', stroke=GOLD, color='#6b4a00')
    s.arrow(120, 165, 185, 118, 'TCP 443 only'); s.arrow(285, 118, 350, 118, 'app port'); s.arrow(490, 118, 575, 135, 'DB port, app SG only', dy=-10)
    return s.render()

def adr_lifecycle():
    s = Svg(760, 150, 'ADR status lifecycle')
    xs = [20, 190, 360, 530]
    for x, n, f in zip(xs, ['Proposed', 'Accepted', 'Deprecated', 'Superseded by ADR-nnn'], ['#fff4e8', '#e8f5ee', '#eceff4', '#eceff4']):
        s.box(x, 40, 150 if n != 'Superseded by ADR-nnn' else 200, 50, n, fill=f)
    for a, b in zip(xs[:3], xs[1:]): s.arrow(a + 150, 65, b, 65)
    s.label(380, 130, 'ADRs are immutable once accepted: change a decision by writing a NEW ADR that supersedes it.', 11)
    return s.render()

def roles_ladder():
    s = Svg(760, 280, 'Career and approval ladder')
    items = ['Junior Technical\nAnalyst', 'Business\nAnalyst', 'Solution /\nSoftware Architect', 'Cloud / Security\nArchitect', 'Enterprise\nArchitect', 'Principal\nArchitect']
    for i, n in enumerate(items):
        h = 50 + i * 28
        s.box(20 + i * 122, 250 - h - 10, 112, h, n, fill=['#eef2fb', '#eef2fb', '#e8f5ee', '#e8f5ee', '#fff4e8', '#fdecea'][i], size=10.5)
    s.label(380, 272, 'Authority to approve high-impact decisions grows to the right; juniors prepare, seniors approve.', 10.5)
    return s.render()
