; Inno Setup 6 script (free, https://jrsoftware.org/isinfo.php). Build with:  iscc /DAppVersion=1.0.0 installer\CRM_Master.iss
; Expects the self-contained publish output in dist\publish (see build\publish.ps1).
#define AppName "CRM MASTER"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\dist\publish"
#endif

[Setup]
AppId={{946DCE31-C952-4165-92AA-D81E62161220}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppName}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\CRM_Master.exe
UninstallDisplayName={#AppName}
SetupIconFile=..\CRM_Master.App\Assets\app.ico
LicenseFile=EULA_EVALUATION.txt
OutputDir=..\dist\installer
OutputBaseFilename=CRM_MASTER_Setup_{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Installs for the current user by default (no administrator rights needed); the user may choose "for all users".
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes
CloseApplications=yes
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\CRM_Master.exe"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\CRM_Master.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CRM_Master.exe"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
// User data (database, backups, logs) lives in %LOCALAPPDATA%\CRM Master, NOT in the install folder, so upgrades never touch it.
// On uninstall the data is kept unless the user explicitly chooses to delete it.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\CRM Master');
    if DirExists(DataDir) and (not UninstallSilent) then
      if MsgBox('Do you also want to permanently delete your CRM MASTER data (database, backups, logs)?' + #13#10 + #13#10 + DataDir + #13#10 + #13#10 +
                'Choose No to keep your data (recommended if you plan to reinstall).', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(DataDir, True, True, True);
  end;
end;
