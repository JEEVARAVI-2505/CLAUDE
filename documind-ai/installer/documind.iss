; Inno Setup 6 script. Per-user install by default (no administrator rights needed), data stays in %LOCALAPPDATA%\DocuMindAI.
#define AppName "DocuMind AI Professional"
#define AppVersion "0.9.0"

[Setup]
AppId={{6F3B3C1E-5C41-4B0B-9C57-2E3E0C7B9A11}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Your installer name here
DefaultDirName={autopf}\DocuMind
DefaultGroupName=DocuMind
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\dist\installer
OutputBaseFilename=DocuMind-Setup-{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=..\docs\EULA_DRAFT.md
UninstallDisplayIcon={app}\DocuMind.exe
SetupIconFile=documind.ico
DisableProgramGroupPage=yes

[Files]
Source: "..\dist\DocuMind\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\DocuMind"; Filename: "{app}\DocuMind.exe"
Name: "{autodesktop}\DocuMind"; Filename: "{app}\DocuMind.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\DocuMind.exe"; Description: "Start DocuMind"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Customer data (%LOCALAPPDATA%\DocuMindAI) is deliberately NOT removed by the uninstaller. See docs\ADMIN_GUIDE.md for erasing it.
Type: filesandordirs; Name: "{app}"
