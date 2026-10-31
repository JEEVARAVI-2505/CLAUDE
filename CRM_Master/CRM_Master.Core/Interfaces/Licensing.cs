namespace CRM_Master.Core.Interfaces;

public enum LicenseMode { Evaluation, Licensed }

/// <summary>What the About screen and the status banner show.</summary>
public sealed record LicenseStatus(LicenseMode Mode, DateTime? StartedUtc, int EvaluationDays, int DaysRemaining, bool IsExpired, string Message);

/// <summary>
/// Source of licence state. Version 1.0 ships <c>EvaluationLicenseProvider</c> (a local 30-day evaluation notice). A future commercial
/// release plugs in a provider that validates a genuine activation (e.g. a signed licence file or an online check) behind this interface.
/// </summary>
public interface ILicenseProvider
{
    LicenseStatus GetStatus(DateTime nowUtc);
}
