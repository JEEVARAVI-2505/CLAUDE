# Licensing architecture (v1.0 — evaluation only)

CRM MASTER deliberately ships **no fake licence validation**. Version 1.0 contains:
* `ILicenseProvider` (Core) — what the UI asks: mode (Evaluation / Licensed), days remaining, expired flag, message.
* `EvaluationLicenseProvider` (Engine) — a local 30-day *notice* counted from first launch (stored in settings). It is **informational and not tamper-resistant**; it never blocks access to or export of the user's data. It is not DRM.

To sell the product legitimately, add a real provider behind `ILicenseProvider` — for example a signed licence file (asymmetric signature verified offline with an embedded public key; machine-binding optional) or an online activation call to your own licence server —
then register it in `App.xaml.cs` instead of the evaluation provider. Keep the private key / server credentials out of the repository. Decide the commercial terms (seat/machine limits, grace period, offline behaviour) and put them in your EULA.
No payment, activation or licence server is included or required for the free build.
