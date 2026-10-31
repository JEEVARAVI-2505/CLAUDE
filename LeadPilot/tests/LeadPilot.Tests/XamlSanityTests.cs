using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Windows.Input;
using LeadPilot.Application.ViewModels;

namespace LeadPilot.Tests;

/// <summary>
/// The WPF project can only be compiled on Windows, so this test statically checks every XAML file against the real view-model types:
/// well-formed XML, resource keys exist, every {Binding} path resolves, commands are ICommand, two-way bindings have setters,
/// converters get the argument types they expect, and every view has a code-behind with InitializeComponent.
/// </summary>
public class XamlSanityTests
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly Assembly VmAsm = typeof(ViewModelBase).Assembly;

    private static string Desktop()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "src", "LeadPilot.Desktop"))) d = d.Parent;
        return Path.Combine(d!.FullName, "src", "LeadPilot.Desktop");
    }

    private static readonly Dictionary<string, Type> SpecialRoots = new()
    {
        ["StatusBanner"] = typeof(ViewModelBase), ["MainWindow"] = typeof(MainViewModel)
    };

    private static Type? ResolveType(string name) => VmAsm.GetTypes().FirstOrDefault(t => t.Name == name && t.Namespace == "LeadPilot.Application.ViewModels")
                                                  ?? VmAsm.GetTypes().FirstOrDefault(t => t.Name == name);

    [Fact]
    public void Every_xaml_file_is_consistent_with_the_view_models()
    {
        var root = Desktop();
        var files = Directory.GetFiles(root, "*.xaml", SearchOption.AllDirectories);
        files.Length.Should().BeGreaterThan(20);
        var docs = files.ToDictionary(f => f, f => XDocument.Load(f, LoadOptions.SetLineInfo));
        var keys = new HashSet<string>(docs.Values.SelectMany(d => d.Descendants()).Select(e => (string?)e.Attribute(X + "Key")).Where(k => k != null)!);
        var converterClasses = Directory.GetFiles(Path.Combine(root, "Services"), "*.cs").SelectMany(f => Regex.Matches(File.ReadAllText(f), @"class (\w+)").Select(m => m.Groups[1].Value)).ToHashSet();
        var problems = new List<string>();

        foreach (var (file, doc) in docs)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var rel = Path.GetRelativePath(root, file);
            Type? pageVm = null;
            if (rel.StartsWith("Views", StringComparison.Ordinal) || name == "MainWindow")
            {
                var vmName = SpecialRoots.TryGetValue(name, out var sp) ? sp.Name : name.Replace("View", "") + "ViewModel";
                pageVm = ResolveType(vmName);
                if (pageVm == null) problems.Add($"{rel}: no view-model {vmName}");
                var cs = file + ".cs";
                if (!File.Exists(cs) && name != "MainWindow") problems.Add($"{rel}: missing code-behind");
                else if (File.Exists(cs) && !File.ReadAllText(cs).Contains("InitializeComponent")) problems.Add($"{rel}: code-behind lacks InitializeComponent()");
                var cls = (string?)doc.Root!.Attribute(X + "Class");
                if (cls == null || !cls.EndsWith("." + name, StringComparison.Ordinal)) problems.Add($"{rel}: x:Class does not match file name");
            }
            new Walker(rel, keys, converterClasses, problems).Walk(doc.Root!, pageVm, pageVm, name == "Styles" ? null : null);
        }
        problems.Should().BeEmpty("XAML problems:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Every_view_model_has_a_view_and_a_template()
    {
        var root = Desktop();
        var templates = File.ReadAllText(Path.Combine(root, "Themes", "Templates.xaml"));
        var pages = VmAsm.GetTypes().Where(t => t.IsSubclassOf(typeof(ViewModelBase)) && !t.IsAbstract).ToList();
        pages.Should().HaveCountGreaterThan(15);
        foreach (var p in pages)
        {
            var view = p.Name.Replace("ViewModel", "View");
            File.Exists(Path.Combine(root, "Views", view + ".xaml")).Should().BeTrue(p.Name + " needs " + view + ".xaml");
            templates.Should().Contain($"vm:{p.Name}", p.Name + " needs a DataTemplate in Templates.xaml");
        }
        var app = File.ReadAllText(Path.Combine(root, "App.xaml.cs"));
        foreach (var p in pages) app.Should().Contain("typeof(" + p.Name + ")", p.Name + " must be registered in the DI container");
    }

    private sealed class Walker
    {
        private readonly string _file; private readonly HashSet<string> _keys; private readonly HashSet<string> _converterClasses; private readonly List<string> _problems;
        private static readonly string[] SystemKeys = { "UiFont", "IconFont" };
        public Walker(string file, HashSet<string> keys, HashSet<string> converterClasses, List<string> problems) { _file = file; _keys = keys; _converterClasses = converterClasses; _problems = problems; }

        private void Problem(XElement e, string msg) => _problems.Add($"{_file}:{((System.Xml.IXmlLineInfo)e).LineNumber}: {msg}");

        public void Walk(XElement e, Type? ctx, Type? page, object? _)
        {
            var local = e.Name.LocalName;
            // context switches
            if (local == "DataTemplate" && (string?)e.Attribute("DataType") is { } dt)
            {
                var tn = Regex.Match(dt, @"x:Type\s+\w+:(\w+)").Groups[1].Value;
                var t = ResolveType(tn);
                if (t == null) { Problem(e, "unknown DataType " + dt); return; }
                if (_file.EndsWith("Templates.xaml", StringComparison.Ordinal) && t.IsSubclassOf(typeof(ViewModelBase))) return; // view mapping
                ctx = t;
            }
            if (local == "Style" && (string?)e.Attribute(X + "Key") == "ChatBubble") ctx = typeof(ChatMessage);

            Type? childCtx = ctx;
            foreach (var a in e.Attributes())
            {
                if (a.IsNamespaceDeclaration) continue;
                var v = a.Value.Trim();
                if (!v.StartsWith('{')) continue;
                var m = Parse(v);
                if (m == null) continue;
                switch (m.Value.Name)
                {
                    case "StaticResource" or "DynamicResource":
                        var k = m.Value.Positional.FirstOrDefault() ?? "";
                        if (k.StartsWith("x:Type", StringComparison.Ordinal) || k.StartsWith('{')) break;
                        if (!_keys.Contains(k) && !SystemKeys.Contains(k)) Problem(e, $"resource '{k}' is not defined");
                        break;
                    case "x:Static":
                        break;
                    case "Binding":
                        var itemType = CheckBinding(e, a, m.Value, ctx, page);
                        if (a.Name.LocalName == "ItemsSource") childCtx = itemType;
                        break;
                }
            }
            foreach (var c in e.Elements())
            {
                // property-elements like Setter.Value/ItemsControl.ItemTemplate inherit the item context; everything else inherits ctx
                Walk(c, childCtx, page, null);
            }
        }

        private Type? CheckBinding(XElement e, XAttribute attr, Markup m, Type? ctx, Type? page)
        {
            var path = m.Named.TryGetValue("Path", out var p) ? p : m.Positional.FirstOrDefault() ?? "";
            if (m.Named.ContainsKey("ElementName") || m.Named.ContainsKey("Source")) return null;
            var mode = m.Named.GetValueOrDefault("Mode", "");
            var start = ctx;
            if (m.Named.TryGetValue("RelativeSource", out var rs))
            {
                if (!rs.Contains("AncestorType=UserControl")) return null;
                if (!path.StartsWith("DataContext.", StringComparison.Ordinal)) { Problem(e, "RelativeSource binding must start with DataContext."); return null; }
                path = path["DataContext.".Length..]; start = page;
            }
            if (path is "" or ".") return start;
            if (start == null) { Problem(e, $"binding '{path}' has no data context"); return null; }
            var t = start; PropertyInfo? last = null;
            foreach (var seg in path.Split('.'))
            {
                var name = Regex.Replace(seg, @"\[.*\]", "");
                last = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (last == null) { Problem(e, $"'{name}' not found on {t.Name} (binding '{path}')"); return null; }
                t = last.PropertyType;
            }
            var attrName = attr.Name.LocalName; var el = e.Name.LocalName;
            if (attrName == "Command" || attrName == "InputBindings") { }
            if (attrName == "Command" && !typeof(ICommand).IsAssignableFrom(t)) Problem(e, $"'{path}' is {t.Name}, not an ICommand");
            if (attrName == "Command" && attr.Parent != null && el is "KeyBinding") { /* fine */ }

            var twoWayDefault = (el, attrName) is ("TextBox", "Text") or ("Run", "Text") or ("CheckBox", "IsChecked") or ("DatePicker", "SelectedDate") or ("Slider", "Value") or ("ComboBox", "SelectedItem")
                or ("ListBox", "SelectedItem") or ("DataGrid", "SelectedItem") or ("PasswordBox", "Password") || attrName.EndsWith("Password", StringComparison.Ordinal);
            if (mode != "OneWay" && mode != "OneTime" && (twoWayDefault || mode is "TwoWay" or "OneWayToSource") && last!.GetSetMethod() == null)
                Problem(e, $"two-way binding on read-only property '{path}' ({el}.{attrName}) - add Mode=OneWay");
            if (mode == "OneWay" && el == "Run") { }

            if (m.Named.TryGetValue("Converter", out var conv))
            {
                var ck = Regex.Match(conv, @"StaticResource\s+(\w+)").Groups[1].Value;
                var expects = ck switch { "BoolToVis" or "InverseBool" => typeof(bool), "FracStar" => typeof(double), "ScoreStar" => typeof(int), _ => null };
                if (expects != null && t != expects) Problem(e, $"converter {ck} expects {expects.Name} but '{path}' is {t.Name}");
            }
            // item type for ItemsSource
            if (attrName == "ItemsSource")
            {
                var en = t.GetInterfaces().Concat(new[] { t }).FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (en == null || t == typeof(string)) { if (!typeof(IEnumerable).IsAssignableFrom(t)) Problem(e, $"ItemsSource '{path}' is not a collection"); return null; }
                return en.GetGenericArguments()[0];
            }
            return null;
        }
    }

    private readonly record struct Markup(string Name, List<string> Positional, Dictionary<string, string> Named);

    private static Markup? Parse(string s)
    {
        if (!s.StartsWith('{') || !s.EndsWith('}')) return null;
        var inner = s[1..^1].Trim();
        var sp = inner.IndexOf(' ');
        var name = sp < 0 ? inner : inner[..sp];
        var rest = sp < 0 ? "" : inner[(sp + 1)..];
        var parts = new List<string>(); var depth = 0; var cur = new System.Text.StringBuilder();
        foreach (var ch in rest)
        {
            if (ch == '{') depth++; if (ch == '}') depth--;
            if (ch == ',' && depth == 0) { parts.Add(cur.ToString().Trim()); cur.Clear(); } else cur.Append(ch);
        }
        if (cur.Length > 0) parts.Add(cur.ToString().Trim());
        var pos = new List<string>(); var named = new Dictionary<string, string>();
        foreach (var part in parts)
        {
            var eq = part.IndexOf('=');
            if (eq > 0 && !part.StartsWith('{') && !part.Contains('{', StringComparison.Ordinal) | eq < part.IndexOf('{', StringComparison.Ordinal)) named[part[..eq].Trim()] = part[(eq + 1)..].Trim();
            else pos.Add(part);
        }
        return new Markup(name, pos, named);
    }
}
