@echo off
"%~dp0InstallerEngine.exe" install %1
set "RESULT=%errorlevel%"
pause
exit /b %RESULT%
