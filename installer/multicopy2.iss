#define MyAppName      "Multicopy2"
#define MyAppPublisher "Justin Pulley"
#define MyAppURL       "https://github.com/jpect/Multicopy"
#define MyAppExeName   "Multicopy2.exe"

; Version injected by build-installer.ps1 via /DMyAppVersion=x.y.z
; Fallback for compiling manually in Inno Setup IDE:
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

[Setup]
AppId={{E5872B6C-ACC9-4074-9D70-7FC761A58BA6}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

OutputDir=output
OutputBaseFilename=Multicopy2-{#MyAppVersion}-Setup

Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Admin required — app needs to set volume labels and erase drives
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; All publish output files (framework-dependent); excludes debug symbols
Source: "..\Multicopy2\bin\Release\net9.0-windows\win-x64\publish\*"; \
  DestDir: "{app}"; \
  Excludes: "*.pdb"; \
  Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}";                         Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}";   Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                   Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#MyAppName}}"; \
  Flags: nowait postinstall skipifsilent

[Code]
// Check whether .NET 9 (or later) WindowsDesktop runtime is installed.
// The registry key has DWORD values named by version, e.g. "9.0.16" = 1.
function IsDotNet9DesktopInstalled: Boolean;
var
  KeyPath: String;
  Names:   TArrayOfString;
  i:       Integer;
begin
  Result  := False;
  KeyPath := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';

  if RegGetValueNames(HKLM, KeyPath, Names) then
  begin
    for i := 0 to GetArrayLength(Names) - 1 do
    begin
      // Accept any 9.x or higher version string
      if (Length(Names[i]) >= 2) and
         (StrToIntDef(Copy(Names[i], 1, Pos('.', Names[i]) - 1), 0) >= 9) then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  if not IsDotNet9DesktopInstalled then
  begin
    if MsgBox(
      '.NET 9 Desktop Runtime is required but was not found on this PC.' + #13#10 + #13#10 +
      'Click OK to open the Microsoft download page, then install the' + #13#10 +
      '"Windows Desktop Runtime x64" package and re-run this installer.',
      mbError, MB_OKCANCEL) = IDOK then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/9.0', '', '', SW_SHOW, ewNoWait, ErrorCode);
    end;
    Result := False;
  end;
end;
