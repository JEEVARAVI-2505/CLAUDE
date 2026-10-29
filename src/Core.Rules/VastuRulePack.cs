using BuildingStudio.Core.Model;

namespace BuildingStudio.Core.Rules;

/// <summary>Preferred / acceptable / avoid zones for one room type.</summary>
public sealed class VastuZoneRule
{
    public List<Direction> Preferred { get; set; } = [];
    public List<Direction> Acceptable { get; set; } = [];
    public List<Direction> Avoid { get; set; } = [];
    public double Weight { get; set; } = 1.0;
    public string Note { get; set; } = "";
}

public sealed class VastuPenalties
{
    public double Acceptable { get; set; } = 3;
    public double Neutral { get; set; } = 6;
    public double Avoid { get; set; } = 12;
}

public sealed class VastuStrictnessFactors
{
    public double Strict { get; set; } = 1.5;
    public double Moderate { get; set; } = 1.0;
    public double Flexible { get; set; } = 0.5;

    public double For(VastuStrictness s) => s switch
    {
        VastuStrictness.Strict => Strict,
        VastuStrictness.Flexible => Flexible,
        _ => Moderate,
    };
}

/// <summary>An extra (non-zone) Vastu rule with a fixed penalty.</summary>
public sealed class VastuExtraRule
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public double Penalty { get; set; }
}

/// <summary>Vastu rule pack (rules/vastu.default.json).</summary>
public sealed class VastuRulePack
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
    public VastuPenalties Penalties { get; set; } = new();
    public VastuStrictnessFactors Strictness { get; set; } = new();
    public Dictionary<RoomType, VastuZoneRule> Rooms { get; set; } = [];
    public VastuZoneRule Entrance { get; set; } = new();
    public Dictionary<string, VastuZoneRule> SiteItems { get; set; } = [];
    public List<VastuExtraRule> ExtraRules { get; set; } = [];

    public VastuExtraRule Extra(string id) =>
        ExtraRules.FirstOrDefault(r => r.Id == id) ?? new VastuExtraRule { Id = id, Description = id };
}
