using System.Diagnostics;
using BuildingStudio.AI.Services;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Model.Commands;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Studio.Pipeline;

// AI Building Studio command line: runs the same pipeline as the desktop app.
var opts = Args.Parse(args);
var command = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "help";
var ai = new AiSettings { Provider = opts.Get("ai", "none"), Model = opts.Get("model", ""), ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "" }
    .Create(new HttpClient { Timeout = TimeSpan.FromSeconds(60) });

switch (command)
{
    case "presets":
        for (var i = 0; i < BriefPresets.All.Count; i++) Console.WriteLine($"{i}: {BriefPresets.All[i].Name}");
        return 0;
    case "demo":
        return await Demo();
    case "edit":
        return await Edit();
    case "gate2":
        return Gate2();
    default:
        Console.WriteLine("""
AI Building Studio CLI
  buildingstudio presets
  buildingstudio demo [--preset 0-4 | --text "30x40 east facing G+1 ..."] [--style Modern|Contemporary|Traditional|Colonial]
                      [--option 1] [--seed 1] [--out out/demo] [--ai none|ollama|gemini]
  buildingstudio edit --project out/demo/project.json --cmd "swap kitchen and store" [--out out/edited]
  buildingstudio gate2        (Phase 2 gate: 5 preset briefs x 3+ valid options)
""");
        return 0;
}

async Task<int> Demo()
{
    var studio = StudioService.Create(ai: ai);
    Brief brief;
    if (opts.Has("text"))
    {
        var (parsed, source) = await studio.BriefAi.ParseAsync(opts.Get("text", ""));
        brief = parsed.Brief;
        studio.Validator.ApplyBylawSetbacks(brief);
        Console.WriteLine($"Brief parsed with {source}:");
        foreach (var u in parsed.Understood) Console.WriteLine($"  · {u}");
        foreach (var w in parsed.Warnings) Console.WriteLine($"  ! {w}");
    }
    else
    {
        var i = int.Parse(opts.Get("preset", "4"));
        brief = studio.Preset(i);
        Console.WriteLine($"Preset {i}: {BriefPresets.All[i].Name}");
    }
    if (opts.Has("style")) brief.Style = Enum.Parse<ExteriorStyle>(opts.Get("style", "Contemporary"), true);

    var issues = studio.Validator.Validate(brief);
    foreach (var issue in issues) Console.WriteLine($"  [{issue.Severity}] {issue.Message} {issue.Suggestion}");
    if (issues.Any(x => x.Severity == IssueSeverity.Error)) Console.WriteLine("  (continuing: rooms will be scaled to fit)");

    var sw = Stopwatch.StartNew();
    var options = studio.Generate(brief, 5, int.Parse(opts.Get("seed", "1")));
    Console.WriteLine($"\n{options.Count} options in {sw.ElapsedMilliseconds} ms:");
    foreach (var o in options) Console.WriteLine($"  {o.Name}  fitness {o.Metrics.Fitness,5:0.0}  {o.Summary}");

    var pick = options[Math.Clamp(int.Parse(opts.Get("option", "1")) - 1, 0, options.Count - 1)];
    foreach (var t in pick.TradeOffs.Take(6)) Console.WriteLine($"    trade-off: {t}");
    var model = studio.CreateModel(brief, pick);
    return Export(studio, model, opts.Get("out", Path.Combine("out", "demo")), options, brief);
}

async Task<int> Edit()
{
    var studio = StudioService.Create(ai: ai);
    var model = ProjectSerializer.Load(opts.Get("project", "out/demo/project.json"));
    studio.Rebuild(model);
    var stack = new CommandStack(model);
    var before = studio.Evaluate(model);
    var message = await studio.ApplyTextEditAsync(stack, opts.Get("cmd", ""), int.Parse(opts.Get("level", "0")));
    var after = studio.Evaluate(model);
    Console.WriteLine(message);
    Console.WriteLine($"Vastu {before.Vastu.Score:0} -> {after.Vastu.Score:0}; cost {ReportComposer.Money(before.Cost.Total)} -> {ReportComposer.Money(after.Cost.Total)}");
    return Export(studio, model, opts.Get("out", Path.Combine("out", "edited")), null, model.Brief);
}

int Export(StudioService studio, BuildingModel model, string outDir, IReadOnlyList<LayoutOption>? options, Brief brief)
{
    var sw = Stopwatch.StartNew();
    var report = studio.Evaluate(model);
    var result = ExportService.ExportAll(studio, model, outDir);
    Console.WriteLine($"\nModel: Vastu {report.Vastu.Score:0}/100 · bylaw failures {report.Compliance.Failures} · " +
                      $"cost {report.Cost.Currency} {ReportComposer.Money(report.Cost.Total)} ({ReportComposer.Money(report.Cost.CostPerSqft)}/sq ft) · " +
                      $"{model.Structure.Columns.Count} columns · steel {model.Structure.Bbs.Sum(b => b.Weight):0} kg");
    foreach (var f in report.Fixes) Console.WriteLine($"  fix: {f.Description}");
    if (options is { Count: > 1 })
    {
        Console.WriteLine("\nOption cost comparison:");
        foreach (var c in studio.CompareOptions(brief, options))
            Console.WriteLine($"  {c.Name}: {ReportComposer.Money(c.Total)} ({ReportComposer.Money(c.PerSqft)}/sq ft), Vastu {c.Vastu:0}");
    }
    Console.WriteLine($"\nExported {result.Files.Count} files to {Path.GetFullPath(outDir)} in {sw.ElapsedMilliseconds} ms");
    foreach (var f in result.Files.Where(f => !f.Contains("drawings"))) Console.WriteLine($"  {Path.GetFileName(f)}");
    Console.WriteLine($"  drawings/ ({result.Files.Count(f => f.Contains("drawings"))} SVG + DXF files)");
    return 0;
}

int Gate2()
{
    var studio = StudioService.Create();
    var ok = true;
    for (var i = 0; i < BriefPresets.All.Count; i++)
    {
        var brief = studio.Preset(i);
        var sw = Stopwatch.StartNew();
        var options = studio.Generate(brief, 5);
        var valid = options.Count(o => o.Storeys.All(s => PlanEditor.IsValidTiling(s) && LayoutScorer.Unreachable(s) == 0));
        var pass = valid >= 3;
        ok &= pass;
        Console.WriteLine($"{(pass ? "PASS" : "FAIL")} {BriefPresets.All[i].Name}: {valid}/{options.Count} valid options, best Vastu {options[0].Metrics.Vastu:0}, {sw.ElapsedMilliseconds} ms");
    }
    return ok ? 0 : 1;
}

internal sealed class Args
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public static Args Parse(string[] args)
    {
        var a = new Args();
        for (var i = 0; i < args.Length; i++)
            if (args[i].StartsWith("--"))
                a._values[args[i][2..]] = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true";
        return a;
    }

    public bool Has(string key) => _values.ContainsKey(key);
    public string Get(string key, string fallback) => _values.GetValueOrDefault(key, fallback);
}
