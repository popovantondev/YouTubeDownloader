@echo off
setlocal EnableExtensions DisableDelayedExpansion
if "%~1"=="" (
 echo Drag a video file onto CHECK_VIDEO.cmd.
 pause
 exit /b 1
)
"%~dp0ffprobe.exe" -v error -show_streams -show_format -of json "%~1" >"%~dp0VIDEO_REPORT.json"
if errorlevel 1 (
 echo Inspection failed.
 pause
 exit /b 1
)
echo Report written to %~dp0VIDEO_REPORT.json
pause
