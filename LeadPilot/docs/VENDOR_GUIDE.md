# Vendor guide (for the person selling LeadPilot AI)

## Build the installer
* Easiest: push a tag `leadpilot-v1.0.0` - GitHub Actions builds, tests, installs/uninstalls on a clean Windows runner, and attaches `LeadPilotAI_Setup.exe` + `SHA256.txt` to a GitHub Release.
* Locally on Windows: install .NET 8 SDK and Inno Setup 6 (both free), then `powershell -File installer\build-installer.ps1 -Version 1.0.0` → `artifacts\LeadPilotAI_Setup.exe`.

## Licensing (do this once before you sell)
1. `dotnet run --project tools/LeadPilot.LicenseTool -- keygen --out C:\secure\leadpilot-keys`
2. Copy `public.pem` over `src/LeadPilot.Infrastructure/Licensing/vendor-public-key.pem`, commit it, rebuild. **Never commit or lose `private.pem`** (if lost you cannot issue keys for this build).
3. Issue a key after payment (payment itself is handled by your shop/processor - this app has no payment code):
   `... -- sign --key C:\secure\...\private.pem --holder "Acme Ltd" --tier Professional --days 365`
   Single-PC keys need the customer's **computer ID** (shown in About & Licensing): `--tier Single --machine XXXX-XXXX-XXXX-XXXX`.
4. Check any key: `... -- verify --pub public.pem --license LPAI1-…`

Until step 2 is done, builds only offer the free trial - paid keys cannot validate (by design: no shared secret is shipped).
Offline licensing cannot stop a determined cracker; it stops casual sharing (machine-bound keys, signed tiers, signed expiry).

## Code signing
The installer is **unsigned** by default and Windows SmartScreen may warn. When you buy a certificate, add repository secrets `CODESIGN_PFX_BASE64` (base64 of the .pfx) and `CODESIGN_PFX_PASSWORD`; the workflow then signs `LeadPilotAI_Setup.exe` automatically. Do not describe the product as "signed" before then.

## Before you charge money
* Have a lawyer review `installer/EULA.txt` and `Privacy.txt` for your country.
* Decide support channels and add them to the About page text.
* Test on your own Windows 10 and 11 machines (CI covers install, launch of every screen, uninstall; it does not test live internet sources).
