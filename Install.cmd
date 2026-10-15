@echo off
setlocal
cscript //nologo "%~dp0setup.js" install %1
set "RESULT=%errorlevel%"
pause
exit /b %RESULT%
