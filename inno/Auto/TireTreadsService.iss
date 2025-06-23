[Setup]
AppName=TireTreadService
AppVersion=1.0.0.0
DefaultDirName={pf32}\MAM Software\VAST\TireTreadService
DefaultGroupName=TireTreadService
OutputDir=.\Output
OutputBaseFilename=Install_TireTreadService
Compression=lzma
SolidCompression=yes

[Files]
Source: "C:\TireTreads\TireTreadsService\bin\Release\TireTreadsService.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\TireTreads\TireTreadsService\bin\Release\appsettings.json";    DestDir: "{app}"; Flags: ignoreversion
Source: "C:\TireTreads\TireTreadsService\bin\Release\version.json";        DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "sc.exe"; Parameters: "create VASTTireTread binPath= ""{app}\TireTreadsService.exe"" start= auto"; StatusMsg: "Registering Windows Service..."; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "start VASTTireTread"; StatusMsg: "Starting Service..."; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop VASTTireTread"; StatusMsg: "Stopping Service..."; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete VASTTireTread"; StatusMsg: "Removing Service..."; Flags: runhidden waituntilterminated
