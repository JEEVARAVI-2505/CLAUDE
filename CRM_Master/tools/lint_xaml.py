#!/usr/bin/env python3
"""
Static checks for the WPF app (it cannot be launched on the Linux build box, so catch runtime-only mistakes here):
  1. every {StaticResource K} / {DynamicResource K} resolves to an x:Key (or implicit style type) in the app resources or the same file;
  2. Light.xaml and Dark.xaml define identical keys;
  3. every top-level {Binding Path} in a view (outside templates) names a real property / command of the view's view-model;
  4. {Binding DataContext.X, RelativeSource AncestorType=...} also resolves X against the view-model;
  5. every view / window has its code-behind and x:Class matches; every VM referenced in ViewTemplates exists.
Exit code 1 when problems are found.
"""
import re, sys, glob, os
import xml.etree.ElementTree as ET

APP = os.path.join(os.path.dirname(__file__), '..', 'CRM_Master.App')
XNS = '{http://schemas.microsoft.com/winfx/2006/xaml}'
problems = []

def local(tag): return tag.split('}')[-1]

# ---------- collect resource keys ----------
keys = set()
def collect_keys(path):
    tree = ET.parse(path)
    for el in tree.iter():
        k = el.get(XNS + 'Key')
        if k: keys.add(k)
        if local(el.tag) == 'Style' and el.get('TargetType') and not el.get(XNS + 'Key'):
            keys.add('{x:Type ' + el.get('TargetType').replace('{x:Type ', '').replace('}', '') + '}')
for f in glob.glob(APP + '/Themes/*.xaml') + [APP + '/App.xaml', APP + '/Views/ViewTemplates.xaml']:
    collect_keys(f)

# ---------- theme parity ----------
def keys_of(p): return {e.get(XNS + 'Key') for e in ET.parse(p).iter() if e.get(XNS + 'Key')}
if keys_of(APP + '/Themes/Light.xaml') != keys_of(APP + '/Themes/Dark.xaml'):
    problems.append('Light.xaml and Dark.xaml define different keys')

# ---------- view-model members ----------
def vm_members(name):
    path = os.path.join(APP, 'ViewModels', name + '.cs')
    if not os.path.exists(path): return None
    src = open(path).read()
    m = set(re.findall(r'public\s+(?:static\s+)?[\w<>\[\],\.\? ]+?\s+(\w+)\s*(?:\{|=>)', src))
    m |= {n[0].upper() + n[1:] for n in re.findall(r'\[ObservableProperty\][^;]*?\s_?(\w+)\s*(?:=|;)', src)}
    for meth in re.findall(r'\[RelayCommand[^\]]*\][^(]*?\s(\w+)\s*\(', src):
        m.add(re.sub(r'Async$', '', meth) + 'Command')
    for n in re.findall(r'public\s+(?:readonly\s+)?[\w<>\.\?, ]+\s+(\w+)\s*;', src): m.add(n)
    base = os.path.join(APP, 'ViewModels', 'ViewModelBase.cs')
    m |= {'IsBusy', 'StatusMessage'} if 'ViewModelBase' in src else set()
    return m

# ---------- view -> VM mapping from ViewTemplates ----------
view_vm = {}
for dt in ET.parse(APP + '/Views/ViewTemplates.xaml').iter():
    if local(dt.tag) == 'DataTemplate':
        t = dt.get('DataType', ''); vm = re.search(r'vm:(\w+)', t)
        child = list(dt)[0] if len(list(dt)) else None
        if vm and child is not None: view_vm[local(child.tag)] = vm.group(1)
extra = {'ContactEditWindow': 'ContactEditViewModel', 'FirstRunWindow': 'FirstRunViewModel', 'MainWindow': 'MainViewModel', 'RuleEditWindow': 'RuleEditViewModel'}
view_vm.update(extra)

RES = re.compile(r'\{(Static|Dynamic)Resource\s+([^}]+)\}')
BIND = re.compile(r'\{Binding\s*([^}]*)\}')
TEMPLATE_TAGS = {'DataTemplate', 'ControlTemplate', 'HierarchicalDataTemplate', 'Style', 'ItemsPanelTemplate'}

def check_view(path):
    name = os.path.basename(path).replace('.xaml', '')
    src = open(path).read()
    # resources
    own = set()
    tree = ET.parse(path)
    for el in tree.iter():
        if el.get(XNS + 'Key'): own.add(el.get(XNS + 'Key'))
    for kind, key in RES.findall(src):
        key = key.strip()
        if key.startswith('{x:Type'): key = key
        if key not in keys and key not in own and not key.startswith('{x:Type'):
            problems.append(f'{name}: {kind}Resource "{key}" not found')
    # code-behind
    cb = path + '.cs'
    root = tree.getroot(); xc = root.get(XNS + 'Class')
    if xc and not os.path.exists(cb): problems.append(f'{name}: missing code-behind {name}.xaml.cs')
    if xc and os.path.exists(cb):
        cls = xc.split('.')[-1]
        if f'partial class {cls}' not in open(cb).read(): problems.append(f'{name}: code-behind does not declare partial class {cls}')
    # bindings
    vmname = view_vm.get(name)
    if vmname is None: return
    members = vm_members(vmname)
    if members is None: problems.append(f'{name}: view-model {vmname} not found'); return
    def walk(el, in_template):
        tpl = in_template or local(el.tag).endswith('Column') or local(el.tag) == 'DataGrid.Columns' or local(el.tag) in TEMPLATE_TAGS or local(el.tag).endswith('Template') or 'Template' in local(el.tag).split('.')[-1] or local(el.tag).split('.')[-1] in ('ItemTemplate', 'CellTemplate', 'RowStyle', 'Triggers', 'ContextMenu', 'ItemContainerStyle')
        for attr, val in el.attrib.items():
            for b in BIND.findall(val):
                b = b.strip(); first = b.split(',')[0].strip()
                if first.startswith(('Path=',)): first = first[5:]
                if 'RelativeSource' in b or 'ElementName' in b or 'Source=' in b:
                    mm = re.match(r'(?:Path=)?DataContext\.(\w+)', first)
                    if mm and 'AncestorType' in b and mm.group(1) not in members:
                        if not tpl or True: problems.append(f'{name}: DataContext.{mm.group(1)} is not a member of {vmname}')
                    continue
                if tpl: continue
                seg = re.match(r'(?:Path=)?([A-Za-z_]\w*)', first)
                if seg and seg.group(1) not in members and first not in ('', '.'):
                    problems.append(f'{name}: binding "{seg.group(1)}" is not a member of {vmname}')
        for c in el: walk(c, tpl)
    walk(root, False)

for f in sorted(glob.glob(APP + '/Views/*.xaml')):
    if f.endswith('ViewTemplates.xaml'): continue
    check_view(f)
for f in [APP + '/App.xaml'] + glob.glob(APP + '/Themes/Styles*.xaml'):
    src = open(f).read(); own = keys_of(f)
    for kind, key in RES.findall(src):
        key = key.strip()
        if key not in keys and key not in own and not key.startswith('{x:Type'): problems.append(f'{os.path.basename(f)}: {kind}Resource "{key}" not found')

# ---------- every VM in ViewTemplates exists ----------
for v, vm in view_vm.items():
    if not os.path.exists(os.path.join(APP, 'ViewModels', vm + '.cs')): problems.append(f'view {v}: VM {vm} missing')

if problems:
    print(f'{len(problems)} problem(s):'); [print(' -', p) for p in sorted(set(problems))]; sys.exit(1)
print('XAML lint passed:', len(glob.glob(APP + "/Views/*.xaml")), 'views checked;', len(keys), 'resource keys known')
