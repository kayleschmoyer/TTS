[Setup]
AppName=TireTreadsService
AppVersion=1.0.0
DefaultDirName={pf}\TireTreadsService
DefaultGroupName=TireTreadsService
OutputDir=Output
OutputBaseFilename=TireTreadsServiceInstaller
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Files]
Source: "C:\TireTreads\TireTreadsService\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Run]
Filename: "sc.exe"; Parameters: "create TireTreadsService binPath= ""{app}\TireTreadsService.exe"" start= auto DisplayName= ""Tire Treads Inventory Service"""; Flags: runhidden
Filename: "sc.exe"; Parameters: "start TireTreadsService"; Flags: runhidden

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop TireTreadsService"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete TireTreadsService"; Flags: runhidden
