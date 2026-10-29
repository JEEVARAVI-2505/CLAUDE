using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Drawing;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Vastu;

var outDir = args.Length > 1 ? args[1] : "out";
Directory.CreateDirectory(outDir);
var rules = RuleRepository.Load();
var gen = new LayoutGenerator(new VastuEngine(rules.Vastu), rules.Bylaws);
for (var i = 0; i < BriefPresets.All.Count; i++)
{
    var b = BriefPresets.All[i].Create();
    new BriefValidator(rules.Bylaws).ApplyBylawSetbacks(b);
    var opts = gen.Generate(b, 3);
    foreach (var o in opts)
    {
        var m = new BuildingModel { Brief = b, Storeys = o.Storeys.ToList() };
        foreach (var s in m.Storeys)
            File.WriteAllText(Path.Combine(outDir, $"p{i}_{o.Name[^1]}_{s.Level}.svg"), SvgWriter.Write(PlanDrawer.Draw(m, s), 900));
        Console.WriteLine($"{BriefPresets.All[i].Name} {o.Name}: {o.Summary} fit={o.Metrics.Fitness:0.0}");
        foreach (var t in o.TradeOffs) Console.WriteLine("   - " + t);
    }
}
