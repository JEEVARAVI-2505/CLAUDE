using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Engine.Structure.Tests;

internal static class TestModels
{
    private static readonly RuleRepository Rules = RuleRepository.Load();

    public static BuildingModel FromPreset(Func<Core.Model.Brief> create)
    {
        var brief = create();
        new BriefValidator(Rules.Bylaws).ApplyBylawSetbacks(brief);
        var option = new LayoutGenerator(new VastuEngine(Rules.Vastu), Rules.Bylaws).Generate(brief, 1)[0];
        return new BuildingModel { Brief = brief, Storeys = option.Storeys.ToList() };
    }
}
