namespace CRM_Master.Core.DTOs;

/// <summary>Progress report from a long-running operation.</summary>
public sealed record ProgressInfo(string Stage, int Done, int Total)
{
    public double Fraction => Total <= 0 ? 0 : Math.Clamp((double)Done / Total, 0, 1);
}
