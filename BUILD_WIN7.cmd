@echo off
setlocal EnableExtensions DisableDelayedExpansion
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe"
if not exist "%CSC%" exit /b 2
"%CSC%" /nologo /optimize+ /langversion:3 /platform:x86 /target:exe /out:YTWin7SabrHost.exe SOURCE\*.cs Shared\*.cs
if errorlevel 1 exit /b 1
"%CSC%" /nologo /optimize+ /langversion:3 /platform:x86 /target:winexe /win32icon:PeakMP3\ASSETS\PeakMP3.ico /win32manifest:PeakMP3\SOURCE\app.manifest /out:PeakMP3\PeakMP3.exe /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll PeakMP3\SOURCE\*.cs SOURCE\MiniJson.cs Shared\*.cs
if errorlevel 1 exit /b 1
"%CSC%" /nologo /optimize+ /langversion:3 /platform:x86 /target:winexe /win32icon:installer\download.ico /win32manifest:PeakMP3\SOURCE\app.manifest /out:BrowserSetup.exe /r:System.Drawing.dll /r:System.Windows.Forms.dll installer\ConnectionWizard.cs
if errorlevel 1 exit /b 1
"%CSC%" /nologo /optimize+ /langversion:3 /platform:x86 /target:exe /out:InstallerEngine.exe installer\InstallEngine.cs SOURCE\IntegrationRegistration.cs SOURCE\MiniJson.cs
exit /b %ERRORLEVEL%
