@echo off
setlocal EnableExtensions DisableDelayedExpansion
set "CSC=%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe"
if not exist "%CSC%" (
 echo Build requires the .NET 3.5 compiler. Installation needs only the prebuilt EXE.
 exit /b 1
)
"%CSC%" /nologo /optimize+ /platform:x86 /target:exe /out:"%~dp0YTWin7SabrHost.exe" "%~dp0SOURCE\Host.cs" "%~dp0SOURCE\MiniJson.cs"
exit /b %errorlevel%
