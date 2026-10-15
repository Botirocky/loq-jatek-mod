#ifndef AppVersion
  #define AppVersion "1.0.1"
#endif

[Setup]
AppId={{6B1E3C2A-7F4D-4E8B-9C21-B0710C4E5A11}
AppName=LOQ Játék Mód
AppVersion={#AppVersion}
AppPublisher=Botirocky
AppPublisherURL=https://github.com/Botirocky/loq-jatek-mod
AppSupportURL=https://github.com/Botirocky/loq-jatek-mod/issues
DefaultDirName={autopf}\LOQ Jatek Mod
DefaultGroupName=LOQ Játék Mód
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir=..\dist
OutputBaseFilename=LoqJatekMod-{#AppVersion}-telepito
SetupIconFile=..\src\LoqJatekMod\ikon.ico
UninstallDisplayIcon={app}\LoqJatekMod.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"

[Tasks]
Name: "asztal"; Description: "Ikon az asztalra"; GroupDescription: "További ikonok:"

[Files]
Source: "..\publish\LoqJatekMod.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\LOQ Játék Mód"; Filename: "{app}\LoqJatekMod.exe"
Name: "{group}\LOQ Játék Mód eltávolítása"; Filename: "{uninstallexe}"
Name: "{autodesktop}\LOQ Játék Mód"; Filename: "{app}\LoqJatekMod.exe"; Tasks: asztal

[Run]
Filename: "{app}\LoqJatekMod.exe"; Description: "LOQ Játék Mód indítása"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
Filename: "{app}\LoqJatekMod.exe"; Parameters: "--visszaallit"; Flags: runhidden waituntilterminated; RunOnceId: "Visszaallitas"

[Registry]
Root: HKLM; Subkey: "SOFTWARE\Botirocky\LoqJatekMod"; Flags: uninsdeletekey

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\LoqJatekMod"
