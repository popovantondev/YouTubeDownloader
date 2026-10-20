[Setup]
#ifdef ISOLATED_TEST
AppId=popovantondev.YouTubeDownloader.PeakMP3.Test260
#else
AppId=popovantondev.YouTubeDownloader.PeakMP3
#endif
AppName=YouTube Downloader + PeakMP3
AppVersion=2.6.0
AppVerName=YouTube Downloader 2.6.0 Preview + PeakMP3 0.16.0
AppPublisher=popovantondev
AppPublisherURL=https://github.com/popovantondev/YouTubeDownloader
DefaultDirName={localappdata}\YTWin7_360_MP3
DisableDirPage=yes
DefaultGroupName=YouTube Downloader
PrivilegesRequired=lowest
MinVersion=6.1sp1
ArchitecturesAllowed=x86compatible
WizardStyle=modern
SetupIconFile=download.ico
UninstallDisplayIcon={app}\PeakMP3\PeakMP3.exe
Compression=lzma2/normal
SolidCompression=yes
OutputDir=..\dist
#ifdef ISOLATED_TEST
OutputBaseFilename=YouTubeDownloader-PeakMP3-2.6.0-TEST-Setup
#else
OutputBaseFilename=YouTubeDownloader-PeakMP3-2.6.0-Preview-Setup
#endif
DisableProgramGroupPage=yes
UninstallFilesDir={app}
CloseApplications=no
RestartApplications=no

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[CustomMessages]
en.ConnectBrowser=Connect the browser extension
ru.ConnectBrowser=Подключить расширение браузера
de.ConnectBrowser=Browser-Erweiterung verbinden
en.PeakShortcut=PeakMP3
ru.PeakShortcut=PeakMP3
de.PeakShortcut=PeakMP3
en.MissingRuntime=This package needs the system .NET 3.5 or .NET 4 runtime. Windows 7 SP1 normally includes .NET 3.5.1. See the offline guide. Setup will not enable Windows features or request administrator privileges.
ru.MissingRuntime=Нужен системный .NET 3.5 или .NET 4. Windows 7 SP1 обычно содержит .NET 3.5.1. См. офлайн-инструкцию. Установщик не включает компоненты Windows и не запрашивает права администратора.
de.MissingRuntime=System-.NET 3.5 oder .NET 4 erforderlich. Windows 7 SP1 enthält normalerweise .NET 3.5.1. Siehe Offline-Anleitung. Setup aktiviert keine Windows-Features und verlangt keine Administratorrechte.
en.InstallFailed=The local components could not be installed. Existing files and registration were restored where possible. Close PeakMP3 and active browser downloads, then retry. Details:
ru.InstallFailed=Не удалось установить локальные компоненты. Прежние файлы и регистрация по возможности восстановлены. Закройте PeakMP3 и активные загрузки браузера и повторите. Подробности:
de.InstallFailed=Lokale Komponenten konnten nicht installiert werden. Vorherige Dateien und Registrierung wurden soweit möglich wiederhergestellt. PeakMP3 und aktive Browserdownloads schließen und erneut versuchen. Details:

[Files]
Source: "..\.payload\*"; DestDir: "{tmp}\YouTubeDownloaderPayload"; Flags: dontcopy ignoreversion recursesubdirs createallsubdirs

[Icons]
#ifndef ISOLATED_TEST
Name: "{userdesktop}\PeakMP3"; Filename: "{app}\PeakMP3\PeakMP3.exe"
Name: "{group}\PeakMP3"; Filename: "{app}\PeakMP3\PeakMP3.exe"
Name: "{group}\YouTube Downloader"; Filename: "{app}\BrowserSetup.exe"; Parameters: "{language}"
Name: "{group}\Uninstall"; Filename: "{uninstallexe}"
#endif

[Run]
Filename: "{app}\BrowserSetup.exe"; Parameters: "{language}"; Description: "{cm:ConnectBrowser}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\InstallerEngine.exe"; Parameters: "uninstall-app {language}"; RunOnceId: "RemoveCombined260"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: files; Name: "{app}\InstallerEngine.exe"
Type: dirifempty; Name: "{app}\extension"
Type: dirifempty; Name: "{app}\PeakMP3"
Type: dirifempty; Name: "{app}"

[Code]
function InitializeSetup(): Boolean;
var Installed: Cardinal;
begin
  Result := (RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5', 'Install', Installed) and (Installed = 1)) or
            (RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Install', Installed) and (Installed = 1));
  if not Result then MsgBox(CustomMessage('MissingRuntime'), mbError, MB_OK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var ExitCode: Integer; Engine: String;
begin
  Result := '';
  ExtractTemporaryFiles('*');
  Engine := ExpandConstant('{tmp}\YouTubeDownloaderPayload\InstallerEngine.exe');
  if not Exec(Engine, 'install ' + ActiveLanguage, '', SW_HIDE, ewWaitUntilTerminated, ExitCode) or (ExitCode <> 0) then
    Result := CustomMessage('InstallFailed') + #13#10 + ExpandConstant('{localappdata}\YTWin7_WatchCapture');
end;
