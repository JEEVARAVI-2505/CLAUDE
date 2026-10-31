using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.DuplicateDetection;

/// <summary>Configurable matching engine settings.</summary>
public sealed class DuplicateOptions
{
    public bool MatchEmail { get; set; } = true;
    public bool MatchPhone { get; set; } = true;
    public bool MatchName { get; set; } = true;
    public bool MatchCompany { get; set; } = true;
    /// <summary>When false, names must be identical after normalisation (no spelling tolerance).</summary>
    public bool UseFuzzy { get; set; } = true;
    public double HighThreshold { get; set; } = 0.90;
    public double PossibleThreshold { get; set; } = 0.80;
    public double LowThreshold { get; set; } = 0.70;
    public int MaxExactBlockSize { get; set; } = 100;
    public int MaxFuzzyBlockSize { get; set; } = 400;
    public string DefaultCountryCode { get; set; } = "91";

    public static DuplicateOptions FromSettings(AppSettings s) => new()
    {
        HighThreshold = s.DuplicateHighThreshold, PossibleThreshold = s.DuplicatePossibleThreshold, LowThreshold = s.DuplicateLowThreshold, DefaultCountryCode = s.DefaultCountryCode
    };
}

/// <summary>A scored pair of contacts. <c>AId &lt; BId</c> always.</summary>
public sealed record DuplicatePair(int AId, int BId, double Score, MatchClass Class, string MatchedOn);

/// <summary>Contacts connected by matches. Low-confidence matches form their own two-contact clusters.</summary>
public sealed class DuplicateCluster
{
    public List<int> ContactIds { get; init; } = new();
    public List<DuplicatePair> Pairs { get; init; } = new();
    public MatchClass Class => Pairs.Count == 0 ? MatchClass.Low : Pairs.Max(p => p.Class);
    public double TopScore => Pairs.Count == 0 ? 0 : Pairs.Max(p => p.Score);
}

public sealed record ScanResult(List<DuplicateCluster> Clusters, int ContactsScanned, int CandidatePairs, int SkippedLargeBlocks);

public enum FieldCompareStatus { Match, Conflict, OnlyLeft, OnlyRight, BothEmpty }

/// <summary>Side-by-side comparison of one field.</summary>
public sealed record FieldComparison(ContactField Field, string Label, string? Left, string? Right, FieldCompareStatus Status);
