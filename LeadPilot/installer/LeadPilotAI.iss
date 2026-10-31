; Inno Setup 6 script for LeadPilot AI.  Build with:  ISCC.exe /DMyAppVersion=1.0.0 installer\LeadPilotAI.iss
; Expects the self-contained publish output in ..\publish (see build-installer.ps1).
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppName "LeadPilot AI"
#define MyAppExe "LeadPilotAI.exe"

[Setup]
AppId={{B7F4D2C1-6A3E-4B8F-9E21-5C0D7A4E8F13}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=LeadPilot Software
DefaultDirName={autopf}\LeadPilot AI
DefaultGroupName=LeadPilot AI
DisableProgramGroupPage=yes
OutputDir=..\artifacts
OutputBaseFilename=LeadPilotAI_Setup
SetupIconFile=..\src\LeadPilot.Desktop\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExe}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
LicenseFile=EULA.txt
InfoBeforeFile=Privacy.txt
InfoAfterFile=ReleaseNotes.txt
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=LeadPilot Software
VersionInfoProductName={#MyAppName}
VersionInfoDescription={#MyAppName} Setup
; Code signing: not signed unless a certificate is supplied. In CI the "Sign" step runs signtool on the finished installer when the
; CODESIGN_PFX_BASE64 / CODESIGN_PFX_PASSWORD secrets exist. See docs/VENDOR_GUIDE.md.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\docs\USER_GUIDE.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "ReleaseNotes.txt"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "EULA.txt"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "Privacy.txt"; DestDir: "{app}\docs"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{group}\User guide"; Filename: "{app}\docs\USER_GUIDE.md"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Run]
; Creates %LOCALAPPDATA%\LeadPilotAI (Data, Backups, Logs, Exports) and the empty database for the installing user, then exits.
Filename: "{app}\{#MyAppExe}"; Parameters: "--init"; Flags: runasoriginaluser waituntilterminated; StatusMsg: "Preparing your local database..."
Filename: "{app}\{#MyAppExe}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
// Uninstall never touches the user's data unless they explicitly agree (or pass /DELETEDATA=1 for scripted removal).
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
  Wipe: Boolean;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\LeadPilotAI');
    Wipe := ExpandConstant('{param:DELETEDATA|0}') = '1';
    if (not Wipe) and (not UninstallSilent) and DirExists(DataDir) then
      Wipe := MsgBox('Do you also want to delete your lead data, backups and settings stored in:' + #13#10 + DataDir + #13#10#13#10 +
                     'Choose No to keep your data (recommended). You can then reinstall later and continue where you left off.',
                     mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
    if Wipe and DirExists(DataDir) then
      DelTree(DataDir, True, True, True);
  end;
end;
