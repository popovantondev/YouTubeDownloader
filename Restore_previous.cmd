@echo off
"%~dp0InstallerEngine.exe" restore %1
set "RESULT=%errorlevel%"
pause
exit /b %RESULT%
