using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Vastu;

public enum VastuFit { Preferred, Acceptable, Neutral, Avoid, NotApplicable }

/// <summary>Per-room Vastu result with a plain-language explanation.</summary>
public sealed record RoomVastuResult(
    Guid RoomId, string RoomName, RoomType Type, int Level, Direction Zone, VastuFit Fit, double Penalty, string Explanation);

/// <summary>A fired extra rule (entrance, adjacency, openings, site items).</summary>
public sealed record VastuRuleHit(string RuleId, string Message, double Penalty);

/// <summary>Whole-model Vastu report. Score = 100 - penalties (averaged over storeys), clamped to 0..100.</summary>
public sealed record VastuReport(double Score, IReadOnlyList<RoomVastuResult> Rooms, IReadOnlyList<VastuRuleHit> Rules)
{
    public double TotalPenalty => Rooms.Sum(r => r.Penalty) + Rules.Sum(r => r.Penalty);
}

/// <summary>A one-click improvement: swap two rooms.</summary>
public sealed record FixSuggestion(Guid RoomA, Guid RoomB, int Level, string Description, double VastuGain, double AreaMatchChange);
