@echo off
"%~dp0InstallerEngine.exe" uninstall %1
set "RESULT=%errorlevel%"
pause
exit /b %RESULT%
