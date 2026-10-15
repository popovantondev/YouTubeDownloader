@echo off
cscript //nologo "%~dp0setup.js" restore %1
if errorlevel 1 pause
