using CRM_Master.Core.Interfaces;

namespace CRM_Master.Engine.Licensing;

/// <summary>
/// Local evaluation notice: counts days since first launch (stored in Settings). It is deliberately simple and NOT tamper-resistant -
/// it is informational and never blocks access to or export of the user's data. It is not an activation or DRM system.
/// </summary>
public sealed class EvaluationLicenseProvider : ILicenseProvider
{
    public const int EvaluationDays = 30;
    private readonly ISettingsService _settings;
    public EvaluationLicenseProvider(ISettingsService settings) => _settings = settings;

    public LicenseStatus GetStatus(DateTime nowUtc)
    {
        var start = _settings.Current.FirstRunUtc;
        if (start is null) return new LicenseStatus(LicenseMode.Evaluation, null, EvaluationDays, EvaluationDays, false, $"Evaluation version - {EvaluationDays} days from first launch.");
        var used = (int)Math.Floor((nowUtc - start.Value).TotalDays);
        var left = Math.Max(0, EvaluationDays - used);
        return left > 0
            ? new LicenseStatus(LicenseMode.Evaluation, start, EvaluationDays, left, false, $"Evaluation version - {left} day(s) remaining.")
            : new LicenseStatus(LicenseMode.Evaluation, start, EvaluationDays, 0, true, "Evaluation period has ended. Your data remains fully accessible; please obtain a licence to continue commercial use.");
    }
}
